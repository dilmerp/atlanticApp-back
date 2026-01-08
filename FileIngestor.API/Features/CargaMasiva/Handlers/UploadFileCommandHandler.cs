using Common.Domain.Entities;
using Common.Domain.Enums;
using Common.Domain.Interfaces;
using Common.Messages;
using FileIngestor.API.Features.CargaMasiva.Commands;
using FileIngestor.Application.Interfaces; 
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileIngestor.API.Features.CargaMasiva.Handlers
{
    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMessagePublisher _publisher;


        public UploadFileCommandHandler(IApplicationDbContext context, IMessagePublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<int> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            
            var fileKey = Guid.NewGuid().ToString();

            var cargaArchivo = new CargaArchivo
            {
                FileKey = fileKey,
                NombreArchivo = request.File.FileName,
                Usuario = request.UsuarioEmail,
                Periodo = request.Periodo,
                FechaRegistro = DateTimeOffset.UtcNow,
                Estado = EstadoCarga.Pendiente,
                MensajeError = string.Empty,
            };

            _context.CargaArchivos.Add(cargaArchivo);
            await _context.SaveChangesAsync(cancellationToken);

            
            var jobEvent = new JobCreatedEvent(
                CargaArchivoId: cargaArchivo.Id,
                FileKey: cargaArchivo.FileKey,
                FileName: cargaArchivo.NombreArchivo,
                UserEmail: cargaArchivo.Usuario,
                FileSizeInBytes: request.File.Length
            );

            await _publisher.PublishJobCreatedEventAsync(jobEvent);

            return cargaArchivo.Id;
        }
    }
}