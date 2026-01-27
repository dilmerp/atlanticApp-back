using Microsoft.AspNetCore.Http;
using MediatR;

namespace FileIngestor.Application.Features.CargaMasiva.Commands
{

    public record UploadFileCommand(
            IFormFile File,
            string Usuario,
            string Periodo
     ) : IRequest<CargaResponseDto>;

    public class CargaResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}