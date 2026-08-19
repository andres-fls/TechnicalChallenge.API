// TechnicalChallenge.API/Dtos/Products/ProductResponseDto.cs
namespace TechnicalChallenge.API.Dtos.Products;

public class ProductResponseDto
{
    public int Id { get; set; }
    public int ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public string? Brand { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
}
