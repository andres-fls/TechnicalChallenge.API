namespace TechnicalChallenge.API.Dtos.Products;

    public class PatchProductDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public string? Availability { get; set; }
        public string? Condition { get; set; }
        public string? Brand { get; set; }
        public string? SourceUrl { get; set; }
    }