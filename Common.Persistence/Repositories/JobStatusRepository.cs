using Common.Domain.Entities;
using Common.Domain.Enums;
using Common.Domain.Interfaces;
using Common.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Common.Persistence.Repositories
{
    // JobStatusRepository implementa la interfaz IJobStatusRepository
    public class JobStatusRepository : IJobStatusRepository
    {
        private readonly AppDbContext _context;

        public JobStatusRepository(AppDbContext context)
        {
            _context = context;
        }

        // Implementación 1: Creación inicial del registro (Estado: Pendiente)
        public async Task CreateInitialJobAsync(CargaArchivo job, CancellationToken cancellationToken)
        {
            _context.CargaArchivos.Add(job);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Implementación 2: Validación de Duplicidad por Periodo 
        public async Task<CargaArchivo?> GetActiveJobByPeriodAsync(string periodo, CancellationToken cancellationToken)
        {
            // Valida Si existe una carga previa 
            return await _context.CargaArchivos
                .Where(j => j.Periodo == periodo)
                .Where(j => j.Estado != EstadoCarga.Error) 
                .OrderByDescending(j => j.FechaRegistro) 
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Implementación 3: Actualización de Estado (Usado por todos los Workers/APIs)
        public async Task UpdateStatusAsync(
            Guid jobId,
            EstadoCarga newStatus,
            int? processedRows = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            var job = await _context.CargaArchivos.FindAsync(new object[] { jobId }, cancellationToken);

            if (job == null)
            {
                return;
            }

            job.Estado = newStatus;

            if (newStatus == EstadoCarga.Finalizado || newStatus == EstadoCarga.Error)
            {
                job.FechaFin = DateTime.UtcNow;        
                job.MensajeError = errorMessage;       
            }

            _context.CargaArchivos.Update(job);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}