using Microsoft.AspNetCore.Http;
using MediatR;

namespace FileIngestor.Application.Features.CargaMasiva.Commands
{
    
    public record UploadFileCommand(
        IFormFile File,
        string Usuario,
        string Periodo
    ) : IRequest<bool>;
}