namespace Common.Domain.Interfaces
{
    /// <summary>
    /// Interfaz que combina la interfaz IGenericRepository con un tipo de ID fijo (int) 
    /// para usarlo en la capa de Application/CQRS.
    /// Esto resuelve el error original en el CommandHandler.
    /// </summary>
    public interface IRepositoryAsync<TEntity> : IGenericRepository<TEntity, int> where TEntity : class
    {
        // Aquí puede agregar métodos específicos para todos los repositorios si los necesita.
        // Por ahora, solo extiende el repositorio genérico con TId = int.
    }
}