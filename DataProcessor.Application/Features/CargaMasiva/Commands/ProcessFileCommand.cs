using MediatR;
using System;

namespace DataProcessor.Application.Features.CargaMasiva.Commands
{
    /// <summary>
    /// Comando que representa la solicitud para procesar un archivo de carga masiva,
    /// iniciado por un mensaje de RabbitMQ.
    /// </summary>
    public class ProcessFileCommand : IRequest<bool>
    {
        public int CargaArchivoId { get; set; }
        public string FileKey { get; set; }
    }
}