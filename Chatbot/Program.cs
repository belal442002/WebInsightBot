using Chatbot.Services;

namespace Chatbot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IOpenAIService, OpenAIService>();
            builder.Services.AddScoped<IFileService, FileService>();

            builder.Services.AddHttpClient<IWebScrapingService, WebScrapingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            var app = builder.Build();


            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();

            app.Run();
        }
    }
}
