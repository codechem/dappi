// namespace CCApi.SourceGenerator;
//
// [ApiController]
// [Route("api/[controller]")]
// [Authorize(Policy = Constants.Authorization.Policies.RequireAdminOrTenantGroup)] // TO BE ADDED IN ATTRIBUTEPROPERTY
// public class CpeInfoController(AppDbContext dbContext) : ControllerBase // Services to be injected
// {
//     [HttpGet]
//     public async Task<IActionResult> GetCpeStrings()
//     {
//         return Ok(await dbContext.CpeInformation.ToListAsync());
//     }
// }
// [HttpGet("{id}")]
// public async Task<IActionResult> GetBook(string id)
// {
//     if(id is null) return BadRequest();
//         
//     var result = _appDbContext.Books.FirstOrDefault(p => p.Id == id);
//     if (result is null) return NotFound();
//     // transform to DTO before return
//     return Ok(result);
// }