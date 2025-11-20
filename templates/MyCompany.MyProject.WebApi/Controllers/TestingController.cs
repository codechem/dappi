using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Expressions;
using Dappi.HeadlessCms.ActionFilters;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCompany.MyProject.WebApi.Data;

namespace MyCompany.MyProject.WebApi.Controllers
{
    [ApiExplorerSettings(GroupName = "Toolkit")]
    [Route("api/testing")]
    [ApiController]
    public class TestingController(
        AppDbContext dbContext,
        IMediaUploadService uploadService) : ControllerBase
    {
        [HttpGet("filter")]
        [CollectionFilter]
        public async Task<IActionResult> FilterCollection()
        {
            // query = query.Include(x => x.Books)!.ThenInclude(x => x.Reviews).Include(x => x.Address);
            
            try
            {
                // var query = dbContext.Books.AsQueryable();
                var guids = new List<Guid>
                {
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Parse("00000000-0000-0000-0000-000000000000")
                };
                // query = query.Where(x => guids.Contains(x.Id));
                
                var guidsString = string.Join(",", guids.Select(g => $"\"{g}\""));

                var query = dbContext.Books
                    .AsQueryable()
                    .Where($"Id IN ({guidsString})");

                return Ok(await query.ToListAsync());
                
                // query = query.WhereInterpolated()
                // query = query.WhereInterpolated($"Id IN ({string.Join(",", guids.Select(x => $"\"{Guid.Parse(x.ToString())}\""))}).ToArray()");
                // query = query.Where($"\"{string.Join(",",guids.Select(x => $"\"{x}\""))}\".Contains(Id)");
                // query = query.Where("@0.Contains(AuthorId)", guids.ToArray());
                // query = query.Where($"Id.ToString() In({string.Join(",", guids)})");
                // string ex = $"{guids}.Contains(Id)";
                // query = query.Where(ex , guids);
            }
            catch (ParseException e)
            {
                return BadRequest(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(e);
            }
        }
    }
}