using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ImageService.API.Models;

public class ImageUploadRequest
{
    [Required]
    public IFormFile Image { get; set; }
}
