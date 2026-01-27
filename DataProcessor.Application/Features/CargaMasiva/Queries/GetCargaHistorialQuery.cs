using MediatR;

namespace DataProcessor.Application.Features.CargaMasiva.Queries;

public record GetCargaHistorialQuery() : IRequest<IEnumerable<CargaStatusDto>>;