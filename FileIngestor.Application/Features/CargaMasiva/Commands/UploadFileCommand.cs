using Microsoft.AspNetCore.Http;
using MediatR;

namespace FileIngestor.Application.Features.CargaMasiva.Commands
{

    public record UploadFileCommand(
            IFormFile File,
            string Usuario,
            string Periodo
     ) : IRequest<CargaResponseDto>;

    // Definimos la estructura de lo que queremos devolver al controlador
    public class CargaResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}