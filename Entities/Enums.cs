namespace TechnicalChallenge.API.Entities;

public enum ExtractionStatus
{
    Pending,
    Processing,
    Completed,
    CompletedWithErrors,
    Failed
}

public enum ExtractionItemStatus
{
    Pending,
    Processing,
    Success,
    Failed
}