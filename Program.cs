using Microsoft.EntityFrameworkCore;
using TechnicalChallenge.API.Background;
using TechnicalChallenge.API.Data;
using TechnicalChallenge.API.Services;

namespace TechnicalChallenge.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IScraperService, ScraperService>();
            builder.Services.AddSingleton<ExtractionQueue>();
            builder.Services.AddHostedService<ExtractionWorker>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
