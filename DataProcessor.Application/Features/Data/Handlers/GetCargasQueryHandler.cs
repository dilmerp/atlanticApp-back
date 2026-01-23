// DataProcessor.Application/Features/Data/Handlers/GetCargasQueryHandler.cs

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
    public class GetCargasQueryHandler : IRequestHandler<GetCargasQuery, List<CargaArchivo>>
    {
        private readonly IApplicationDbContext _context;

        public GetCargasQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CargaArchivo>> Handle(GetCargasQuery request, CancellationToken cancellationToken)
        {
            // Retorna todas las cargas ordenadas por las más recientes
            return await _context.CargaArchivos
                                 .OrderByDescending(c => c.FechaRegistro)
                                 .ToListAsync(cancellationToken);
        }
    }
}