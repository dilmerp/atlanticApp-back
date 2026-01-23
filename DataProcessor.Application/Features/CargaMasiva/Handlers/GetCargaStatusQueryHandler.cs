using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Domain.Interfaces;
using DataProcessor.Application.Features.CargaMasiva.Queries; // Importante: para ver la Query y el DTO

namespace DataProcessor.Application.Features.CargaMasiva.Handlers
{
    // El IRequestHandler DEBE coincidir con el IRequest de la Query (sin el '?' en el tipo TResponse)
    public class GetCargaStatusQueryHandler : IRequestHandler<GetCargaStatusQuery, CargaStatusDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetCargaStatusQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CargaStatusDto?> Handle(GetCargaStatusQuery request, CancellationToken cancellationToken)
        {
            var result = await _context.CargaArchivos
                .Where(x => x.Id == request.Id)
                .Select(x => new CargaStatusDto(
                    x.Id,
                    x.NombreArchivo,
                    x.Estado.ToString(),
                    x.MensajeError ?? string.Empty,
                    x.FechaRegistro,
                    x.FechaFin
                ))
                .FirstOrDefaultAsync(cancellationToken);

            // Si es null, devolvemos null (el '!' silencia la advertencia del compilador)
            return result!;
        }
    }
}