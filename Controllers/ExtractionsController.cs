using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnicalChallenge.API.Data;
using TechnicalChallenge.API.Entities;
using TechnicalChallenge.API.Dtos;
using TechnicalChallenge.API.Services;

namespace TechnicalChallenge.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExtractionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IScraperService _scraper;

    public ExtractionsController(AppDbContext context, IScraperService scraper)
    {
        _context = context;
        _scraper = scraper;
    }

    // POST: api/extractions
    [HttpPost]
    public async Task<ActionResult<ExtractionResponseDto>> StartExtraction(ExtractionRequestDto request)
    {
        if (request.ProductIds == null || !request.ProductIds.Any())
        {
            return BadRequest("Debe enviar al menos un ProductId");
        }

        // 1. Crear la Extraction
        var extraction = new Extraction
        {
            Status = ExtractionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.Extractions.Add(extraction);
        await _context.SaveChangesAsync();

        // 2. Crear los ExtractionItems
        var items = new List<ExtractionItem>();
        foreach (var productId in request.ProductIds)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                // Si el producto no existe, creamos un item con estado Failed
                items.Add(new ExtractionItem
                {
                    ExtractionId = extraction.Id,
                    ProductId = productId,
                    Status = ExtractionItemStatus.Failed,
                    ErrorMessage = $"Producto con Id {productId} no encontrado",
                    CompletedAt = DateTime.UtcNow
                });
                continue;
            }

            items.Add(new ExtractionItem
            {
                ExtractionId = extraction.Id,
                ProductId = productId,
                Status = ExtractionItemStatus.Pending
            });
        }
        _context.ExtractionItems.AddRange(items);
        await _context.SaveChangesAsync();

        // 3. Procesar la extracción (por ahora sincrónico, luego lo haremos asíncrono)
        await ProcessExtractionAsync(extraction.Id);

        // 4. Devolver respuesta
        return Ok(await GetExtractionResponseAsync(extraction.Id));
    }

    // GET: api/extractions/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ExtractionResponseDto>> GetExtraction(int id)
    {
        var response = await GetExtractionResponseAsync(id);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

    // Método privado para procesar la extracción
    private async Task ProcessExtractionAsync(int extractionId)
    {
        var extraction = await _context.Extractions
            .Include(e => e.ExtractionItems)
            .ThenInclude(ei => ei.Product)
            .FirstOrDefaultAsync(e => e.Id == extractionId);

        if (extraction == null) return;

        // Actualizar estado de la extracción
        extraction.Status = ExtractionStatus.Processing;
        extraction.StartedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        bool anyFailed = false;

        foreach (var item in extraction.ExtractionItems)
        {
            item.Status = ExtractionItemStatus.Processing;
            item.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            try
            {
                // Scrapear el producto
                var product = await _scraper.ScrapeProductAsync(
                    item.Product.ExternalId,
                    item.Product.SourceUrl
                );

                // Actualizar el producto en BD
                var existingProduct = await _context.Products.FindAsync(item.ProductId);
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
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                item.Status = ExtractionItemStatus.Failed;
                item.ErrorMessage = ex.Message;
                item.CompletedAt = DateTime.UtcNow;
                anyFailed = true;
                await _context.SaveChangesAsync();
            }
        }

        // Actualizar estado de la extracción
        extraction.CompletedAt = DateTime.UtcNow;
        if (anyFailed)
        {
            extraction.Status = ExtractionStatus.CompletedWithErrors;
        }
        else
        {
            extraction.Status = ExtractionStatus.Completed;
        }
        await _context.SaveChangesAsync();
    }

    // Método privado para construir el DTO de respuesta
    private async Task<ExtractionResponseDto?> GetExtractionResponseAsync(int extractionId)
    {
        var extraction = await _context.Extractions
            .Include(e => e.ExtractionItems)
            .ThenInclude(ei => ei.Product)
            .FirstOrDefaultAsync(e => e.Id == extractionId);

        if (extraction == null) return null;

        var itemsDto = extraction.ExtractionItems.Select(ei => new ExtractionItemResponseDto
        {
            Id = ei.Id,
            ProductId = ei.ProductId,
            ProductName = ei.Product?.Name ?? "Producto eliminado",
            Status = ei.Status.ToString(),
            ErrorMessage = ei.ErrorMessage,
            StartedAt = ei.StartedAt,
            CompletedAt = ei.CompletedAt
        }).ToList();

        return new ExtractionResponseDto
        {
            Id = extraction.Id,
            Status = extraction.Status.ToString(),
            CreatedAt = extraction.CreatedAt,
            StartedAt = extraction.StartedAt,
            CompletedAt = extraction.CompletedAt,
            TotalItems = itemsDto.Count,
            SuccessCount = itemsDto.Count(i => i.Status == "Success"),
            FailedCount = itemsDto.Count(i => i.Status == "Failed"),
            Items = itemsDto
        };
    }
}