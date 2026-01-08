using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FileIngestor.Application.DTO
{
    public class UploadFileDto
    {
        // Archivo a subir
        [Required]
        public IFormFile File { get; set; }

        // Periodo (ej. "2026-01")
        [Required]
        [StringLength(10)]
        public string Periodo { get; set; }
    }
}

