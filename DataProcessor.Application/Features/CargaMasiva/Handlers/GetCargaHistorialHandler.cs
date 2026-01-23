using Common.Domain.Interfaces;
using DataProcessor.Application.Features.CargaMasiva.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore; // Indispensable para AsNoTracking y ToListAsync
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Application.Features.CargaMasiva.Handlers;

public class GetCargaHistorialHandler : IRequestHandler<GetCargaHistorialQuery, IEnumerable<CargaStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public GetCargaHistorialHandler(IApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IEnumerable<CargaStatusDto>> Handle(GetCargaHistorialQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = "carga_historial_completo";

        // Intento leer de Redis
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<IEnumerable<CargaStatusDto>>(cachedData, _jsonOptions)!;
        }

        // Consulta a Base de Datos con AsNoTracking (ahora funcionará)
        var historial = await _context.CargaArchivos
            .AsNoTracking()
            .OrderByDescending(x => x.FechaRegistro)
            .Select(x => new CargaStatusDto(
                x.Id,
                x.NombreArchivo,
                x.Estado.ToString(),
                x.MensajeError ?? string.Empty,
                x.FechaRegistro,
                x.FechaFin
            ))
            .ToListAsync(cancellationToken);

        // Guardar en Redis
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        var serializedData = JsonSerializer.Serialize(historial, _jsonOptions);
        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

        return historial;
    }
}