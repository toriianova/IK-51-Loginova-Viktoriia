using ІК_51_23_Логінова_В.Р_.Models.Config;
using ІК_51_23_Логінова_В.Р_.Services;
using Microsoft.OpenApi.Models;
namespace ІК_51_23_Логінова_В.Р_
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.Configure<SpotifySettings>(
            builder.Configuration.GetSection("Spotify"));

            builder.Services.AddScoped<SpotifyAuthService>();
            builder.Services.AddScoped<SpotifyApiService>();
            builder.Services.AddScoped<FavoritesService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Music Search Bot API",
                    Version = "v1",
                    Description = "API for searching music tracks, managing favorites, ratings and notes."
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();//перенаправляє з http в https

            app.UseAuthorization();//перевірка авторизації

            app.MapControllers();//маршрути контролерів


            app.Run();
        }
    }
}
