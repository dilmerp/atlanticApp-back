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
using Microsoft.Extensions.Caching.Distributed; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Application.Features.CargaMasiva.Handlers
{
    public class ProcessFileCommandHandler : IRequestHandler<ProcessFileCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProcessFileCommandHandler> _logger;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IDistributedCache _cache; 

        public ProcessFileCommandHandler(
            IApplicationDbContext context,
            HttpClient httpClient,
            ILogger<ProcessFileCommandHandler> logger,
            IMessagePublisher messagePublisher,
            IDistributedCache cache) 
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
            _messagePublisher = messagePublisher;
            _cache = cache;
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
                carga.Estado = EstadoCarga.EnProceso;
                await _context.SaveChangesAsync(cancellationToken);

                var url = $"http://seaweedfs-volume:8080/{carga.FileKey}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var fileStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1);

                var existentesEnDBList = await _context.DataProcesadas
                    .Select(p => $"{p.CodigoProducto}-{p.Periodo}")
                    .ToListAsync(cancellationToken);

                var existentesEnDBSet = existentesEnDBList.ToHashSet();
                var nuevasEntidades = new List<DataProcesada>();
                var erroresDeRegistro = new List<string>();
                var clavesYaVistasEnEsteArchivo = new HashSet<string>();

                foreach (var row in rows)
                {
                    if (row.CellsUsed().All(c => c.IsEmpty())) continue;

                    var filaNum = row.RowNumber();
                    var codigoProducto = row.Cell(2).GetString()?.Trim();
                    var nombreProducto = row.Cell(3).GetString() ?? "SIN NOMBRE";
                    decimal precio = row.Cell(4).TryGetValue(out decimal p) ? p : 0m;
                    int cantidad = row.Cell(5).TryGetValue(out int c) ? c : 0;
                    var periodo = row.Cell(6).GetString()?.Trim() ?? carga.Periodo;

                    if (string.IsNullOrWhiteSpace(codigoProducto) || string.IsNullOrWhiteSpace(periodo))
                    {
                        erroresDeRegistro.Add($"Fila {filaNum}: Código de Producto o Período es inválido.");
                        continue;
                    }

                    var claveDuplicidad = $"{codigoProducto}-{periodo}";

                    if (clavesYaVistasEnEsteArchivo.Contains(claveDuplicidad))
                    {
                        erroresDeRegistro.Add($"Fila {filaNum}: Duplicado interno.");
                        continue;
                    }
                    clavesYaVistasEnEsteArchivo.Add(claveDuplicidad);

                    if (existentesEnDBSet.Contains(claveDuplicidad))
                    {
                        erroresDeRegistro.Add($"Fila {filaNum}: Duplicado DB.");
                        continue;
                    }

                    nuevasEntidades.Add(new DataProcesada
                    {
                        CodigoProducto = codigoProducto,
                        NombreProducto = nombreProducto,
                        Precio = precio,
                        Cantidad = cantidad,
                        Periodo = periodo,
                        CargaArchivoId = carga.Id,
                        FechaCreacion = DateTime.UtcNow
                    });
                }

                bool conErrores = erroresDeRegistro.Any();
                carga.MensajeError = conErrores
                    ? $"Carga finalizada con {erroresDeRegistro.Count} registros omitidos."
                    : string.Empty;

                if (nuevasEntidades.Any())
                {
                    _context.DataProcesadas.AddRange(nuevasEntidades);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Se insertaron {Count} entidades en DataProcesada.", nuevasEntidades.Count);

                    try
                    {
                        await _cache.RemoveAsync("carga_historial_completo", cancellationToken);
                        _logger.LogInformation(">>> CACHÉ DE REDIS INVALIDADO (Key: carga_historial_completo) <<<");
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "No se pudo limpiar el caché de Redis, pero los datos se guardaron.");
                    }
                }

                carga.Estado = EstadoCarga.Finalizado;
                carga.FechaFin = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                PublicarNotificacion(carga, conErrores);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en carga {CargaId}", carga.Id);
                carga.Estado = EstadoCarga.Error;
                carga.MensajeError = ex.Message ?? "Error desconocido";
                await _context.SaveChangesAsync(cancellationToken);
                PublicarNotificacion(carga, true);
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

            _messagePublisher.Publish(
                exchangeName: "notifications.exchange",
                routingKey: "notificaciones",
                message: jobFinishedEvent);
        }
    }
}