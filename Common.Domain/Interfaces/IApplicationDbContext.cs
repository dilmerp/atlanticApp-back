using Common.Domain.Entities; //  Importar la entidad CargaArchivo
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Domain.Interfaces
{
    /// <summary>
    /// Contrato que define la interacción de las capas externas con el contexto de la base de datos.
    /// Desacopla las capas superiores (Domain, Application) de la implementación concreta (Entity Framework Core).
    /// </summary>
    public interface IApplicationDbContext
    {
        // ----------------------------------------------------------------------
        // 1. DbSets para las entidades que quieres exponer y manejar
        // ----------------------------------------------------------------------

        /// <summary>
        /// Acceso a la colección (tabla) de CargaArchivo para el seguimiento de trabajos.
        /// </summary>
        DbSet<CargaArchivo> CargaArchivos { get; set; } 
        DbSet<DataProcesada> DataProcesadas { get; set; }

        // ----------------------------------------------------------------------
        // 2. Método para persistir los cambios
        // ----------------------------------------------------------------------

        /// <summary>
        /// Guarda todos los cambios realizados en el contexto de forma asíncrona.
        /// Implementa de forma asíncrona la funcionalidad de SaveChanges de DbContext.
        /// </summary>
        /// <param name="cancellationToken">Token para la cancelación asíncrona.</param>
        /// <returns>Número de objetos escritos en la base de datos.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}