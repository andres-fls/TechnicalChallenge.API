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
        var extraction = await context.Extractions
            .Include(e => e.ExtractionItems)
            .ThenInclude(ei => ei.Product)
            .FirstOrDefaultAsync(e => e.Id == extractionId);

        if (extraction == null) return;

        try
        {
            extraction.Status = ExtractionStatus.Processing;
            extraction.StartedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            bool anyFailed = false;

            // FILTRO: Solo procesar ítems que estén en estado Pending 
            var pendingItems = extraction.ExtractionItems.Where(i => i.Status == ExtractionItemStatus.Pending).ToList();

            foreach (var item in pendingItems)
            {
                // Si por algún motivo el producto es null, marcar como Failed
                if (item.Product == null)
                {
                    item.Status = ExtractionItemStatus.Failed;
                    item.ErrorMessage = "Producto no encontrado";
                    item.CompletedAt = DateTime.UtcNow;
                    anyFailed = true;
                    await context.SaveChangesAsync();
                    continue;
                }

                item.Status = ExtractionItemStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                try
                {
                    var product = await scraper.ScrapeProductAsync(
                        item.Product.ExternalId,
                        item.Product.SourceUrl
                    );

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

                    item.Status = ExtractionItemStatus.Success;
                    item.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    item.Status = ExtractionItemStatus.Failed;
                    item.ErrorMessage = ex.Message;
                    item.CompletedAt = DateTime.UtcNow;
                    anyFailed = true;
                    await context.SaveChangesAsync();
                }
            }

            // Actualizar estado de la extracción al finalizar todos los items
            extraction.CompletedAt = DateTime.UtcNow;
            if (anyFailed)
            {
                extraction.Status = ExtractionStatus.CompletedWithErrors;
            }
            else
            {
                extraction.Status = ExtractionStatus.Completed;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Error fatal: marcar extracción como Failed
            extraction.Status = ExtractionStatus.Failed;
            extraction.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            _logger.LogError(ex, "Error fatal procesando extracción {ExtractionId}", extractionId);
        }
    }
}