using Azure.Storage.Blobs;
using ImageService.API.Middleware;
using ImageService.API.OperationFilters;
using ImageService.Core;
using ImageService.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using Microsoft.OpenApi.Models;

namespace ImageService.API;

public class Startup
{
    private IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddApplicationInsightsTelemetry(opt =>
        {
            opt.ConnectionString = _configuration["ApplicationInsightsInstrumentationKey"];
        });

        services.AddSingleton(x =>
        {
            var connectionString = _configuration["AzureBlobStorageConnectionString"];
            var containerName = "images";

            var client = new BlobContainerClient(connectionString, containerName);
            client.CreateIfNotExists();
            return client;
        });

        services.AddTransient<ErrorHandlingMiddleware>();
        services.AddScoped<IImageService, Core.ImageService>();
        services.AddSingleton<RecyclableMemoryStreamManager>();
        services.AddSingleton<IBlobContainerWrapper, BlobContainerWrapper>();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Image API", Version = "v1" });
            c.EnableAnnotations();
            c.OperationFilter<FileUploadOperationFilter>();
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
    {
        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.Use(async (context, next) =>
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = long.Parse(_configuration["MaxImageSizeInBytes"]);
            await next.Invoke();
        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Image API v1"));

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}