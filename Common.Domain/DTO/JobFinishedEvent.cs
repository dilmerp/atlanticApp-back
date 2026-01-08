using System;

namespace Common.Messages 
{
    // Este DTO se publica cuando la carga masiva ha terminado (éxito o error).
    public class JobFinishedEvent
    {
        /// <summary>
        /// Identificador de la tabla CargaArchivo. Usado para actualizar el estado final.
        /// </summary>
        public int CargaArchivoId { get; set; }

        /// <summary>
        /// Correo electrónico del usuario que inició la carga. Usado para la notificación.
        /// </summary>
        public string? UsuarioEmail { get; set; }

        /// <summary>
        /// Fecha y hora en que finalizó el procesamiento.
        /// </summary>
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Indica si el procesamiento terminó con errores de datos.
        /// </summary>
        public bool ConErrores { get; set; }
    }
}