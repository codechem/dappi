# Users And Permissions Setup

This guide shows how to set up the roles and permissions system in your own ASP.NET Core project.

To keep the examples concrete, this guide uses an imaginary application named `Acme.Store.Api`.

The recommended setup has five parts:

1. Register the system in `Program.cs`
2. Create your `AppUser`
3. Create a DbContext that inherits from `UsersAndPermissionsDbContext`
4. Add the first EF Core migration and update the database
5. Define role permissions with `RoleConfiguration<TUser>` classes

An optional last step shows how to accept external JWTs with a custom `JwtValidationProvider`.

## Example Project Shape

Your project might look something like this:

```text
Acme.Store.Api/
|-- Controllers/
|-- Data/
|   `-- AppDbContext.cs
|-- UsersAndPermissionsSystem/
|   |-- Data/
|   |   `-- AppUsersAndPermissionsDbContext.cs
|   |-- Entities/
|   |   `-- AppUser.cs
|   |-- ExternalAuth/
|   |   |-- CustomJwtOptions.cs
|   |   `-- CustomJwtValidationProvider.cs
|   `-- Permissions/
|       |-- PublicRoleConfiguration.cs
|       |-- AuthenticatedRoleConfiguration.cs
|       `-- AdminRoleConfiguration.cs
|-- Program.cs
`-- appsettings.json
```

The exact folder names are up to you. The important part is that your user type, users-and-permissions DbContext, role configuration classes, and external JWT provider code all live in your application assembly. Keeping them under a dedicated `UsersAndPermissionsSystem` folder makes the setup easier to maintain.

---

## Start From Program.cs

Your application startup should follow this pattern:

```csharp
using Acme.Store.Api.Data;
using Acme.Store.Api.UsersAndPermissionsSystem.Data;
using Dappi.HeadlessCms;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.UsersAndPermissions;
using GeneratedPermissions;
using AppUser = Acme.Store.Api.UsersAndPermissionsSystem.Entities.AppUser;

namespace Acme.Store.Api;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDappi<AppDbContext>(builder.Configuration);
        builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

        builder.Services.AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
            PermissionsMeta.Controllers,
            builder.Configuration
        );

        var app = builder.Build();

        await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
            typeof(Program).Assembly
        );

        await app.UseDappi<AppDbContext>();

        app.UseHttpsRedirection();
        app.MapControllers();

        app.Run();
    }
}
```

There are two important phases here:

- `AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(...)` registers the services, DbContext, identity stores, permission metadata, and internal authentication pieces.
- `UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(typeof(Program).Assembly)` runs migrations and discovers all `RoleConfiguration<AppUser>` classes from the supplied assembly.

If you want your API to accept third-party JWTs directly, you can chain `.AddExternalJwtProvider<YourProvider, AppUser>()` after `AddUsersAndPermissionsSystem(...)`. That part is optional and is covered later in this guide.

---

## 1. Create AppUser

Create an application user type that inherits from the users-and-permissions base `AppUser` type.

```csharp
using Dappi.HeadlessCms.UsersAndPermissions.Core;

namespace Acme.Store.Api.UsersAndPermissionsSystem.Entities;

public class AppUser : Dappi.HeadlessCms.UsersAndPermissions.Core.AppUser
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
}
```

This gives you the identity fields from the base type and adds the role relationship that the permissions system expects.

Minimum requirements:

- `RoleId` must point to the assigned `AppRole`
- `Role` must be the navigation property to that role
- The class must be the same `TUser` type used in `AddUsersAndPermissionsSystem`, `UseUsersAndPermissionsSystem`, and any custom JWT provider

---

## 2. Create AppUsersAndPermissionsDbContext

Create a dedicated DbContext for the users-and-permissions system and inherit from `UsersAndPermissionsDbContext`.

```csharp
using Acme.Store.Api.UsersAndPermissionsSystem.Entities;
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Microsoft.EntityFrameworkCore;

namespace Acme.Store.Api.UsersAndPermissionsSystem.Data;

public class AppUsersAndPermissionsDbContext(
    DbContextOptions<AppUsersAndPermissionsDbContext> options
) : UsersAndPermissionsDbContext(options)
{
    public DbSet<AppUser> AppUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Role)
            .WithMany(r => (IEnumerable<AppUser>)r.Users);
    }
}
```

Important points:

- Always inherit from `UsersAndPermissionsDbContext`
- Always call `base.OnModelCreating(modelBuilder)`
- Keep the `AppUsers` DbSet on this context because ASP.NET Core Identity stores your users through this DbContext

The base class already manages the users-and-permissions tables such as roles and permissions.

---

## 3. Register The System In Program.cs

The registration in `Program.cs` should follow this pattern:

```csharp
builder.Services.AddDappi<AppDbContext>(builder.Configuration);

builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

