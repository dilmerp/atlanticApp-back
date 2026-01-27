using MediatR;
using Microsoft.AspNetCore.Mvc;
using DataProcessor.Application.Features.CargaMasiva.Queries;
using Microsoft.AspNetCore.Http;

namespace DataProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CargaMasivaController : ControllerBase
{
    private readonly IMediator _mediator;

    public CargaMasivaController(IMediator mediator)
    {
        _mediator = mediator;
    }

[HttpGet("historial")]
    public async Task<ActionResult<IEnumerable<CargaStatusDto>>> GetHistorial()
    {
        var result = await _mediator.Send(new GetCargaHistorialQuery());
        return Ok(result);
    }



    /// <summary>
    /// Obtiene el estado de procesamiento de un archivo cargado.
    /// </summary>
    /// <param name="id">ID de la carga (CargaArchivoId)</param>
    /// <returns>Detalles del estado actual de la carga</returns>
    [HttpGet("status/{id}")]
    [ProducesResponseType(typeof(CargaStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CargaStatusDto?>> GetStatus(int id)
    {
        // El envío a MediatR activará el Handler que consulta Redis y luego DB
        var result = await _mediator.Send(new GetCargaStatusQuery(id));

        if (result == null)
        {
            return NotFound(new { mensaje = $"No se encontró la carga con ID {id}" });
        }

        return Ok(result);
    }
}