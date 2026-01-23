using Microsoft.AspNetCore.Mvc;

namespace DataProcessor.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "DataProcessor API está viva y respondiendo" });
        }
    }
}