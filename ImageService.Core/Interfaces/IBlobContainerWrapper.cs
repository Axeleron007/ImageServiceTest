using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace ImageService.Core.Interfaces;

public interface IBlobContainerWrapper
{
    BlockBlobClient GetBlockBlobClient(string blobName);
    AsyncPageable<BlobItem> GetBlobsAsync(string prefix = null, CancellationToken cancellationToken = default);
}
