using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DataProcessor.Infrastructure.Services
{
    public class SeaweedFsDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly string _volumeUrl; 

        public SeaweedFsDownloadService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _volumeUrl = configuration["SeaweedFs:VolumeUrl"]
                ?? throw new ArgumentNullException("SeaweedFs:VolumeUrl no está configurado.");
        }

        public async Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken)
        {
            // fileKey es el fid (ejemplo: "5,036f79f5f0")
            var response = await _httpClient.GetAsync($"{_volumeUrl}/{fileKey}", cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}