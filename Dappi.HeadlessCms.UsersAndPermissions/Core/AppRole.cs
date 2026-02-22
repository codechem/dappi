namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    public class AppRole
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        private readonly List<AppPermission> _permissions = [];
        public IEnumerable<IAppUser> Users { get; set; }

        private AppRole() { } // For EF Core

        public IReadOnlyList<AppPermission> Permissions => _permissions.AsReadOnly();

        public bool IsDefaultForAuthenticatedUser { get; private set; }

        public AppRole(string name, IEnumerable<AppPermission> permissions)
        {
            Name = name;
            _permissions.AddRange(permissions);
        }

        public bool HasPermission(string permissionName) =>
            _permissions.Any(p => p.Name == permissionName);

        public static AppRole CreateDefaultPublicUserRole(IEnumerable<AppPermission> permissions)
        {
            return new AppRole(UsersAndPermissionsConstants.DefaultRoles.Public, permissions);
        }

        public static AppRole CreateDefaultAuthenticatedUserRole(
            IEnumerable<AppPermission> permissions
        )
        {
            return new AppRole(UsersAndPermissionsConstants.DefaultRoles.Authenticated, permissions)
            {
                IsDefaultForAuthenticatedUser = true,
            };
        }

        public void AddPermission(AppPermission permission)
        {
            if (_permissions.All(p => p.Name != permission.Name))
                _permissions.Add(permission);
        }

        public void RemovePermission(AppPermission permission)
        {
            var existing = _permissions.FirstOrDefault(p => p.Name == permission.Name);
            if (existing != null)
                _permissions.Remove(existing);
        }

        public void ClearPermissions() => _permissions.Clear();

        public override string ToString()
        {
            return Name;
        }
    }
}
