using MediatR;
using System;

namespace DataProcessor.Application.Features.CargaMasiva.Queries;

public record GetCargaStatusQuery(int Id) : IRequest<CargaStatusDto?>;