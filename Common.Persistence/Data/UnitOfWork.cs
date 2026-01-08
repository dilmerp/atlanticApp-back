using FileIngestor.Application.Interfaces;
using Common.Domain.Interfaces;
using Common.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Persistence
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork>? _logger;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(AppDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                _logger?.LogDebug(">>> TRx START: Nueva transacción iniciada (UnitOfWork).");
            }
            else
            {
                _logger?.LogDebug("BeginTransactionAsync called but a transaction is already active.");
            }
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                _logger?.LogDebug("CommitTransactionAsync called but there is no active transaction.");
                return;
            }

            try
            {
                await SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);

                _logger?.LogDebug("<<< TRx COMMIT: Transacción confirmada (UnitOfWork).");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during CommitTransactionAsync - rolling back.");
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                _logger?.LogWarning("XXX TRx ROLLBACK: Iniciando rollback (UnitOfWork).");
                try
                {
                    await _currentTransaction.RollbackAsync(cancellationToken);
                    _logger?.LogInformation("XXX TRx ROLLBACK: Rollback completado (UnitOfWork).");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "RollbackTransactionAsync failed.");
                    throw;
                }
                finally
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await RollbackTransactionAsync();
                }

                await _context.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while disposing UnitOfWork asynchronously.");
                throw;
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}