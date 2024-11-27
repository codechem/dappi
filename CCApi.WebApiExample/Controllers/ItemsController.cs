using CCApi.WebApiExample.Data;
using Microsoft.AspNetCore.Mvc;

namespace CCApi.WebApiExample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
     public async Task<IActionResult> GetCpeStrings()
     {
         return Ok("Success");
     }
}