using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cocobot.Persistance
{
    public interface IMediaRepository
    {
        Task Delete(string key);
        Task<Uri> GetUri(string key);
        Task<BlobDownloadStreamingResult> Download(string key, CancellationToken cancellationToken);
        Task Upload(string key, string contentType, Stream stream, CancellationToken cancellationToken);
    }
}
