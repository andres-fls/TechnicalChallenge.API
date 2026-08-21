using AngleSharp;
using AngleSharp.Dom;
using System.Globalization;
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
        var document = await _context.OpenAsync(url);

        // 1. Nombre
        var name = document.QuerySelector(".product-information h2")?.TextContent?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new Exception("No se encontró el nombre del producto");

        // 2. Precio (con TryParse y cultura invariante)
        var priceElement = document.QuerySelector(".product-information span span");
        if (priceElement == null)
            throw new Exception("No se encontró el elemento del precio");

        var priceText = priceElement.TextContent
            .Replace("Rs.", "")
            .Replace(",", "")
            .Trim();

        if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            throw new Exception($"No se pudo parsear el precio: '{priceText}'");

        // 3. Categoría
        var category = document.QuerySelectorAll(".product-information p")
            .FirstOrDefault(p => p.TextContent.Contains("Category:"))?
            .TextContent?
            .Replace("Category:", "")
            .Trim();
        if (string.IsNullOrEmpty(category))
            throw new Exception("No se encontró la categoría");

        // 4. Availability
        var availability = document.QuerySelectorAll(".product-information p")
            .FirstOrDefault(p => p.TextContent.Contains("Availability:"))?
            .TextContent?
            .Replace("Availability:", "")
            .Trim();
        if (string.IsNullOrEmpty(availability))
            throw new Exception("No se encontró la disponibilidad");

        // 5. Condition
        var condition = document.QuerySelectorAll(".product-information p")
            .FirstOrDefault(p => p.TextContent.Contains("Condition:"))?
            .TextContent?
            .Replace("Condition:", "")
            .Trim();
        if (string.IsNullOrEmpty(condition))
            throw new Exception("No se encontró la condición");

        // 6. Brand
        var brand = document.QuerySelectorAll(".product-information p")
            .FirstOrDefault(p => p.TextContent.Contains("Brand:"))?
            .TextContent?
            .Replace("Brand:", "")
            .Trim();
        if (string.IsNullOrEmpty(brand))
            throw new Exception("No se encontró la marca");

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
}
