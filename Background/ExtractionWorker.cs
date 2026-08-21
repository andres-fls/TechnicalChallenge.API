using TechnicalChallenge.API.Data;
using TechnicalChallenge.API.Entities;
using TechnicalChallenge.API.Services;
using Microsoft.EntityFrameworkCore;

namespace TechnicalChallenge.API.Background;

public class ExtractionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExtractionQueue _queue;
    private readonly ILogger<ExtractionWorker> _logger;

    // Límite de extracciones simultáneas (no de productos individuales).
    // Esto mantiene un control conservador sobre el total de peticiones HTTP al sitio externo.
    private readonly SemaphoreSlim _semaphore = new(5);

    public ExtractionWorker(IServiceProvider serviceProvider, ExtractionQueue queue, ILogger<ExtractionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var extractionId = await _queue.DequeueAsync(stoppingToken);
                _logger.LogInformation($"Procesando extracción {extractionId}");

                // Procesar en un scope para tener DbContext y ScraperService
                _ = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var scraper = scope.ServiceProvider.GetRequiredService<IScraperService>();

                        await ProcessExtractionAsync(context, scraper, extractionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error procesando extracción {extractionId}");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el worker");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessExtractionAsync(AppDbContext context, IScraperService scraper, int extractionId)
    {
        // Variable para controlar si hubo fallos en items individuales
        bool anyFailed = false;

        try
        {
            // 1. Obtener la extracción con sus items y productos
            var extraction = await context.Extractions
                .Include(e => e.ExtractionItems)
                .ThenInclude(ei => ei.Product)
                .FirstOrDefaultAsync(e => e.Id == extractionId);

            // Si no existe, salir
            if (extraction == null)
            {
                _logger.LogWarning($"Extracción {extractionId} no encontrada");
                return;
            }

            // 2. Marcar la extracción como Processing
            extraction.Status = ExtractionStatus.Processing;
            extraction.StartedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            _logger.LogInformation($"Extracción {extractionId} iniciada");

            // 3. Procesar cada item (solo los pendientes con ProductId no nulo)
            foreach (var item in extraction.ExtractionItems.Where(i => i.ProductId != null && i.Status == ExtractionItemStatus.Pending))
            {
                // Marcar item como Processing
                item.Status = ExtractionItemStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                try
                {
                    // Scraping del producto
                    var product = await scraper.ScrapeProductAsync(
                        item.Product!.ExternalId,
                        item.Product!.SourceUrl
                    );

                    // Actualizar el producto en BD
                    var existingProduct = await context.Products.FindAsync(item.ProductId);
                    if (existingProduct != null)
                    {
                        existingProduct.Name = product.Name;
                        existingProduct.Price = product.Price;
                        existingProduct.Category = product.Category;
                        existingProduct.Availability = product.Availability;
                        existingProduct.Condition = product.Condition;
                        existingProduct.Brand = product.Brand;
                        existingProduct.SourceUrl = product.SourceUrl;
                    }

                    // Marcar item como Success
                    item.Status = ExtractionItemStatus.Success;
                    item.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Item {item.Id} (Producto {item.ProductId}) procesado con éxito");
                }
                catch (Exception ex)
                {
                    // Error en este item específico
                    item.Status = ExtractionItemStatus.Failed;
                    item.ErrorMessage = ex.Message;
                    item.CompletedAt = DateTime.UtcNow;
                    anyFailed = true;
                    await context.SaveChangesAsync();
                    _logger.LogError(ex, $"Error en item {item.Id} (Producto {item.ProductId})");
                }
            }

            // 4. Marcar la extracción como Completed o CompletedWithErrors
            extraction.CompletedAt = DateTime.UtcNow;
            extraction.Status = anyFailed ? ExtractionStatus.CompletedWithErrors : ExtractionStatus.Completed;
            await context.SaveChangesAsync();
            _logger.LogInformation($"Extracción {extractionId} finalizada con estado {extraction.Status}");
        }
        catch (Exception ex)
        {
            // ERROR FATAL: Algo grave ocurrió fuera del procesamiento de items
            // (ej. error de BD, error al obtener la extracción, error al guardar, etc.)

            _logger.LogError(ex, $"Error FATAL en extracción {extractionId}");

            try
            {
                // Intentar marcar la extracción como Failed 
                var extraction = await context.Extractions.FindAsync(extractionId);
                if (extraction != null)
                {
                    extraction.Status = ExtractionStatus.Failed;
                    extraction.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Extracción {extractionId} marcada como Failed tras error fatal");
                }
            }
            catch (Exception innerEx)
            {
                // Si incluso falla al marcar la extracción, solo logueamos
                _logger.LogError(innerEx, $"No se pudo actualizar el estado de la extracción {extractionId} tras error fatal");
            }
        }
    }
}