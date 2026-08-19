// TechnicalChallenge.API/Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnicalChallenge.API.Data;
using TechnicalChallenge.API.Dtos.Products;
using TechnicalChallenge.API.Entities;

namespace TechnicalChallenge.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
    {
        var products = await _context.Products
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                ExternalId = p.ExternalId,
                Name = p.Name,
                Price = p.Price,
                Category = p.Category,
                Availability = p.Availability,
                Condition = p.Condition,
                Brand = p.Brand,
                SourceUrl = p.SourceUrl
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        var productDto = new ProductResponseDto
        {
            Id = product.Id,
            ExternalId = product.ExternalId,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            Availability = product.Availability,
            Condition = product.Condition,
            Brand = product.Brand,
            SourceUrl = product.SourceUrl
        };

        return Ok(productDto);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> CreateProduct(CreateProductDto createDto)
    {
        // Verificar que no exista otro producto con el mismo ExternalId
        var existing = await _context.Products
            .AnyAsync(p => p.ExternalId == createDto.ExternalId);

        if (existing)
        {
            return Conflict($"Ya existe un producto con ExternalId {createDto.ExternalId}");
        }

        var product = new Product
        {
            ExternalId = createDto.ExternalId,
            Name = createDto.Name,
            Price = createDto.Price,
            Category = createDto.Category,
            Availability = createDto.Availability,
            Condition = createDto.Condition,
            Brand = createDto.Brand,
            SourceUrl = createDto.SourceUrl
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var productDto = new ProductResponseDto
        {
            Id = product.Id,
            ExternalId = product.ExternalId,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            Availability = product.Availability,
            Condition = product.Condition,
            Brand = product.Brand,
            SourceUrl = product.SourceUrl
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchProduct(int id, PatchProductDto patchDto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Actualizar solo los campos que vinieron en la petición
        if (patchDto.Name != null)
            product.Name = patchDto.Name;
        if (patchDto.Price.HasValue)
            product.Price = patchDto.Price.Value;
        if (patchDto.Category != null)
            product.Category = patchDto.Category;
        if (patchDto.Availability != null)
            product.Availability = patchDto.Availability;
        if (patchDto.Condition != null)
            product.Condition = patchDto.Condition;
        if (patchDto.Brand != null)
            product.Brand = patchDto.Brand;
        if (patchDto.SourceUrl != null)
            product.SourceUrl = patchDto.SourceUrl;

        await _context.SaveChangesAsync();

        // Devolver el producto actualizado
        var responseDto = new ProductResponseDto
        {
            Id = product.Id,
            ExternalId = product.ExternalId,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            Availability = product.Availability,
            Condition = product.Condition,
            Brand = product.Brand,
            SourceUrl = product.SourceUrl
        };
        return Ok(responseDto);
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        // Verificar si tiene ExtractionItems asociados 
        var hasItems = await _context.ExtractionItems
            .AnyAsync(ei => ei.ProductId == id);

        if (hasItems)
        {
            return Conflict("No se puede eliminar el producto porque tiene historial de extracciones.");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}