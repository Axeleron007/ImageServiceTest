using Azure.Storage.Blobs;
using ImageService.API.Middleware;
using ImageService.API.OperationFilters;
using ImageService.Core;
using ImageService.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IO;
using Microsoft.OpenApi.Models;
using System;

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
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        _configuration = builder.Build();

        services.AddControllers();

        services.AddApplicationInsightsTelemetry();

        services.AddSingleton(x =>
        {
            var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
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

        var builder = new ConfigurationBuilder()
            .AddConfiguration(configuration);

        _configuration = builder.Build();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Image API v1"));
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}