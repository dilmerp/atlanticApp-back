using System;
using System.Threading.Tasks;

namespace FileIngestor.Application.Interfaces
{
    /// <summary>
    /// Define un contrato universal para el servicio de caché.
    /// Esto permite intercambiar implementaciones (Redis, MemoryCache) fácilmente.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Obtiene un valor de la caché.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a obtener.</typeparam>
        /// <param name="key">Clave de la caché.</param>
        /// <returns>El objeto de la caché o el valor por defecto si no existe.</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Establece un valor en la caché con una expiración definida.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a almacenar.</typeparam>
        /// <param name="key">Clave de la caché.</param>
        /// <param name="value">Valor a almacenar.</param>
        /// <param name="expiration">Tiempo de vida del objeto en caché.</param>
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

        /// <summary>
        /// Elimina un valor de la caché.
        /// </summary>
        /// <param name="key">Clave de la caché.</param>
        Task RemoveAsync(string key);
    }
}
