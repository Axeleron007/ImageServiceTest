using ImageService.API.Models;
using ImageService.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace ImageService.API.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : ControllerBase
    {
        private const int ThumbnailHeight = 160;
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
        {
            var response = await _imageService.UploadImageAsync(request.Image, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOriginal(string id, CancellationToken cancellationToken)
        {
            var response = await _imageService.GetImagePathAsync(id, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id}/variation")]
        public async Task<IActionResult> GetResizedImage(string id, int targetHeight, CancellationToken cancellationToken)
        {
            var response = await _imageService.ResizeAndSaveAsync(id, targetHeight, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id}/thumbnail")]
        public async Task<IActionResult> GetThumbnailImage(string id, CancellationToken cancellationToken)
        {
            var response = await _imageService.ResizeAndSaveAsync(id, ThumbnailHeight, cancellationToken);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var response = await _imageService.DeleteImagesAsync(id, cancellationToken);
            return Ok(response);
        }
    }
}
