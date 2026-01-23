using MediatR;

namespace DataProcessor.Application.Features.CargaMasiva.Queries;

// El Query no necesita parámetros porque queremos todo el historial
public record GetCargaHistorialQuery() : IRequest<IEnumerable<CargaStatusDto>>;