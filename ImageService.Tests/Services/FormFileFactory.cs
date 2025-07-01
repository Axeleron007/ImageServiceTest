using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageService.Tests.Services;

public static class FormFileFactory
{
    public static async Task<IFormFile> CreateFakeImageFormFileAsync(
        string fileName = "test",
        string fileExtension = "jpg",
        int width = 100,
        int height = 100)
    {
        // Create a dummy image
        var image = new Image<Rgba32>(width, height);

        var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream);
        stream.Position = 0;

        var fileNameWithExtension = $"{fileName}.{fileExtension}";

        // Wrap it in a FormFile
        var formFile = new FormFile(stream, 0, stream.Length, "image", fileNameWithExtension)
        {
            Headers = new HeaderDictionary
            {
                { "Content-Disposition", new StringValues($"form-data; name=\"image\"; filename=\"{fileNameWithExtension}\"") }
            },
            ContentType = "image/jpeg"
        };

        return formFile;
    }
}