using Azure.Storage;
using Azure.Storage.Blobs.Models;
using ImageService.Core.Dtos;
using ImageService.Core.Exceptions;
using ImageService.Core.Helpers;
using ImageService.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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

    public ImageService(
        IBlobContainerWrapper containerWrapper,
        RecyclableMemoryStreamManager streamManager,
        IConfiguration configuration)
    {
        _containerWrapper = containerWrapper;
        _streamManager = streamManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Upload original image
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    /// <exception cref="BusinessValidationException"></exception>
    public async Task<GetImageResponseDto> UploadImageAsync(IFormFile image, CancellationToken cancellationToken)
    {
        var extension = string.Join(string.Empty, Path.GetExtension(image.FileName).Skip(1));
        var supportedExtensions = _configuration["SupportedImageExtensions"].Split(",");

        if (!supportedExtensions.Contains(extension))
        {
            throw new BusinessValidationException("Unsupported image extension.");
        }

        if (image.Length > int.Parse(_configuration["MaxImageSizeInBytes"]))
        {
            throw new BusinessValidationException("File too large."); // No more than 1 GB file
        }

        var id = Guid.NewGuid().ToString();

        var blob = _containerWrapper.GetBlockBlobClient(id);

        using var imageStream = image.OpenReadStream();
        using var imageInfo = await Image.LoadAsync<Rgba32>(imageStream);
        var width = imageInfo.Width;
        var height = imageInfo.Height;

        imageStream.Position = 0;

        await blob.UploadAsync(image.OpenReadStream(), new BlobUploadOptions
        {
            TransferOptions = new StorageTransferOptions
            {
                MaximumTransferSize = 4 * 1024 * 1024, // 4 MB blocks
                MaximumConcurrency = 4                 // parallelism
            },
            HttpHeaders = new BlobHttpHeaders { ContentType = image.ContentType }
        }, cancellationToken);

        return BuildSuccessImageResponse(id, blob.Uri.ToString());
    }

    /// <summary>
    /// Retrieve image by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="BusinessValidationException"></exception>
    public async Task<GetImageResponseDto> GetImagePathAsync(string id, CancellationToken cancellationToken)
    {
        var blob = _containerWrapper.GetBlockBlobClient(id);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            throw new BusinessValidationException($"Image with id {id} not found.");
        }

        return BuildSuccessImageResponse(id, blob.Uri.ToString());
    }

    /// <summary>
    /// Resize and save image to target height with original aspect ratio
    /// </summary>
    /// <param name="id"></param>
    /// <param name="targetHeight"></param>
    /// <returns></returns>
    /// <exception cref="BusinessValidationException"></exception>
    /// <exception cref="TargetHeightExceededException"></exception>
    public async Task<GetImageResponseDto> ResizeAndSaveAsync(string id, int targetHeight, CancellationToken cancellationToken)
    {
        var resizedName = $"{id}_{targetHeight}";

        var resizedBlob = _containerWrapper.GetBlockBlobClient(resizedName);

        // Fast path: If already resized
        if (await resizedBlob.ExistsAsync())
        {
            return BuildSuccessImageResponse(id, resizedBlob.Uri.ToString());
        }

        var originalBlob = _containerWrapper.GetBlockBlobClient(id);

        if (!await originalBlob.ExistsAsync(cancellationToken))
        {
            throw new BusinessValidationException("Original image not found.");
        }

        var download = await originalBlob.DownloadAsync(cancellationToken);
        var stream = download.Value.Content;

        var (image, format) = await ImageHelper.LoadImageAndFormatAsync(stream);

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

        await resizedBlob.UploadAsync(outStream, new BlobUploadOptions
        {
            TransferOptions = new StorageTransferOptions
            {
                MaximumTransferSize = 4 * 1024 * 1024, // 4 MB blocks
                MaximumConcurrency = 4                 // parallelism
            },
            HttpHeaders = new BlobHttpHeaders { ContentType = download.Value.Details.ContentType }
        });

        return BuildSuccessImageResponse(id, resizedBlob.Uri.ToString());
    }

    /// <summary>
    /// Delete original and resized images by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="BusinessValidationException"></exception>
    public async Task<DeleteImageResponseDto> DeleteImagesAsync(string id, CancellationToken cancellationToken)
    {
        var notFound = true;

        var blobs = _containerWrapper.GetBlobsAsync(prefix: id);
        await foreach (var blobItem in blobs)
        {
            var blobClient = _containerWrapper.GetBlockBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            notFound = false;
        }

        if (notFound)
        {
            throw new BusinessValidationException("Image not found.");
        }

        return new DeleteImageResponseDto
        {
            Id = id,
            Message = "Success"
        };
    }

    private static GetImageResponseDto BuildSuccessImageResponse(string id, string url)
    {
        return new GetImageResponseDto
        {
            Id = id,
            Url = url,
            Message = "Success"
        };
    }
}