using ClosedXML.Excel;
using Common.Domain.Entities;
using Common.Domain.Enums;
using Common.Domain.Interfaces;
using Common.Messages;
using DataProcessor.Application.Features.CargaMasiva.Commands;
using FileIngestor.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Application.Features.CargaMasiva.Handlers
{
    public class ProcessFileCommandHandler : IRequestHandler<ProcessFileCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileDownloadService _fileDownloadService;
        private readonly ILogger<ProcessFileCommandHandler> _logger;
        private readonly IMessagePublisher _messagePublisher;

        public ProcessFileCommandHandler(
            IApplicationDbContext context,
            IFileDownloadService fileDownloadService,
            ILogger<ProcessFileCommandHandler> logger,
            IMessagePublisher messagePublisher)
        {
            _context = context;
            _fileDownloadService = fileDownloadService;
            _logger = logger;
            _messagePublisher = messagePublisher;
        }

        public async Task<bool> Handle(ProcessFileCommand request, CancellationToken cancellationToken)
        {
            var carga = await _context.CargaArchivos
                .FirstOrDefaultAsync(c => c.Id == request.CargaArchivoId, cancellationToken);

            if (carga == null)
            {
                _logger.LogWarning("CargaArchivo {CargaId} no existe. Se ignora.", request.CargaArchivoId);
                return true;
            }

            if (carga.Estado == EstadoCarga.Finalizado)
            {
                _logger.LogInformation("Carga {CargaId} ya fue procesada. Se ignora.", carga.Id);
                return true;
            }

            _logger.LogInformation("Procesamiento de Carga ID {Id} iniciado.", carga.Id);

            try
            {
                // Validación de período duplicado
                var existeOtraCarga = await _context.CargaArchivos.AnyAsync(c =>
                    c.Periodo == carga.Periodo &&
                    c.Id != carga.Id &&
                    c.Estado != EstadoCarga.Error,
                    cancellationToken);

                if (existeOtraCarga)
                {
                    carga.Estado = EstadoCarga.Error;
                    carga.MensajeError = "Ya existe una carga para el período";
                    await _context.SaveChangesAsync(cancellationToken);
                    PublicarNotificacion(carga, conErrores: true);
                    return true;
                }

                carga.Estado = EstadoCarga.EnProceso;
                await _context.SaveChangesAsync(cancellationToken);

                ////  BLOQUE DE PRUEBA DIRECTO
                //var testEntity = new DataProcesada
                //{
                //    CodigoProducto = "TEST001",
                //    NombreProducto = "Producto de prueba",
                //    Precio = 123.45m,
                //    Cantidad = 10,
                //    Periodo = carga.Periodo,
                //    CargaArchivoId = carga.Id,
                //    FechaCreacion = DateTime.UtcNow
                //};

                //_context.DataProcesadas.Add(testEntity);
                //await _context.SaveChangesAsync(cancellationToken);

                //_logger.LogInformation("Registro de prueba insertado en DataProcesada con IdCarga {CargaId}", carga.Id);
                

                using var fileStream = await _fileDownloadService.DownloadAsync(carga.FileKey, cancellationToken);
                if (fileStream == null)
                    throw new Exception($"No se pudo descargar el archivo con FileKey: {carga.FileKey}");

                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1);

                var existentes = await _context.DataProcesadas
                    .Select(p => new { p.CodigoProducto, p.Periodo, p.CargaArchivoId })
                    .ToListAsync(cancellationToken);

                var existentesSet = existentes
                    .Select(e => $"{e.CodigoProducto}-{e.Periodo}-{e.CargaArchivoId}")
                    .ToHashSet();

                var nuevasEntidades = new List<DataProcesada>();

                foreach (var row in rows)
                {
                    if (row.CellsUsed().All(c => c.IsEmpty())) continue;

                    var codigoProducto = row.Cell(2).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(codigoProducto))
                    {
                        _logger.LogWarning("Fila {RowNumber}: Producto sin código, se omite.", row.RowNumber());
                        continue;
                    }

                    var nombreProducto = row.Cell(3).GetString() ?? "SIN NOMBRE";

                    decimal precio = row.Cell(4).TryGetValue(out decimal p) ? p : 0m;
                    int cantidad = row.Cell(5).TryGetValue(out int c) ? c : 0;
                    var periodo = row.Cell(6).GetString()?.Trim() ?? carga.Periodo;

                    var key = $"{codigoProducto}-{periodo}-{carga.Id}";

                    if (existentesSet.Contains(key))
                    {
                        _logger.LogWarning("Fila {RowNumber}: Producto '{Codigo}' ya existe en periodo {Periodo}, carga {CargaId}. Se omite.",
                            row.RowNumber(), codigoProducto, periodo, carga.Id);
                        continue;
                    }

                    var entidad = new DataProcesada
                    {
                        CodigoProducto = codigoProducto,
                        NombreProducto = nombreProducto,
                        Precio = precio,
                        Cantidad = cantidad,
                        Periodo = periodo,
                        CargaArchivoId = carga.Id,
                        FechaCreacion = DateTime.UtcNow
                    };

                    nuevasEntidades.Add(entidad);
                    existentesSet.Add(key);
                }

                if (nuevasEntidades.Any())
                {
                    _context.DataProcesadas.AddRange(nuevasEntidades);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Se insertaron {Count} nuevas entidades en DataProcesada.", nuevasEntidades.Count);
                }
                else
                {
                    _logger.LogInformation("No se encontraron nuevas entidades válidas para insertar en DataProcesada.");
                }

                carga.Estado = EstadoCarga.Finalizado;
                carga.FechaFin = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                PublicarNotificacion(carga, conErrores: false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al procesar la carga {CargaId}", carga.Id);

                carga.Estado = EstadoCarga.Error;
                carga.MensajeError = ex.Message;
                await _context.SaveChangesAsync(cancellationToken);

                PublicarNotificacion(carga, conErrores: true);
                return false;
            }
        }

        private void PublicarNotificacion(CargaArchivo carga, bool conErrores)
        {
            var jobFinishedEvent = new JobFinishedEvent
            {
                CargaArchivoId = carga.Id,
                UsuarioEmail = carga.Usuario,
                FechaFin = carga.FechaFin ?? DateTime.UtcNow,
                ConErrores = conErrores
            };

            _logger.LogInformation("Publicando JobFinishedEvent para Carga ID {CargaId}. Con Errores: {ConErrores}", carga.Id, conErrores);

            _messagePublisher.Publish(
                exchangeName: "notifications.exchange",
                routingKey: "notificaciones",
                message: jobFinishedEvent);
        }
    }
}

