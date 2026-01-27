using Common.Domain.Entities;
using Common.Domain.Interfaces;
using DataProcessor.Application.Features.Data.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Application.Features.Data.Handlers
{
    public class GetDataProcesadaQueryHandler : IRequestHandler<GetDataProcesadaQuery, List<DataProcesada>>
    {
        private readonly IApplicationDbContext _context;

        public GetDataProcesadaQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DataProcesada>> Handle(GetDataProcesadaQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DataProcesadas.AsQueryable();

            // Aplicar filtro por Periodo si se proporciona
            if (!string.IsNullOrWhiteSpace(request.Periodo))
            {
                query = query.Where(d => d.Periodo == request.Periodo);
            }

            // Aplicar filtro por Código de Producto si se proporciona
            if (!string.IsNullOrWhiteSpace(request.CodigoProducto))
            {
                query = query.Where(d => d.CodigoProducto == request.CodigoProducto);
            }

            // Ordenar por defecto y ejecutar la consulta a la base de datos
            return await query
                         .OrderByDescending(d => d.FechaCreacion)
                         .ToListAsync(cancellationToken);
        }
    }
}