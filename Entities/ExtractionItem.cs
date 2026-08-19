using System.ComponentModel.DataAnnotations;

namespace TechnicalChallenge.API.Entities;

public class ExtractionItem
{
    public int Id { get; set; }

    [Required]
    public int ExtractionId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public ExtractionItemStatus Status { get; set; } = ExtractionItemStatus.Pending;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navegación
    public Extraction Extraction { get; set; } = null!;
    public Product Product { get; set; } = null!;
}