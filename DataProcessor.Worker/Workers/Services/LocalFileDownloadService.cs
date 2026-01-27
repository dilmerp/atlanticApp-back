using Common.Domain.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Worker
{
    public class LocalFileDownloadService : IFileDownloadService
    {
        public Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken)
        {
            var uploadsFolder = "/app/uploads";
            var filePath = Path.Combine(uploadsFolder, fileKey);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"No se encontró el archivo con clave {fileKey} en {uploadsFolder}");
            }

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream>(stream);
        }
    }
}
