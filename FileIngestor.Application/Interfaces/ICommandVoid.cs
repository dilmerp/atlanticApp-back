using MediatR;

namespace FileIngestor.Application.Interfaces
{
    /// <summary>
    /// Marca un Command de MediatR que NO devuelve un valor específico 
    /// (representa una operación Void).
    /// </summary>
    // Hereda de IRequest<Unit> para cumplir con la firma de MediatR, 
    // donde 'Unit' es el tipo que MediatR usa para operaciones sin retorno.
    public interface ICommandVoid : IRequest<Unit>
    {
    }
}