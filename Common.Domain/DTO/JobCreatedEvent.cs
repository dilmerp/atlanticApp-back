using System;
using System.Text.Json.Serialization; 

namespace Common.Messages 
{
    /// <summary>
    /// Define el evento publicado a RabbitMQ después de que un archivo ha sido subido 
    /// y está listo para ser procesado por el Worker Service.
    /// Se utiliza como 'record' para inmutabilidad y concisión.
    /// </summary>
    public record JobCreatedEvent(
        
        int CargaArchivoId,
        string FileKey,
        string FileName,
        string UserEmail,
        long FileSizeInBytes
    )
    {
        
        
    }
}