using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cocobot.Persistance
{
    internal class AzureMediaRepository : IMediaRepository
    {
        private readonly BlobContainerClient _container;

        public AzureMediaRepository(IConfiguration config)
        {
            var connectionString = config.GetValue<string>("MediaStorage:ConnectionString");
            if (connectionString == null)
                throw new Exception("No connection string configured for media storage.");

            var containerName = config.GetValue<string>("MediaStorage:ContainerName");
            if (containerName == null)
                throw new Exception("No connection string configured for media storage.");

            this._container = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<BlobDownloadStreamingResult> Download(string key, CancellationToken cancellationToken)
        {
            var blob = this._container.GetBlobClient(key);
            var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value;
        }

        public async Task<Uri> GetUri(string key)
        {
            var blob = this._container.GetBlobClient(key);
            return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow + TimeSpan.FromHours(24));
        }

        public async Task Upload(string key, string contentType, Stream stream, CancellationToken cancellationToken)
        {
            var blob = this._container.GetBlobClient(key);
            await blob.DeleteIfExistsAsync();
            _ = await blob.UploadAsync(stream,
                                 httpHeaders: new BlobHttpHeaders() { ContentType = contentType },
                                 transferOptions: new StorageTransferOptions { MaximumTransferSize = 1_048_576 },
                                 cancellationToken: cancellationToken);
        }

        public async Task Delete(string key)
        {
            var blob = this._container.GetBlobClient(key);
            _ = await blob.DeleteIfExistsAsync();
        }
    }
}
