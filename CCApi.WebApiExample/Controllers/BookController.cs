using CCApi.WebApiExample.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CCApi.WebApiExample.Controllers;

public partial class BookController
{
    [HttpGet("temperature")]
    public async Task<IActionResult> GetTemperature()
    {
        // utilizing HttpContext to get the service from dependency injection
        // alternatively, you can use constructor injection, but before that we should disable it from generator
        var weatherService = HttpContext.RequestServices.GetService(typeof(IWeatherService)) as IWeatherService;
        if (weatherService == null)
        {
            return BadRequest("Weather service not found");
        }
        var temperature =  await weatherService.GetCurrentTemperature();
        return Ok(temperature);
    }
}