using System;

namespace Common.Domain.Exceptions
{
    /// <summary>
    /// Clase base abstracta para todas las excepciones de Reglas de Negocio/Dominio.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }

        // Constructor requerido para la serialización (útil para el middleware de errores)
        protected DomainException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}