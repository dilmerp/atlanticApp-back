namespace DataProcessor.Application.Features.CargaMasiva.Queries;

public record CargaStatusDto(int Id, string NombreArchivo, string Estado, string MensajeError, DateTimeOffset FechaRegistro, DateTimeOffset? FechaFin);