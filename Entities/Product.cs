using System.ComponentModel.DataAnnotations;

namespace TechnicalChallenge.API.Entities;

public class Product
{
    public int Id { get; set; }

    [Required]
    public int ExternalId { get; set; } // ID en Automation Exercise

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(150)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Availability { get; set; } = string.Empty; // "In Stock", etc.

    [MaxLength(50)]
    public string? Condition { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Required]
    [MaxLength(500)]
    public string SourceUrl { get; set; } = string.Empty;

    // Relación: un producto puede tener muchos ExtractionItems
    public ICollection<ExtractionItem> ExtractionItems { get; set; } = new List<ExtractionItem>();
}