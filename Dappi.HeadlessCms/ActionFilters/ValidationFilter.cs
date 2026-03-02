using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dappi.HeadlessCms.ActionFilters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            if (!context.ModelState.IsValid)
            {
                var errorsInModelState = context
                    .ModelState.Where(p => p.Value.Errors.Count > 0)
                    .ToDictionary(
                        keyValuePair => keyValuePair.Key,
                        keyValuePair => keyValuePair.Value.Errors.Select(p => p.ErrorMessage)
                    )
                    .ToArray();
                var list = errorsInModelState.SelectMany(error => error.Value).ToList();
                if (list.Count != 0)
                {
                    var errorLines = list.Select(s => s)
                        .Distinct()
                        .Aggregate((current, next) => current + "\n" + next);
                    throw new ValidationException(errorLines);
                }
            }
            await next();
        }
    }
}