builder.Services.AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
    PermissionsMeta.Controllers,
    builder.Configuration
);
```

What each part does:

- `AddDappi<AppDbContext>(...)` registers your main application DbContext
- `AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(...)` registers the default Dappi authentication pieces
- `AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(...)` registers the users-and-permissions DbContext, identity stores, permission repository, controllers, and internal JWT settings used by the permissions system

`PermissionsMeta.Controllers` is generated by the source generator and contains the controller and method metadata used to build available permissions.

If permissions are missing after you add or rename controller actions, rebuild the project so `PermissionsMeta.Controllers` is regenerated.

---

## 4. Add The First Migration And Update The Database

`UseUsersAndPermissionsSystem(...)` applies pending migrations at startup, but it does not create migration files for you.

You still need to create the first migration for `AppUsersAndPermissionsDbContext`.

From your Web API project folder:

```bash
dotnet ef migrations add InitialUsersAndPermissions \
  --context AppUsersAndPermissionsDbContext \
    --output-dir UsersAndPermissionsSystem/Migrations
dotnet ef database update --context AppUsersAndPermissionsDbContext
```

This gives you two useful guarantees:

- the migration exists in source control
- the database schema is ready before the application starts syncing roles and permissions

On later runs, `UseUsersAndPermissionsSystem(...)` will apply any additional pending migrations automatically.

If this is also the first time you are setting up the main application database, create and apply the migration for your main `AppDbContext` as well.

---

## 5. Add Role Configurations

The current system does not require you to build roles inline in `Program.cs`.

Instead, create one class per role by inheriting from `RoleConfiguration<AppUser>`. The system discovers these classes automatically when you call:

```csharp
await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
    typeof(Program).Assembly
);
```

### Public Role

Create a role configuration for unauthenticated requests.

```csharp
using Acme.Store.Api.UsersAndPermissionsSystem.Entities;
using Dappi.HeadlessCms.UsersAndPermissions.Api;
using Dappi.HeadlessCms.UsersAndPermissions.Core;

namespace Acme.Store.Api.UsersAndPermissionsSystem.Permissions;

public class PublicRoleConfiguration : RoleConfiguration<AppUser>
{
    public override string RoleName => UsersAndPermissionsConstants.DefaultRoles.Public;

    public override void Configure(IControllerConfigurationBuilder<AppUser> builder)
    {
        builder
            .ForController(nameof(UsersAndPermissionsController<AppUser>))
            .Allow(nameof(UsersAndPermissionsController<AppUser>.Login))
            .Allow(nameof(UsersAndPermissionsController<AppUser>.Register))
            .Allow(nameof(UsersAndPermissionsController<AppUser>.Refresh));
    }
}
```

Typical permissions here are:

- login
- register
- refresh token

### Authenticated Role

Create the default role for authenticated users.

```csharp
using Acme.Store.Api.Controllers;
using Acme.Store.Api.UsersAndPermissionsSystem.Entities;
using Dappi.HeadlessCms.UsersAndPermissions.Core;

namespace Acme.Store.Api.UsersAndPermissionsSystem.Permissions;

public class AuthenticatedRoleConfiguration : RoleConfiguration<AppUser>
{
    public override string RoleName => UsersAndPermissionsConstants.DefaultRoles.Authenticated;

    public override void Configure(IControllerConfigurationBuilder<AppUser> builder)
    {
        builder
            .ForController(nameof(ProductsController))
            .Allow(nameof(ProductsController.GetAll))
            .Allow(nameof(ProductsController.GetById))
            .ForController(nameof(ProfileController))
            .Allow(nameof(ProfileController.GetCurrentUserProfile));
    }
}
```

Useful builder methods:

- `ForController(nameof(MyController))` selects a controller
- `Allow(nameof(MyController.MyAction))` grants one action
- `AllowAll()` grants all discovered actions on the current controller
- `Deny(nameof(MyController.MyAction))` removes one action after a broader allow

### Custom Roles

You can add as many roles as needed.

```csharp
using Acme.Store.Api.Controllers;
using Acme.Store.Api.UsersAndPermissionsSystem.Entities;
using Dappi.HeadlessCms.UsersAndPermissions.Core;

namespace Acme.Store.Api.UsersAndPermissionsSystem.Permissions;

public class AdminRoleConfiguration : RoleConfiguration<AppUser>
{
    public override string RoleName => "Admin";

    public override void Configure(IControllerConfigurationBuilder<AppUser> builder)
    {
        builder
            .ForController(nameof(ProductsController))
            .AllowAll()
            .ForController(nameof(AdminUsersController))
            .AllowAll();
    }
}
```

The important part is that these classes are in an assembly that you pass to `UseUsersAndPermissionsSystem(...)`.

---

## 6. First Run Behavior

When the app starts and this line runs:

```csharp
await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
    typeof(Program).Assembly
);
```

the system:

1. discovers all `RoleConfiguration<AppUser>` classes in the supplied assembly
2. runs pending migrations for `AppUsersAndPermissionsDbContext`
3. rebuilds the available permission list from `PermissionsMeta.Controllers`
4. creates or updates roles in the database
5. removes orphaned roles that are no longer configured and are not assigned to any user

That means your source code is the source of truth for role definitions.

---

## 7. Optional: Add A Custom External JWT Provider

Use a custom `JwtValidationProvider<AppUser>` when your API should accept JWTs issued by another system directly.

For example, you might trust tokens from Auth0, Azure AD, Keycloak, or another internal identity service.

### Important Current-State Note

`AddExternalJwtProvider<TProvider, TUser>()` currently requires `new()` and creates the provider itself.

That means constructor injection is not available inside the provider today.

If you want to use `IConfiguration`, the simplest current pattern is:

1. bind your settings in `Program.cs`
2. pass them into a static initialization method on the provider before registering it

### Step 1: Add Configuration

```json
{
  "Authentication": {
    "Custom": {
      "Schema": "CustomJwt",
      "Issuer": "https://identity.acme.com",
      "Audience": "acme-store-api",
      "JwksUrl": "https://identity.acme.com/.well-known/jwks.json"
    }
  }
}
```

### Step 2: Create Options

```csharp
namespace Acme.Store.Api.UsersAndPermissionsSystem.ExternalAuth;

