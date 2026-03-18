using System.Reflection;

namespace Dappi.HeadlessCms.UsersAndPermissions.Core;

public interface IRoleConfigurationBuilder<TUser>
    where TUser : AppUser
{
    IControllerConfigurationBuilder<TUser> ForRole(
        string roleName,
        bool isDefaultForAuthenticated = false
    );
    AppRole[] Build();
}

public interface IControllerConfigurationBuilder<TUser>
    where TUser : AppUser
{
    IPermissionConfigurationBuilder<TUser> ForController(string controllerName);
}

public interface IPermissionConfigurationBuilder<TUser>
    where TUser : AppUser
{
    IPermissionConfigurationBuilder<TUser> Allow(string methodName);
    IPermissionConfigurationBuilder<TUser> AllowAll();
    IPermissionConfigurationBuilder<TUser> Deny(string methodName);
    IPermissionConfigurationBuilder<TUser> ForController(string controllerName);
    IRoleConfigurationBuilder<TUser> And();
}

public class AppRoleAndPermissionsBuilder<TUser>
    : IRoleConfigurationBuilder<TUser>,
        IControllerConfigurationBuilder<TUser>,
        IPermissionConfigurationBuilder<TUser>
    where TUser : AppUser
{
    private record RoleEntry(
        string RoleName,
        string Controller,
        string Method,
        bool IsDefaultForAuthenticated
    );

    private readonly List<RoleEntry> _entries = [];
    private string? _currentRole;
    private string? _currentController;
    private bool _currentIsDefaultForAuthenticated;

    public static IRoleConfigurationBuilder<TUser> Create() =>
        new AppRoleAndPermissionsBuilder<TUser>();

    public IControllerConfigurationBuilder<TUser> ForRole(
        string roleName,
        bool isDefaultForAuthenticated = false
    )
    {
        _currentRole = roleName ?? throw new ArgumentNullException(nameof(roleName));
        _currentIsDefaultForAuthenticated = isDefaultForAuthenticated;
        return this;
    }

    IPermissionConfigurationBuilder<TUser> IControllerConfigurationBuilder<TUser>.ForController(
        string controllerName
    ) => SetController(controllerName);

    IPermissionConfigurationBuilder<TUser> IPermissionConfigurationBuilder<TUser>.ForController(
        string controllerName
    ) => SetController(controllerName);

    public IPermissionConfigurationBuilder<TUser> Allow(string methodName)
    {
        AddEntry(methodName);
        return this;
    }

    public IPermissionConfigurationBuilder<TUser> Deny(string methodName)
    {
        _entries.RemoveAll(e =>
            e.RoleName == _currentRole
            && e.Controller == _currentController
            && e.Method == methodName
        );
        return this;
    }

    public IPermissionConfigurationBuilder<TUser> AllowAll()
    {
        if (_currentController == null)
            throw new InvalidOperationException("Call ForController() before AllowAll().");

        var controllerType =
            AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .FirstOrDefault(t => t.Name == _currentController)
            ?? throw new InvalidOperationException(
                $"Controller type '{_currentController}' not found."
            );

        var methods = controllerType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .Distinct();

        foreach (var method in methods)
            AddEntry(method);

        return this;
    }

    public IRoleConfigurationBuilder<TUser> And() => this;

    public AppRole[] Build()
    {
        return _entries
            .GroupBy(e => e.RoleName)
            .Select(roleGroup =>
            {
                var permissions = roleGroup
                    .Select(e => new AppPermission($"{e.Controller}:{e.Method}", ""))
                    .ToList();

                var isDefault = roleGroup.First().IsDefaultForAuthenticated;

                return isDefault
                    ? AppRole.CreateDefaultAuthenticatedUserRole(permissions)
                    : new AppRole(roleGroup.Key, permissions);
            })
            .ToArray();
    }

    private AppRoleAndPermissionsBuilder<TUser> SetController(string controllerName)
    {
        _currentController =
            controllerName ?? throw new ArgumentNullException(nameof(controllerName));
        return this;
    }

    private void AddEntry(string methodName)
    {
        if (_currentRole == null)
            throw new InvalidOperationException("No role selected.");
        if (_currentController == null)
            throw new InvalidOperationException("No controller selected.");
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name required.", nameof(methodName));

        var alreadyAdded = _entries.Any(e =>
            e.RoleName == _currentRole
            && e.Controller == _currentController
            && e.Method == methodName
        );

        if (!alreadyAdded)
            _entries.Add(
                new RoleEntry(
                    _currentRole,
                    _currentController,
                    methodName,
                    _currentIsDefaultForAuthenticated
                )
            );
    }
}
