using Common.Domain.Interfaces;
using Common.Persistence.Data; // IMPORTANTE: Asegúrate de que esta sea la ruta correcta a tu AppDbContext
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Persistence.Repositories
{
    /// <summary>
    /// Implementación genérica del repositorio que maneja las operaciones CRUD básicas
    /// para cualquier entidad que herede de IGenericRepository.
    /// </summary>
    /// <typeparam name="TEntity">El tipo de la entidad (ej: Producto, Categoria).</typeparam>
    /// <typeparam name="TId">El tipo del identificador de la entidad (ej: int, Guid).</typeparam>
    public class GenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId> where TEntity : class
    {
        // Campo protegido para la instancia del contexto de la base de datos (Entity Framework Core)
        protected readonly AppDbContext _dbContext;

        /// <summary>
        /// Constructor que recibe el contexto de base de datos a través de Inyección de Dependencias.
        /// </summary>
        public GenericRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Agrega una nueva entidad y guarda los cambios de forma asíncrona.
        /// </summary>
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Elimina una entidad y guarda los cambios de forma asíncrona.
        /// </summary>
        public async Task DeleteAsync(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Obtiene todas las entidades del tipo TEntity.
        /// </summary>
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbContext.Set<TEntity>().ToListAsync();
        }

        /// <summary>
        /// Busca y obtiene una entidad por su identificador único (TId).
        /// </summary>
        public async Task<TEntity> GetByIdAsync(TId id)
        {
            // Usamos FindAsync que está optimizado para buscar por clave primaria
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

        /// <summary>
        /// Marca la entidad como modificada y guarda los cambios de forma asíncrona.
        /// </summary>
        public async Task UpdateAsync(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