public sealed class CustomJwtOptions
{
    public const string SectionName = "Authentication:Custom";

    public string Schema { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string JwksUrl { get; set; } = string.Empty;
}
```

### Step 3: Create The Provider

```csharp
using System.Security.Claims;
using Acme.Store.Api.UsersAndPermissionsSystem.Entities;
using Dappi.Core.Abstractions.Auth;
using Dappi.HeadlessCms.UsersAndPermissions.Jwt;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.IdentityModel.Tokens;

namespace Acme.Store.Api.UsersAndPermissionsSystem.ExternalAuth;

public class CustomJwtValidationProvider : JwtValidationProvider<AppUser>
{
    private static CustomJwtOptions? _options;

    public static void Initialize(CustomJwtOptions options)
    {
        _options = options;
    }

    private static CustomJwtOptions Options =>
        _options ?? throw new InvalidOperationException(
            "CustomJwtValidationProvider has not been initialized."
        );

    public override SchemaAndIssuerProvider SchemaAndIssuerProvider => new()
    {
        Schema = Options.Schema,
        Issuer = Options.Issuer,
    };

    public override TokenValidationParameters BuildValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                using var client = new HttpClient();
                var jwksJson = client.GetStringAsync(Options.JwksUrl).GetAwaiter().GetResult();
                return new JsonWebKeySet(jwksJson).GetSigningKeys();
            },
            ValidIssuer = Options.Issuer,
            ValidAudiences = new[] { Options.Audience },
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    }

    public override async Task OnUserAuthenticatedAsync(
        ClaimsPrincipal principal,
        ExternalUserSyncContext<AppUser> context
    )
    {
        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
            return;

        await context.FindOrCreateByEmailAsync(email);
    }
}
```

`OnUserAuthenticatedAsync(...)` is where you connect the external identity to a local user record. `FindOrCreateByEmailAsync(...)` is the simplest current pattern and assigns the default authenticated role to new users.

### Step 4: Initialize And Register It In Program.cs

```csharp
var customJwtOptions = builder.Configuration
    .GetSection(CustomJwtOptions.SectionName)
    .Get<CustomJwtOptions>()
    ?? throw new InvalidOperationException("Authentication:Custom is missing.");

CustomJwtValidationProvider.Initialize(customJwtOptions);

builder.Services
    .AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
        PermissionsMeta.Controllers,
        builder.Configuration
    )
    .AddExternalJwtProvider<CustomJwtValidationProvider, AppUser>();
```

This keeps the provider configuration-driven while staying compatible with the current `new()` constraint in `AddExternalJwtProvider(...)`.

If the external identity system uses a symmetric signing key instead of JWKS, replace `IssuerSigningKeyResolver` with an `IssuerSigningKey` built from configuration.

---

## 8. Common Problems

### Permissions are missing

Rebuild the project so `PermissionsMeta.Controllers` is regenerated.

### Roles are not created

Make sure your role configuration classes inherit from `RoleConfiguration<AppUser>` and are in the assembly passed to:

```csharp
await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(
    typeof(Program).Assembly
);
```

### Startup fails on database initialization

Make sure the first migration for `AppUsersAndPermissionsDbContext` exists before relying on runtime migration.

### External users are created without a valid role

Make sure you configure the authenticated default role:

```csharp
public override string RoleName => UsersAndPermissionsConstants.DefaultRoles.Authenticated;
```

New users created by `FindOrCreateByEmailAsync(...)` use the role where `IsDefaultForAuthenticatedUser` is set by the framework for the authenticated default role.

---

## Recommended Setup Order

Use this order in your own project:

1. Create `AppUser`
2. Create `AppUsersAndPermissionsDbContext`
3. Register `AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(...)`
4. Add at least `PublicRoleConfiguration` and `AuthenticatedRoleConfiguration`
5. Create the first migration for `AppUsersAndPermissionsDbContext`
6. Run `dotnet ef database update --context AppUsersAndPermissionsDbContext`
7. Start the app so `UseUsersAndPermissionsSystem(...)` can sync permissions and roles
8. Add a custom `JwtValidationProvider` only if your API must trust third-party JWTs directly
