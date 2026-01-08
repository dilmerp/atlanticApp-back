using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

public interface IFileUploadService
{
    Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken);
}
