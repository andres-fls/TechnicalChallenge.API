using System.ComponentModel.DataAnnotations;

namespace TechnicalChallenge.API.Entities;

public class Extraction
{
    public int Id { get; set; }

    [Required]
    public ExtractionStatus Status { get; set; } = ExtractionStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Relación: una extracción tiene muchos items
    public ICollection<ExtractionItem> ExtractionItems { get; set; } = new List<ExtractionItem>();
}