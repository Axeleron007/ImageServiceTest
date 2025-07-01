using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace ImageService.Core.Helpers;

public static class ImageHelper
{
    public static async Task<(Image image, IImageFormat? format)> LoadImageAndFormatAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            var temp = new MemoryStream();
            await stream.CopyToAsync(temp);
            temp.Position = 0;
            stream = temp;
        }

        var image = await Image.LoadAsync(stream);
        var format = image.Metadata?.DecodedImageFormat;
        return (image, format);
    }
}