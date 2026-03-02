using System.Reflection;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Dappi.HeadlessCms.UsersAndPermissions.Api.Configuration;

public class GenericControllerFeatureProvider<TUser>
    : IApplicationFeatureProvider<ControllerFeature>
    where TUser : AppUser, new()
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var closed = typeof(UsersAndPermissionsController<>).MakeGenericType(typeof(TUser));
        feature.Controllers.Add(closed.GetTypeInfo());
    }
}
