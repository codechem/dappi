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


// GET ENDPOINT
// public async Task<IActionResult> GetBooks([FromQuery] PagingFilter filter)
// {
//     // Start with the base query
//     IQueryable<Book> query = dbContext.Books.Include(p => p.Author);
//
//     // Apply sorting
//     if (!string.IsNullOrEmpty(filter.SortBy))
//     {
//         // Dynamically apply sorting based on SortBy and SortDirection
//         query = filter.SortDirection == SortDirection.Ascending
//             ? query.OrderBy(p => EF.Property<object>(p, filter.SortBy))
//             : query.OrderByDescending(p => EF.Property<object>(p, filter.SortBy));
//     }
//     else
//     {
//         // If no sorting field is provided, default to sorting by Id descending
//         query = query.OrderByDescending(p => p.Id);
//     }
//
//     // Apply paging (Skip and Take)
//     var total = await query.CountAsync(); // Get total count before pagination
//     var data = await query
//         .Skip(filter.Offset)
//         .Take(filter.Limit)
//         .ToListAsync();
//
//     // Prepare response DTO
//     var listDto = new ListResponseDTO<Book>
//     {
//         Data = data,
//         Limit = filter.Limit,
//         Offset = filter.Offset,
//         Total = total
//     };
//
//     return Ok(listDto);
// }