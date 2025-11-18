using System.Text.RegularExpressions;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using MyCompany.MyProject.WebApi.Data;
using MyCompany.MyProject.WebApi.Entities;

namespace MyCompany.MyProject.WebApi.Controllers
{
    [ApiExplorerSettings(GroupName = "Toolkit")]
    [Route("api/testing")]
    [ApiController]
    public class TestingController(
        AppDbContext dbContext,
        IMediaUploadService uploadService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Testing([FromQuery] FilterQuery filterQuery)
        {
            var filters = Request.Query
                .Where(x =>
                    x.Key.Contains("filter", StringComparison.CurrentCultureIgnoreCase)
                ).ToList();
            var parsed= ParseFilter(filters);
            var query = dbContext.Authors.AsQueryable();
            query = query.Include(x => x.Books)!.ThenInclude(x => x.Reviews).Include(x => x.Address);
            if (parsed.Count > 0)
            {
                var res = await query.ApplyFilter<Author>(parsed).ToListAsync();
                return Ok(res);
            }
            
            return Ok(await query.ToListAsync());
        }

        private List<Filter> ParseFilter(List<KeyValuePair<string, StringValues>> filters)
        {
            var filterList = new List<Filter>();
            var nz = Newtonsoft.Json.JsonConvert.SerializeObject(filters);

            foreach (var filter in filters)
            {
                var pattern = @"\[(.*?)\]";

                var matches = Regex.Matches(filter.Key, pattern);

                var newFilter = new Filter
                {
                    Value = filter.Value.ToString(),
                };
                foreach (Match match in matches)
                {
                    var value = match.Value.Replace("[", "").Replace("]", "");
                    if (value.Equals("$and") || value.Equals("$or"))
                    {
                        value = value.Replace("$", "");
                        var parsedOperator = Enum.Parse<Operator>(value, true);
                        newFilter.Operator = parsedOperator;
                    } 
                    else if (value.StartsWith("$") && (!value.Equals("$and") || !value.Equals("$or")))
                    {
                        value = value.Replace("$", "");
                        if (Enum.TryParse(value, true, out Operation operation))
                        {
                            newFilter.Operation = operation;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid filter operation: {value}");
                        }
                    }
                    else if (int.TryParse(value, out int parsedValue))
                    {
                        //Logic for indexing TBD
                    }
                    else
                    {
                        newFilter.Fields.Add(value);
                    }
                }

                filterList.Add(newFilter);
            }

            return filterList;
        }

        // {
        //     "Filters": {
        //         "$and": [
        //         { "Field": "name", "Operator": "$eq", "Value": "John" },
        //         { "Field": "age", "Operator": "$gte", "Value": 30 }
        //         ],
        //         "$or": [
        //         { "Field": "country", "Operator": "$eq", "Value": "USA" }
        //         ]
        //     }
        // }
    }
}