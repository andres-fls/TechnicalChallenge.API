namespace TechnicalChallenge.API.Dtos;

public class ExtractionResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ExtractionItemResponseDto> Items { get; set; } = new();
}
