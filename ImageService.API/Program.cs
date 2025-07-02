using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace ImageService.API;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
                webBuilder.UseUrls($"http://0.0.0.0:{port}");
                webBuilder.UseStartup<Startup>();
            });
}