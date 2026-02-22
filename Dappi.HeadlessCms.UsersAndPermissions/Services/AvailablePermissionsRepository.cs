using Dappi.Core.Models;
using Dappi.HeadlessCms.UsersAndPermissions.Core;

namespace Dappi.HeadlessCms.UsersAndPermissions.Services
{
    public class AvailablePermissionsRepository(
        IReadOnlyDictionary<string, IReadOnlyList<MethodRouteEntry>> controllerRoutes
    )
    {
        public IEnumerable<AppPermission> GetAllPermissions()
        {
            var permissions = new List<AppPermission>();
            foreach (var controller in controllerRoutes)
            {
                foreach (var methodRoute in controller.Value)
                {
                    permissions.Add(
                        new AppPermission(
                            $"{controller.Key}:{methodRoute.MethodName}",
                            methodRoute.HttpRoute
                        )
                    );
                }
            }
            return permissions;
        }
    }
}
