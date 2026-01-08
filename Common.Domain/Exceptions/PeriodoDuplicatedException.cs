namespace Common.Domain.Exceptions
{
    // Esta es la clase que el compilador no encuentra.
    // Usada cuando ya existe una carga finalizada exitosamente para el mismo periodo. (403 Forbidden)
    public class PeriodoDuplicatedException : DomainException
    {
        public PeriodoDuplicatedException(string message) : base(message) { }

        // Constructor de serialización requerido si DomainException lo tiene
        protected PeriodoDuplicatedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}