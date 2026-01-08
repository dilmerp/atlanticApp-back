using System.Collections.Generic;
using System.Threading.Tasks;


namespace Common.Domain.Interfaces
{
    // Definimos la interfaz genérica base con operaciones CRUD asíncronas
    public interface IGenericRepository<TEntity, TId> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(TId id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity> AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
    }
}
