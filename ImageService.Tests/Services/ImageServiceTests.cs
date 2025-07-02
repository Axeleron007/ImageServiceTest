using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FluentAssertions;
using ImageService.Core.Exceptions;
using ImageService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageService.Tests.Services;

public class ImageServiceTests
{
    private readonly Mock<IBlobContainerWrapper> _blobContainerWrapperMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<Core.ImageService>> _loggerMock;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private string _url = "https://fakestorageaccount.blob.core.windows.net/container/blob-name";
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    private readonly Core.ImageService _imageService;

    public ImageServiceTests()
    {
        _blobContainerWrapperMock = new Mock<IBlobContainerWrapper>();
        _configurationMock = new Mock<IConfiguration>();
        _streamManager = new RecyclableMemoryStreamManager();
        _loggerMock = new Mock<ILogger<Core.ImageService>>();

        _imageService = new Core.ImageService(
            _blobContainerWrapperMock.Object,
            _streamManager,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UploadImageAsync_ShouldUploadAndReturnMetadata_WhenValidImage()
    {
        // Arrange
        var formFile = await FormFileFactory.CreateFakeImageFormFileAsync();

        _configurationMock.Setup(c => c["SupportedImageExtensions"]).Returns("jpg,png");
        _configurationMock.Setup(c => c["MaxImageSizeInBytes"]).Returns("1073741824");

        var blobClientMock = new Mock<BlockBlobClient>();
        blobClientMock.Setup(b =>
            b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        _blobContainerWrapperMock
            .Setup(c => c.GetBlockBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        blobClientMock
            .SetupGet(b => b.Uri)
            .Returns(new Uri(_url));

        // Act
        var result = await _imageService.UploadImageAsync(formFile, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Success");
        result.Url.Should().Be(_url);
        result.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrow_WhenExtensionNotSupported()
    {
        // Arrange
        var formFile = await FormFileFactory.CreateFakeImageFormFileAsync(fileExtension: "exe");

        _configurationMock.Setup(c => c["SupportedImageExtensions"]).Returns("jpg,png");

        // Act
        Func<Task> act = async () => await _imageService.UploadImageAsync(formFile, _cancellationToken);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
                 .WithMessage("*Unsupported image extension*");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrow_WhenFileSizeTooLarge()
    {
        // Arrange
        var formFile = await FormFileFactory.CreateFakeImageFormFileAsync();

        _configurationMock.Setup(c => c["SupportedImageExtensions"]).Returns("jpg,png");
        _configurationMock.Setup(c => c["MaxImageSizeInBytes"]).Returns("1");

        // Act
        Func<Task> act = async () => await _imageService.UploadImageAsync(formFile, _cancellationToken);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
                 .WithMessage("*File too large*");
    }

    [Fact]
    public async Task GetImagePathAsync_ShouldReturnUrl_WhenBlobExists()
    {
        // Arrange
        string id = "existing-image";
        var blobMock = new Mock<BlockBlobClient>();
        blobMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        blobMock.Setup(b => b.Uri).Returns(new Uri(_url));

        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(id)).Returns(blobMock.Object);

        // Act
        var result = await _imageService.GetImagePathAsync(id, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Success");
        result.Id.Should().Be(id);
        result.Url.Should().Be(_url);
    }

    [Fact]
    public async Task GetImagePathAsync_ShouldThrow_WhenBlobNotExists()
    {
        // Arrange
        string id = "missing";
        var blobMock = new Mock<BlockBlobClient>();
        blobMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(id)).Returns(blobMock.Object);

        // Act
        Func<Task> act = async () => await _imageService.GetImagePathAsync(id, _cancellationToken);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
                 .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteImagesAsync_ShouldDeleteFiles_WhenFilesExist()
    {
        // Arrange
        string id = "test-id";
        var blobNames = new[] { "test-id", "test-id_160" };

        var blobItems = blobNames.Select(name =>
            BlobsModelFactory.BlobItem(name: name)).ToList();

        var pageable = TestAsyncPageable<BlobItem>.Create(blobItems);

        _blobContainerWrapperMock
            .Setup(c => c.GetBlobsAsync(It.Is<string>(p => p == id), default))
            .Returns(pageable);

        foreach (var blobName in blobNames)
        {
            var blockBlobClientMock = new Mock<BlockBlobClient>();
            blockBlobClientMock
                .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), null, default))
                .ReturnsAsync(Response.FromValue(true, null));

            _blobContainerWrapperMock
                .Setup(c => c.GetBlockBlobClient(blobName))
                .Returns(blockBlobClientMock.Object);
        }

        // Act
        var result = await _imageService.DeleteImagesAsync(id, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Message.Should().Be("Success");

        _blobContainerWrapperMock.Verify(c => c.GetBlockBlobClient(It.IsAny<string>()), Times.Exactly(blobNames.Length));
    }

    [Fact]
    public async Task DeleteImagesAsync_ShouldThrow_WhenNothingDeleted()
    {
        // Arrange
        var id = "nonexistent-image-id";

        _blobContainerWrapperMock
            .Setup(c => c.GetBlobsAsync(
                id,
                It.IsAny<CancellationToken>()
                ))
            .Returns(TestAsyncPageable<BlobItem>.Create(new List<BlobItem>()));

        // Act
        var act = async () => await _imageService.DeleteImagesAsync(id, _cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<BusinessValidationException>()
            .WithMessage("Image not found.");
    }

    [Fact]
    public async Task ResizeAndSaveAsync_ShouldReturnUrl_WhenSuccessful()
    {
        // Arrange
        var id = "test-id";
        var targetHeight = 100;
        var originalBlobName = id;
        var resizedBlobName = $"{id}_{targetHeight}";
        _url = $"{_url}_{targetHeight}";

        var resizedBlobClientMock = new Mock<BlockBlobClient>();
        resizedBlobClientMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, null));

        var originalBlobClientMock = new Mock<BlockBlobClient>();
        originalBlobClientMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, null));

        var fakeImage = new Image<Rgba32>(200, 200);
        var stream = new MemoryStream();
        fakeImage.SaveAsJpeg(stream);
        stream.Position = 0;

        var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: stream, contentType: "image/jpeg");
        originalBlobClientMock.Setup(b => b.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(downloadInfo, null));

        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(originalBlobName)).Returns(originalBlobClientMock.Object);
        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(resizedBlobName)).Returns(resizedBlobClientMock.Object);

