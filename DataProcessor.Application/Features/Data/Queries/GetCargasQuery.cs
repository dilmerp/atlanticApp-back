// DataProcessor.Application/Features/Data/Queries/GetCargasQuery.cs

using Common.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace DataProcessor.Application.Features.Data.Queries
{
    // Simplemente devuelve todas las cargas de archivo registradas.
    public class GetCargasQuery : IRequest<List<CargaArchivo>>
    {
        // No requiere propiedades, es una consulta simple de lista.
    }
}