using Dappi.HeadlessCms.Authentication;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = DappiAuthenticationSchemes.DappiAuthenticationScheme)]
public class PluginsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public PluginsController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public IActionResult GetPluginsState()
    {
        var interfaceTypes = new[] { typeof(IEmailService) };

        var availableInterfaces = _serviceProvider.ResolveAvailable(interfaceTypes);

        var services = interfaceTypes.ToDictionary(
            type => type.Name,
            type => availableInterfaces.ContainsKey(type)
        );

        return Ok(new { services });
    }
}