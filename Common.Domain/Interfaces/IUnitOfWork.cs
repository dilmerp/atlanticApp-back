using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Domain.Interfaces
{
    /// <summary>
    /// Contrato para la Unidad de Trabajo, que gestiona transacciones y el guardado de cambios.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Guarda todos los cambios pendientes en el contexto de la base de datos.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Métodos para el control explícito de transacciones, usados por el TransactionBehavior

        /// <summary>
        /// Inicia una nueva transacción de base de datos.
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirma la transacción actual y guarda los cambios.
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deshace la transacción actual.
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}