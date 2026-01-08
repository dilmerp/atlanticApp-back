using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileIngestor.Application.Exceptions
{
    /// <summary>
    /// Excepción personalizada utilizada para encapsular errores de validación
    /// generados por FluentValidation. Esta excepción es lanzada por ValidationBehavior
    /// y debe ser capturada por el ExceptionHandlingMiddleware en la capa API para
    /// devolver un 400 Bad Request estructurado.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Almacena un diccionario de errores de validación, agrupados por nombre de propiedad (campo).
        /// Key: Nombre de la propiedad (ej. "Nombre").
        /// Value: Arreglo de mensajes de error asociados a esa propiedad (ej. ["El nombre es requerido."]).
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException()
            : base("Una o más fallas de validación han ocurrido.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IEnumerable<ValidationFailure> failures)
            : this()
        {
            Errors = failures
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    failureGroup => failureGroup.Key,
                    failureGroup => failureGroup.Select(f => f.ErrorMessage).ToArray());
        }
    }
}
