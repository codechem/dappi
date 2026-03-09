namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    /// <summary>
    /// Base class for configuring permissions for a single role.
    /// Create one subclass per role and implement <see cref="Configure"/>.
    /// The system will discover and apply all configurations automatically.
    /// </summary>
    /// <typeparam name="TUser">The application user type.</typeparam>
    public abstract class RoleConfiguration<TUser>
        where TUser : AppUser
    {
        /// <summary>
        /// The name of the role this configuration applies to.
        /// Use <see cref="UsersAndPermissionsConstants.DefaultRoles"/> for the built-in roles.
        /// </summary>
        public abstract string RoleName { get; }

        /// <summary>
        /// Configure the controller/action permissions for <see cref="RoleName"/>.
        /// </summary>
        /// <param name="builder">
        /// A <see cref="IControllerConfigurationBuilder{TUser}"/> already scoped to <see cref="RoleName"/>.
        /// Call <c>ForController(...).Allow(...)</c> chains on it.
        /// </param>
        public abstract void Configure(IControllerConfigurationBuilder<TUser> builder);
    }
}
