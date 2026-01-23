using MediatR;
using System;

namespace DataProcessor.Application.Features.CargaMasiva.Queries;

//public record CargaStatusDto(
//    int Id,
//    string NombreArchivo,
//    string Estado,
//    string MensajeError,
//    DateTimeOffset FechaRegistro,
//    DateTimeOffset? FechaFin
//);

public record GetCargaStatusQuery(int Id) : IRequest<CargaStatusDto?>;