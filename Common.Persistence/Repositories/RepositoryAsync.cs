//using Common.Domain.Entities;
using Common.Domain.Interfaces;
using Common.Persistence.Data;

namespace Common.Persistence.Repositories
{
    /// <summary>
    /// Implementación concreta del Repositorio Asíncrono.
    /// Esta clase existe para:
    /// 1. Implementar la interfaz IRepositoryAsync<TEntity> que espera el CommandHandler.
    /// 2. Heredar de GenericRepository<TEntity, int>, fijando el tipo de ID a 'int'.
    /// </summary>
    /// <typeparam name="TEntity">La entidad de dominio.</typeparam>
    public class RepositoryAsync<TEntity> : GenericRepository<TEntity, int>, IRepositoryAsync<TEntity> where TEntity : class
    {
        
        public RepositoryAsync(AppDbContext context) : base(context)
        {
            // GenericRepository.
        }
    }
}