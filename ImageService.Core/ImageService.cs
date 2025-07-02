using Azure.Storage;
using Azure.Storage.Blobs.Models;
using ImageService.Core.Dtos;
using ImageService.Core.Exceptions;
using ImageService.Core.Helpers;
using ImageService.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageService.Core;

public class ImageService : IImageService
{
    private readonly IBlobContainerWrapper _containerWrapper;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        IBlobContainerWrapper containerWrapper,
        RecyclableMemoryStreamManager streamManager,
        IConfiguration configuration,
        ILogger<ImageService> logger)
    {
        _containerWrapper = containerWrapper;
        _streamManager = streamManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GetImageResponseDto> UploadImageAsync(IFormFile image, CancellationToken cancellationToken)
    {
        ValidateImageExtension(image.FileName);
        ValidateImageSize(image.Length);

        var id = Guid.NewGuid().ToString();
        var blobClient = _containerWrapper.GetBlockBlobClient(id);

        using var imageStream = image.OpenReadStream();
        using var imageInfo = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        imageStream.Position = 0;
        await UploadToBlobAsync(blobClient, image.OpenReadStream(), image.ContentType, cancellationToken);

        return BuildImageResponse(id, blobClient.Uri.ToString());
    }

    public async Task<GetImageResponseDto> GetImagePathAsync(string id, CancellationToken cancellationToken)
    {
        var blob = _containerWrapper.GetBlockBlobClient(id);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            LogAndThrowNotFound(id);
        }

        return BuildImageResponse(id, blob.Uri.ToString());
    }

    public async Task<GetImageResponseDto> ResizeAndSaveAsync(string id, int targetHeight, CancellationToken cancellationToken)
    {
        var resizedName = $"{id}_{targetHeight}";
        var resizedBlob = _containerWrapper.GetBlockBlobClient(resizedName);

        if (await resizedBlob.ExistsAsync(cancellationToken))
        {
            return BuildImageResponse(id, resizedBlob.Uri.ToString());
        }

        var originalBlob = _containerWrapper.GetBlockBlobClient(id);
        if (!await originalBlob.ExistsAsync(cancellationToken))
        {
            LogAndThrowNotFound(id);
        }

        var download = await originalBlob.DownloadAsync(cancellationToken);
        var (image, format) = await ImageHelper.LoadImageAndFormatAsync(download.Value.Content);

        if (targetHeight > image.Height)
        {
            throw new TargetHeightExceededException();
        }

        int targetWidth = (int)(image.Width * (targetHeight / (double)image.Height));
        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        using var outStream = _streamManager.GetStream();
        if (format != null)
            await image.SaveAsync(outStream, format, cancellationToken);
        else
            await image.SaveAsJpegAsync(outStream, cancellationToken);

        outStream.Position = 0;

        await UploadToBlobAsync(resizedBlob, outStream, download.Value.Details.ContentType, cancellationToken);

        return BuildImageResponse(id, resizedBlob.Uri.ToString());
    }

    public async Task<DeleteImageResponseDto> DeleteImagesAsync(string id, CancellationToken cancellationToken)
    {
        var blobs = _containerWrapper.GetBlobsAsync(prefix: id);
        var anyFound = false;

        await foreach (var blobItem in blobs)
        {
            var blobClient = _containerWrapper.GetBlockBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            anyFound = true;
        }

        if (!anyFound)
        {
            LogAndThrowNotFound(id);
        }

        return new DeleteImageResponseDto
        {
            Id = id,
            Message = "Success"
        };
    }

    private void ValidateImageExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var allowed = _configuration["SupportedImageExtensions"]?.Split(",") ?? Array.Empty<string>();

        if (!allowed.Contains(extension))
        {
            _logger.LogError("Unsupported image extension: {Extension}", extension);
            throw new BusinessValidationException("Unsupported image extension.");
        }
    }

    private void ValidateImageSize(long size)
    {
        var maxSize = long.Parse(_configuration["MaxImageSizeInBytes"]);
        if (size > maxSize)
        {
            _logger.LogError("File too large. Limit: {MaxSize} bytes", maxSize);
            throw new BusinessValidationException("File too large.");
        }
    }

    private async Task UploadToBlobAsync(Azure.Storage.Blobs.Specialized.BlockBlobClient blob, Stream data, string contentType, CancellationToken cancellationToken)
    {
        await blob.UploadAsync(data, new BlobUploadOptions
        {
            TransferOptions = new StorageTransferOptions
            {
                MaximumTransferSize = 4 * 1024 * 1024,
                MaximumConcurrency = 4
            },
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);
    }

    private void LogAndThrowNotFound(string id)
    {
        _logger.LogError("Image with id {Id} not found.", id);
        throw new BusinessValidationException($"Image with id {id} not found.");
    }

    private static GetImageResponseDto BuildImageResponse(string id, string url) => new()
    {
        Id = id,
        Url = url,
        Message = "Success"
    };
}
