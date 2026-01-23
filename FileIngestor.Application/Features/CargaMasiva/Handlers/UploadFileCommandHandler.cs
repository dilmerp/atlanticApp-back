using Common.Domain.Entities;
using Common.Domain.Enums;
using Common.Domain.Exceptions;
using Common.Domain.Interfaces;
using Common.Messages;
using FileIngestor.Application.Features.CargaMasiva.Commands;
using FileIngestor.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed; // IMPORTANTE: Para Redis
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileIngestor.Application.Features.CargaMasiva.Handlers
{
    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, CargaResponseDto>
    {
        private readonly IJobStatusRepository _jobStatusRepository;
        private readonly IFileUploadService _fileStorageService;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IDistributedCache _cache; // Inyectamos el caché

        public UploadFileCommandHandler(
            IJobStatusRepository jobStatusRepository,
            IFileUploadService fileStorageService,
            IMessagePublisher messagePublisher,
            IDistributedCache cache) // Añadimos al constructor
        {
            _jobStatusRepository = jobStatusRepository;
            _fileStorageService = fileStorageService;
            _messagePublisher = messagePublisher;
            _cache = cache;
        }

        public async Task<CargaResponseDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
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
                Estado = EstadoCarga.Pendiente,
                MensajeError = string.Empty
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

            // --- 5. INVALIDACIÓN DE CACHÉ DE REDIS ---
            // IMPORTANTE: La llave debe ser idéntica a la usada en el QueryHandler
            // Al borrarla, forzamos a que el historial se refresque en la siguiente consulta
            string cacheKey = "carga_historial_completo";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            // --- 6. RETORNO DE METADATOS PARA EL FRONTEND ---
            return new CargaResponseDto
            {
                Id = newJob.Id,
                Status = newJob.Estado.ToString(),
                Success = true
            };
        }
    }
}