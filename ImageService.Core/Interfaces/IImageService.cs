using ImageService.Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace ImageService.Core.Interfaces;

public interface IImageService
{
    Task<GetImageResponseDto> UploadImageAsync(IFormFile image, CancellationToken cancellationToken);
    Task<GetImageResponseDto> GetImagePathAsync(string id, CancellationToken cancellationToken);
    Task<DeleteImageResponseDto> DeleteImagesAsync(string id, CancellationToken cancellationToken);
    Task<GetImageResponseDto> ResizeAndSaveAsync(string id, int targetHeight, CancellationToken cancellationToken);
}
