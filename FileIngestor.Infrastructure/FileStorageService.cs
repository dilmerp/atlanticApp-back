using FileIngestor.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileIngestor.Infrastructure.Services
{
    public class FileStorageService : IFileUploadService
    {
        public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
        {
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileKey = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, fileKey);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return fileKey;
        }
    }
}