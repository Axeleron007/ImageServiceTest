using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ImageService.Core.Interfaces;

namespace ImageService.Core;

public class BlobContainerWrapper : IBlobContainerWrapper
{
    private readonly BlobContainerClient _containerClient;

    public BlobContainerWrapper(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public BlockBlobClient GetBlockBlobClient(string blobName)
    {
        return _containerClient.GetBlockBlobClient(blobName);
    }

    public AsyncPageable<BlobItem> GetBlobsAsync(string prefix = null, CancellationToken cancellationToken = default)
    {
        return _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
    }
}