        resizedBlobClientMock
            .SetupGet(b => b.Uri)
            .Returns(new Uri(_url));

        // Act
        var result = await _imageService.ResizeAndSaveAsync(id, targetHeight, _cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Url.Should().Be(_url);
        result.Message.Should().Be("Success");
    }

    [Fact]
    public async Task ResizeAndSaveAsync_ShouldThrow_WhenOriginalBlobNotFound()
    {
        // Arrange
        var id = "missing-id";
        var targetHeight = 100;

        var originalBlobMock = new Mock<BlockBlobClient>();
        originalBlobMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, null));

        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient($"{id}_{targetHeight}")).Returns(originalBlobMock.Object);
        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(id)).Returns(originalBlobMock.Object);

        // Act
        Func<Task> act = async () => await _imageService.ResizeAndSaveAsync(id, targetHeight, _cancellationToken);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("Original image not found.");
    }

    [Fact]
    public async Task ResizeAndSaveAsync_ShouldThrow_WhenTargetHeightExceedsOriginal()
    {
        // Arrange
        var id = "test-id";
        var targetHeight = 1000; // Greater than image height
        var originalBlobName = id;
        var resizedBlobName = $"{id}_{targetHeight}";

        var resizedBlobClientMock = new Mock<BlockBlobClient>();
        resizedBlobClientMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, null));

        var originalBlobClientMock = new Mock<BlockBlobClient>();
        originalBlobClientMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, null));

        var fakeImage = new Image<Rgba32>(200, 200); // height = 200
        var stream = new MemoryStream();
        fakeImage.SaveAsJpeg(stream);
        stream.Position = 0;

        var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: stream, contentType: "image/jpeg");
        originalBlobClientMock.Setup(b => b.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(downloadInfo, null));

        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(originalBlobName)).Returns(originalBlobClientMock.Object);
        _blobContainerWrapperMock.Setup(c => c.GetBlockBlobClient(resizedBlobName)).Returns(resizedBlobClientMock.Object);

        // Act
        Func<Task> act = async () => await _imageService.ResizeAndSaveAsync(id, targetHeight, _cancellationToken);

        // Assert
        await act.Should().ThrowAsync<TargetHeightExceededException>();
    }
}