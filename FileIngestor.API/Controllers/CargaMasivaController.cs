using FileIngestor.API.Features.CargaMasiva.Commands;
using FileIngestor.Application.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class CargaMasivaController : ControllerBase
{
    private readonly ISender _mediator;

    public CargaMasivaController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
        {
            return BadRequest(new { Message = "Debe enviar un archivo válido." });
        }

        var userEmail = "usuario.actual@empresa.com";

        var command = new UploadFileCommand
        {
            File = dto.File,
            Periodo = dto.Periodo,
            UsuarioEmail = userEmail

        };

        int cargaArchivoId = await _mediator.Send(command);

        return Ok(new
        {
            Message = "Carga registrada y trabajo asíncrono publicado",
            CargaArchivoId = cargaArchivoId
        });
    }
}