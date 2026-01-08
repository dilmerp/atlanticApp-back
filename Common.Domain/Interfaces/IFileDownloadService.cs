using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Domain.Interfaces
{
    public interface IFileDownloadService
    {
        Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken);
    }
}
