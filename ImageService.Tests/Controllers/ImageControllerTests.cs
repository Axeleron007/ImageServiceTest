using FluentAssertions;
using ImageService.API.Controllers;
using ImageService.API.Models;
using ImageService.Core.Dtos;
using ImageService.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;

namespace ImageService.Tests.Controllers;

public class ImagesControllerTests
{
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly ImagesController _controller;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public ImagesControllerTests()
    {
        _imageServiceMock = new Mock<IImageService>();
        _controller = new ImagesController(_imageServiceMock.Object);
    }

    [Fact]
    public async Task Upload_ShouldReturnOkResult_WithImageResponse()
    {
        // Arrange
        var imageBytes = Encoding.UTF8.GetBytes("fake image content");
        var stream = new MemoryStream(imageBytes);
        var mockFile = new FormFile(stream, 0, imageBytes.Length, "image", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var request = new ImageUploadRequest { Image = mockFile };

        var expected = new GetImageResponseDto
        {
            Id = "123",
            Url = "http://localhost/images/123",
            Message = "Success"
        };

        _imageServiceMock
            .Setup(s => s.UploadImageAsync(mockFile, _cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Upload(request, _cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetOriginal_ShouldReturnImagePath()
    {
        // Arrange
        var id = "abc123";
        var expected = new GetImageResponseDto
        {
            Id = id,
            Url = $"http://localhost/images/{id}",
            Message = "Success"
        };

        _imageServiceMock
            .Setup(s => s.GetImagePathAsync(id, _cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetOriginal(id, _cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetResizedImage_ShouldReturnResizedImagePath()
    {
        // Arrange
        var id = "img456";
        var height = 120;
        var expected = new GetImageResponseDto
        {
            Id = id,
            Url = $"http://localhost/images/{id}_{height}",
            Message = "Success"
        };

        _imageServiceMock
            .Setup(s => s.ResizeAndSaveAsync(id, height, _cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetResizedImage(id, height, _cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetThumbnailImage_ShouldReturn160pxImage()
    {
        // Arrange
        var id = "img789";
        var expected = new GetImageResponseDto
        {
            Id = id,
            Url = $"http://localhost/images/{id}_160",
            Message = "Success"
        };

        _imageServiceMock
            .Setup(s => s.ResizeAndSaveAsync(id, 160, _cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetThumbnailImage(id, _cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccessMessage()
    {
        // Arrange
        var id = "to-delete";
        var expected = new DeleteImageResponseDto
        {
            Id = id,
            Message = "Success"
        };

        _imageServiceMock
            .Setup(s => s.DeleteImagesAsync(id, _cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Delete(id, _cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }
}
