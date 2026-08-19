using AngleSharp;
using AngleSharp.Dom;
using TechnicalChallenge.API.Entities;

namespace TechnicalChallenge.API.Services;

public class ScraperService : IScraperService
{
    private readonly IBrowsingContext _context;

    public ScraperService()
    {
        var config = Configuration.Default.WithDefaultLoader();
        _context = BrowsingContext.New(config);
    }

    public async Task<Product> ScrapeProductAsync(int externalId, string url)
    {
        try
        {
            var document = await _context.OpenAsync(url);

            // Extraer datos (ajusta los selectores según la página real)
            var name = document.QuerySelector(".product-information h2")?.TextContent?.Trim() ?? "Unknown";
            var priceText = document.QuerySelector(".product-information span span")?.TextContent ?? "0";
            var price = decimal.Parse(priceText.Replace("Rs.", "").Replace(",", "").Trim());
            var category = document.QuerySelector(".product-information p")?.TextContent?.Replace("Category:", "").Trim() ?? "Unknown";
            var availability = document.QuerySelector(".product-information p")?.NextElementSibling?.TextContent?.Trim() ?? "In Stock";
            var condition = document.QuerySelector(".product-information p")?.NextElementSibling?.NextElementSibling?.TextContent?.Trim() ?? "New";
            var brand = document.QuerySelector(".product-information p")?.NextElementSibling?.NextElementSibling?.NextElementSibling?.TextContent?.Trim() ?? "Unknown";

            // Crear objeto Product (sin Id, se generará en BD)
            return new Product
            {
                ExternalId = externalId,
                Name = name,
                Price = price,
                Category = category,
                Availability = availability,
                Condition = condition,
                Brand = brand,
                SourceUrl = url
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al scrappear el producto {externalId}: {ex.Message}", ex);
        }
    }
}
