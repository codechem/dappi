using Dappi.Core.Attributes;
using Dappi.Core.Enums;
using Dappi.HeadlessCms.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dappi.HeadlessCms.ActionFilters
{
    public class AuthorizeActionsFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var customAttribute = context.Controller
                .GetType()
                .GetCustomAttributes(typeof(CcControllerAttribute), true)
                .FirstOrDefault() as CcControllerAttribute;

            var request = context.HttpContext.Request;
            if (customAttribute == null)
            {
                return;
            }

            if (request.Method == "POST" && !customAttribute.AllowedCrudActions.Contains(CrudActions.Create) ||
                request.Method == "PUT" && !customAttribute.AllowedCrudActions.Contains(CrudActions.Update) ||
                request.Method == "DELETE" && !customAttribute.AllowedCrudActions.Contains(CrudActions.Delete))
            {
                throw new MethodNotAllowedException("Method not allowed" , request.Method);
            }
        }
    }
}