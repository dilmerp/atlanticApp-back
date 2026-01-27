using FileIngestor.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileIngestor.Infrastructure.Services
{
    public class FileStorageService : IFileUploadService
    {
        private readonly ILogger<FileStorageService> _logger;
        public FileStorageService(ILogger<FileStorageService> logger)
        {
            _logger = logger;
        }
        public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
        {
            
            var uploadsFolder = "/app/uploads";
            Directory.CreateDirectory(uploadsFolder);

            var fileKey = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, fileKey);

            _logger.LogInformation("Guardando archivo en: {Path}", filePath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return fileKey;
        }
    }
}