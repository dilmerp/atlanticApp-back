using Common.Domain.Entities;
using Common.Domain.Enums;
using Common.Domain.Exceptions;
using Common.Domain.Interfaces;
using Common.Messages;
using FileIngestor.Application.Features.CargaMasiva.Commands;
using FileIngestor.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileIngestor.Application.Features.CargaMasiva.Handlers
{
    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, bool>
    {
        private readonly IJobStatusRepository _jobStatusRepository;
        private readonly IFileUploadService _fileStorageService;
        private readonly IMessagePublisher _messagePublisher;

        public UploadFileCommandHandler(
            IJobStatusRepository jobStatusRepository,
            IFileUploadService fileStorageService,
            IMessagePublisher messagePublisher)
        {
            _jobStatusRepository = jobStatusRepository;
            _fileStorageService = fileStorageService;
            _messagePublisher = messagePublisher;
        }

        public async Task<bool> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            // --- 1. VALIDACIÓN DE DUPLICIDAD POR PERIODO ---
            var existingJob = await _jobStatusRepository.GetActiveJobByPeriodAsync(
                request.Periodo,
                cancellationToken);

            if (existingJob != null)
            {
                if (existingJob.Estado == EstadoCarga.EnProceso || existingJob.Estado == EstadoCarga.Pendiente)
                {
                    throw new PeriodoInProcessException(
                        $"Ya existe una carga para el periodo '{request.Periodo}' en estado {existingJob.Estado}.");
                }

                if (existingJob.IsInProcessOrCompleted)
                {
                    throw new PeriodoDuplicatedException(
                        $"Ya existe una carga finalizada para el periodo '{request.Periodo}'. Carga rechazada.");
                }
            }

            // --- 2. ALMACENAMIENTO DEL ARCHIVO ---
            var fileKey = await _fileStorageService.SaveFileAsync(request.File, cancellationToken);

            // --- 3. CREACIÓN DE REGISTRO INICIAL ---
            var newJob = new CargaArchivo
            {
                Usuario = request.Usuario,
                NombreArchivo = request.File.FileName,
                FileKey = fileKey,
                Periodo = request.Periodo, 
                FechaRegistro = DateTime.UtcNow,
                Estado = EstadoCarga.Pendiente
            };

            await _jobStatusRepository.CreateInitialJobAsync(newJob, cancellationToken);

            // --- 4. PUBLICACIÓN DEL EVENTO ---
            var jobEvent = new JobCreatedEvent(
                CargaArchivoId: newJob.Id,
                FileKey: newJob.FileKey,
                FileName: newJob.NombreArchivo,
                UserEmail: newJob.Usuario,
                FileSizeInBytes: request.File.Length);

            await _messagePublisher.PublishJobCreatedEventAsync(jobEvent);

            return true;
        }
    }
}