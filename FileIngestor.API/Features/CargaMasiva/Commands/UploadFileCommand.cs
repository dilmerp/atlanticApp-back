using MediatR;
using Microsoft.AspNetCore.Http;

namespace FileIngestor.API.Features.CargaMasiva.Commands 
{
    public class UploadFileCommand : IRequest<int>
    {
        public IFormFile File { get; set; }
        public string UsuarioEmail { get; set; }

        public string Periodo { get; set; }
    }
}