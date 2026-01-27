using Common.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace DataProcessor.Application.Features.Data.Queries
{
    public class GetDataProcesadaQuery : IRequest<List<DataProcesada>>
    {
        // Filtros opcionales que el API recibirá desde los parámetros de la URL
        public string Periodo { get; set; }
        public string CodigoProducto { get; set; }
    }
}