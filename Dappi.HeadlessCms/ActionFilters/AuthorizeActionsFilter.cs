using System.Net;
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
            var allowedCrudAction = customAttribute?.AllowedCrudActions.Length is > 0
                ? customAttribute.AllowedCrudActions
                : CcControllerAttribute.DefaultActions.ToArray();
            if (customAttribute == null)
            {
                return;
            }
            if (request.Method == "POST" && !allowedCrudAction.Contains(CrudActions.Create) ||
                request.Method == "PUT" && !allowedCrudAction.Contains(CrudActions.Update) ||
                request.Method == "DELETE" && !allowedCrudAction.Contains(CrudActions.Delete) ||
                request.Method == "PUT" && !allowedCrudAction.Contains(CrudActions.Patch))
            {
                context.Result =
                    new JsonResult(new { Error = "Method not allowed" })
                    {
                        StatusCode = (int)HttpStatusCode.MethodNotAllowed
                    };
            }
        }
    }
}