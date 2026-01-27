using FileIngestor.Application.Features.CargaMasiva.Commands;
using FileIngestor.Application.Features.CargaMasiva.Handlers;
using FileIngestor.Application.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FileIngestor.API.Controllers
{
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
            // 1. Validaciones de entrada
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest(new { Message = "Debe enviar un archivo válido." });
            }

            if (string.IsNullOrEmpty(dto.Periodo))
            {
                return BadRequest(new { Message = "El periodo es obligatorio." });
            }

            // 2. Preparación del comando
            var userEmail = "usuario.actual@empresa.com";
            var command = new UploadFileCommand(dto.File, userEmail, dto.Periodo);

            // 3. Ejecución del comando
            var result = await _mediator.Send(command);

            // 4. Respuesta corregida accediendo a las propiedades del objeto
            return Ok(new
            {
                message = "Carga registrada y trabajo asíncrono publicado",
                success = result.Success,
                id = result.Id,       
                estado = result.Status
            });
        }
    }
}