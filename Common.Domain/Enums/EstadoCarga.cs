namespace Common.Domain.Enums
{
    /// <summary>
    /// Define los estados del ciclo de vida de un trabajo de carga masiva.
    /// </summary>
    public enum EstadoCarga
    {
        Pendiente = 0,      // Inicial: Esperando ser consumido por DataProcessor
        EnProceso = 1,      // DataProcessor ha iniciado la descarga/procesamiento
        Validado = 2,       // (Opcional) La validación del Excel ha terminado (antes de la inserción)
        Finalizado = 3,     // Inserción de datos completada exitosamente (Worker terminó)
        Notificado = 4,     // Correo de notificación enviado al usuario (Notification Worker terminó)
        Error = 5           // Proceso fallido (puede ser en FileIngestor, DataProcessor, o Notification)
    }
}