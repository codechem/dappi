using Microsoft.AspNetCore.Mvc;

namespace CCApi.WebApiExample.Controllers;

public partial class MovieController
{
    [HttpGet("additional")]
    public async Task<IActionResult> GetBooksOne()
    {
        return Ok("Hello Dule from partial class");
    }
} 