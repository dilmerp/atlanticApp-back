namespace Common.Domain.Exceptions
{
    // Usada cuando ya existe una carga activa (Pendiente/EnProceso) para el mismo periodo. (409 Conflict)
    public class PeriodoInProcessException : DomainException
    {
        public PeriodoInProcessException(string message) : base(message) { }

        protected PeriodoInProcessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}