using FileIngestor.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

//namespace FileIngestor.Infrastructure.Services
namespace DataProcessor.Worker
{
    public class SeaweedFsStorageService : IFileUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly string _masterUrl; 

        public SeaweedFsStorageService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _masterUrl = configuration["SeaweedFs:MasterUrl"]
                ?? throw new ArgumentNullException("SeaweedFs:MasterUrl no está configurado.");
        }

        public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
        {
            // 1. Obtener una clave de escritura (FileKey/URL) del SeaweedFS Master
            var assignResponse = await _httpClient.GetFromJsonAsync<AssignResponse>(
                $"{_masterUrl}/dir/assign",
                cancellationToken);

            if (assignResponse == null || string.IsNullOrEmpty(assignResponse.Url))
            {
                throw new InvalidOperationException("Fallo al obtener la clave de SeaweedFS Master.");
            }

            // 2. Subir el archivo al Volume Server asignado
            var volumeServerUrl = $"http://{assignResponse.Url}/{assignResponse.Fid}";

            using var content = new MultipartFormDataContent();
            // Agregar el archivo como StreamContent para eficiencia
            content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);

            var uploadResponse = await _httpClient.PostAsync(volumeServerUrl, content, cancellationToken);
            uploadResponse.EnsureSuccessStatusCode();

            // La clave que se almacena y se envía a la cola es el FID.
            return assignResponse.Fid;
        }

        // DTOs internos para el servicio
        private record AssignResponse(string Fid, string Url, long PublicUrl);
    }
}