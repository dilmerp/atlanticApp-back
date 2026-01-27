using MediatR;

namespace FileIngestor.Application.Interfaces
{
    /// <summary>
    /// Marca un Command de MediatR que devuelve un valor (ej: un ID, un DTO de respuesta).
    /// TResponse es el valor que el Command retornará.
    /// </summary>
    // Hereda de IRequest<TResponse>
    public interface ICommand<TResponse> : IRequest<TResponse>
    {
    }
}