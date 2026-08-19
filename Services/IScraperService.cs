using TechnicalChallenge.API.Entities;

namespace TechnicalChallenge.API.Services;

public interface IScraperService
{
    Task<Product> ScrapeProductAsync(int externalId, string url);
}
