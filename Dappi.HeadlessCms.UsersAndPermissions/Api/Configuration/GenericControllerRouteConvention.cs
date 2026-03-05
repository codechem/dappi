using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api.Configuration;

public class GenericControllerRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.ControllerType.IsGenericType)
        {
            controller.ControllerName = "UsersAndPermissions";
        }
    }
}
