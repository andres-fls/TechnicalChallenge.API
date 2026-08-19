using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using TechnicalChallenge.API.Background;
using TechnicalChallenge.API.Data;
using TechnicalChallenge.API.Dtos;
using TechnicalChallenge.API.Entities;
using TechnicalChallenge.API.Services;

namespace TechnicalChallenge.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExtractionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IScraperService _scraper;
    private readonly ExtractionQueue _queue;

    public ExtractionsController(AppDbContext context, IScraperService scraper, ExtractionQueue queue)
    {
        _context = context;
        _scraper = scraper;
        _queue = queue;
    }

    // POST: api/extractions
    [HttpPost]
    public async Task<ActionResult> StartExtraction(ExtractionRequestDto request)
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

        // 3. Encolar la extracción para procesamiento en segundo plano
        _queue.Enqueue(extraction.Id);

        // 4. Devolver 202 Accepted
        return Accepted(new { extractionId = extraction.Id, status = extraction.Status.ToString() });
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