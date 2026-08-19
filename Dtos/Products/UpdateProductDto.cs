// TechnicalChallenge.API/Dtos/Products/UpdateProductDto.cs
using System.ComponentModel.DataAnnotations;

namespace TechnicalChallenge.API.Dtos.Products;

public class UpdateProductDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(150)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Availability { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Condition { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Required]
    [MaxLength(500)]
    public string SourceUrl { get; set; } = string.Empty;
}