using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CCApi.WebApiExample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : Controller
{
    [HttpGet("secret")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetSecret()
    {
        return Ok("This is a secret!");
    }
}