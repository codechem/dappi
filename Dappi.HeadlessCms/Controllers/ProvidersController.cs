
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    [HttpGet("storage")]
    public IActionResult GetStorageSource(IServiceProvider serviceProvider)
    {
        return Ok(serviceProvider.GetService(typeof(IMediaUploadService))?.ToString());
    }
}
