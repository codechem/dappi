# UsersAndPermissions System - Setup & Usage Guide

## Overview

The **UsersAndPermissions System** is a comprehensive role-based access control (RBAC) framework integrated into Dappi. It allows you to define roles, permissions, and control which authenticated users can access specific API endpoints and methods.

---

## Key Components

### 1. **AppUser Entity** (Your Application)
Implements the `IAppUser` interface and represents an authenticated user in your system.

```csharp
public class AppUser : IAppUser
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
}
```

**Key Properties:**
- `Id`: Unique identifier for the user
- `RoleId`: Foreign key to the `AppRole` (defines user's role)
- `Role`: Navigation property to the user's assigned role

### 2. **AppUsersAndPermissionsDbContext** (Your Application)
A custom DbContext that inherits from `UsersAndPermissionsDbContext`.

```csharp
public class AppUsersAndPermissionsDbContext(DbContextOptions<AppUsersAndPermissionsDbContext> options) 
    : UsersAndPermissionsDbContext(options)
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

**Inherited DbSets:**
- `AppPermissions`: Database representation of available permissions
- `AppRoles`: Database representation of roles and their assigned permissions

### 3. **Core Models** (From Dappi.HeadlessCms.UsersAndPermissions)

#### `AppRole`
Represents a role in your application with associated permissions.
- **Properties:** `Id`, `Name`, `Permissions` (collection), `Users` (collection), `IsDefaultForAuthenticatedUser`
- **Methods:** `AddPermission()`, `ClearPermissions()`, factory methods for default roles

#### `AppPermission`
Represents an individual permission that can be granted to a role.
- **Properties:** `Id`, `Name`, `Description`

#### `IAppUser`
Interface that your user entity must implement.

```csharp
public interface IAppUser
{
    int RoleId { get; }
    AppRole? Role { get; }
}
```

---

## Setup Instructions

### Step 1: Implement IAppUser in Your User Entity

Create an `AppUser` entity that implements `IAppUser`:

```csharp
using Dappi.HeadlessCms.UsersAndPermissions.Core;

public class AppUser : IAppUser
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
}
```

### Step 2: Create AppUsersAndPermissionsDbContext

Create a custom DbContext that inherits from `UsersAndPermissionsDbContext`:

```csharp
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Microsoft.EntityFrameworkCore;

public class AppUsersAndPermissionsDbContext(DbContextOptions<AppUsersAndPermissionsDbContext> options) 
    : UsersAndPermissionsDbContext(options)
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

**Important:** 
- The base class configures the `UsersAndPermissions` schema
- It automatically manages `AppPermissions` and `AppRoles` DbSets
- Call `base.OnModelCreating()` to ensure proper configuration

### Step 3: Create Database Migration

Generate an EF Core migration for the UsersAndPermissions DbContext:

```bash
dotnet ef migrations add UsersAndPermissions \
  --context AppUsersAndPermissionsDbContext \
  --output-dir Migrations/AppUsersAndPermissionsDb
```

This creates:
- Migration files in `Migrations/AppUsersAndPermissionsDb/` folder
- A separate migration path for the UsersAndPermissions schema

### Step 4: Configure Services in Program.cs

Add the UsersAndPermissions System to your dependency injection container:

```csharp
using Dappi.HeadlessCms;
using Dappi.HeadlessCms.UsersAndPermissions;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using GeneratedPermissions;
using MyCompany.MyProject.WebApi.Data;
using MyCompany.MyProject.WebApi.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add core Dappi services
builder.Services.AddDappi<AppDbContext>(builder.Configuration);
builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

// Add UsersAndPermissions System
builder.Services.AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext>(
    PermissionsMeta.Controllers, 
    builder.Configuration
);

builder.Services.AddDappiSwaggerGen();

var app = builder.Build();
```

**Key Parameters:**
- `AppUsersAndPermissionsDbContext`: Your custom DbContext
- `PermissionsMeta.Controllers`: Auto-generated metadata about all available controller methods
- `builder.Configuration`: Configuration object

### Step 5: Define Roles and Permissions

Before building the application, define which roles have access to which endpoints:

```csharp
var permBuilder = new AppRoleAndPermissionsBuilder<AppUser>();

permBuilder
    .ForRole(UsersAndPermissionsConstants.DefaultRoles.Authenticated)
    .ForController(nameof(TestController))
    .Allow(nameof(TestController.GetAllTests))
    .Allow(nameof(TestController.TestCustomMethod))
    .And()
    .ForRole("Admin")
    .ForController(nameof(TestController))
    .AllowAll();

await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(permBuilder);
```

**Builder Methods:**
- `ForRole(string roleName, bool isDefaultForAuthenticated = false)`: Define a new role
- `ForController(string controllerName)`: Specify which controller to configure
- `Allow(string methodName)`: Grant permission for a specific method
- `AllowAll()`: Grant permission for all methods in the controller
- `Deny(string methodName)`: Explicitly deny a method (useful for AllowAll with exceptions)
- `And()`: Move to the next role definition

**Default Roles:**
- `UsersAndPermissionsConstants.DefaultRoles.Public`: For unauthenticated users
- `UsersAndPermissionsConstants.DefaultRoles.Authenticated`: For authenticated users

### Step 6: Apply Migrations and Run

The `UseUsersAndPermissionsSystem` extension method automatically:
1. Runs pending migrations on the UsersAndPermissions database
2. Synchronizes permissions from controllers to the database
3. Creates/updates roles based on your builder configuration

```csharp
await app.UseDappi<AppDbContext>();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
```

---

## Permission Synchronization

### How It Works

Every time your application starts, the permission system:

1. **Scans all controllers** using `PermissionsMeta.Controllers` (generated by the source generator)
2. **Registers permissions** in the database for each controller method
3. **Synchronizes roles** based on your `AppRoleAndPermissionsBuilder` configuration
4. **Removes orphaned roles** that aren't referenced in your builder and have no users

### Automatic Permission Detection

The `PermissionsMeta.Controllers` is automatically generated and contains:
- All public controller methods
- HTTP method mappings
- Full route information

No manual permission registration is needed!

---

## Extension Methods

### `AddUsersAndPermissionsSystem<TDbContext>`
Configures dependency injection for the UsersAndPermissions System.

**Parameters:**
- `services`: IServiceCollection
- `controllerRoutes`: IReadOnlyDictionary<string, IReadOnlyList<MethodRouteEntry>> (from PermissionsMeta)
- `configuration`: IConfiguration

**What it does:**
- Registers the DbContext
- Configures PostgreSQL connection
- Registers `DbContextAccessor<TDbContext>`
- Registers `AvailablePermissionsRepository`
- Adds controllers with proper JSON serialization settings

### `UseUsersAndPermissionsSystem<TDbContext, TUser>`
Initializes the UsersAndPermissions System at runtime.

**Parameters:**
- `app`: WebApplication
- `appRoleAndPermissionsBuilder`: AppRoleAndPermissionsBuilder<TUser>

**What it does:**
1. Creates a scope and retrieves the DbContext
2. Runs all pending migrations
3. Clears and reloads permissions from controllers
4. Synchronizes roles with the builder configuration
5. Maps role controllers
6. Returns the app for method chaining

---

## Database Schema

### UsersAndPermissions Schema

The system creates the following tables in the `UsersAndPermissions` schema:

#### `AppPermissions`
```
Id (int, PK)
Name (string, required)
Description (string, required)
```

#### `AppRoles`
```
Id (int, PK)
Name (string, required)
IsDefaultForAuthenticatedUser (bool)
```

#### `AppPermissionAppRole` (Junction table)
```
PermissionsId (int, FK -> AppPermissions)
RolesId (int, FK -> AppRoles)
```

#### `AppUsers` (In your schema)
```
Id (int, PK)
RoleId (int, FK -> AppRoles.Id)
```

---

## Example: Complete Setup

### 1. Define your AppUser
```csharp
// Entities/AppUser.cs
using Dappi.HeadlessCms.UsersAndPermissions.Core;

public class AppUser : IAppUser
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
}
```

### 2. Create DbContext
```csharp
// Data/AppUsersAndPermissionsDbContext.cs
using Dappi.HeadlessCms.UsersAndPermissions.Database;
using Microsoft.EntityFrameworkCore;

public class AppUsersAndPermissionsDbContext(DbContextOptions<AppUsersAndPermissionsDbContext> options) 
    : UsersAndPermissionsDbContext(options)
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

### 3. Configure Program.cs
```csharp
// Program.cs
using Dappi.HeadlessCms;
using Dappi.HeadlessCms.UsersAndPermissions;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using GeneratedPermissions;
using MyCompany.MyProject.WebApi.Data;
using MyCompany.MyProject.WebApi.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDappi<AppDbContext>(builder.Configuration);
builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);
builder.Services.AddUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext>(
    PermissionsMeta.Controllers, 
    builder.Configuration
);
builder.Services.AddDappiSwaggerGen();

var app = builder.Build();

// Define roles and permissions
var permBuilder = new AppRoleAndPermissionsBuilder<AppUser>();

permBuilder
    .ForRole(UsersAndPermissionsConstants.DefaultRoles.Authenticated)
    .ForController(nameof(ProductController))
    .Allow(nameof(ProductController.GetAllProducts))
    .Allow(nameof(ProductController.GetProductById))
    .And()
    .ForRole("Admin")
    .ForController(nameof(ProductController))
    .AllowAll()
    .And()
    .ForRole("Editor")
    .ForController(nameof(ProductController))
    .Allow(nameof(ProductController.CreateProduct))
    .Allow(nameof(ProductController.UpdateProduct))
    .Allow(nameof(ProductController.DeleteProduct));

// Initialize the system
await app.UseUsersAndPermissionsSystem<AppUsersAndPermissionsDbContext, AppUser>(permBuilder);

// Rest of configuration
await app.UseDappi<AppDbContext>();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
```

### 4. Create Migration
```bash
dotnet ef migrations add UsersAndPermissions \
  --context AppUsersAndPermissionsDbContext \
  --output-dir Migrations/AppUsersAndPermissionsDb
```

---

## Accessing the System at Runtime

### DbContext Accessor
You can inject `IDbContextAccessor` into your services to access the UsersAndPermissions DbContext:

```csharp
public class MyService
{
    private readonly IDbContextAccessor _dbContextAccessor;

    public MyService(IDbContextAccessor dbContextAccessor)
    {
        _dbContextAccessor = dbContextAccessor;
    }

    public async Task<List<AppRole>> GetAllRoles()
    {
        var db = _dbContextAccessor.GetContext<AppUsersAndPermissionsDbContext>();
        return await db.AppRoles.ToListAsync();
    }
}
```

### Available Permissions Repository
Access the automatically-discovered permissions:

```csharp
public class AuthorizationService
{
    private readonly AvailablePermissionsRepository _permissionsRepository;

    public AuthorizationService(AvailablePermissionsRepository permissionsRepository)
    {
        _permissionsRepository = permissionsRepository;
    }

    public var allPermissions = _permissionsRepository.GetAllPermissions();
}
```

---

## Best Practices

### 1. **Inherit from UsersAndPermissionsDbContext**
Always inherit your custom DbContext from `UsersAndPermissionsDbContext` to get automatic table configuration.

### 2. **Call `base.OnModelCreating()`**
Always call the base implementation in your `OnModelCreating` method to ensure the UsersAndPermissions schema is properly configured.

### 3. **Use Separate Migration Folder**
Keep migrations for the UsersAndPermissions DbContext in a separate folder (`Migrations/AppUsersAndPermissionsDb/`) for better organization.

### 4. **Define Roles at Startup**
Configure all roles and permissions in your `Program.cs` before calling `UseUsersAndPermissionsSystem`. This ensures consistency across deployments.

### 5. **Use Default Roles**
Leverage the built-in default roles (`Public`, `Authenticated`) for common scenarios instead of creating custom ones.

### 6. **Fluent Configuration**
Use the fluent builder pattern for clean, readable role configuration:

```csharp
permBuilder
    .ForRole("Admin")
    .ForController("Users").AllowAll()
    .And()
    .ForRole("User")
    .ForController("Products").Allow("GetAll")
    .And();
```

---

## Troubleshooting

### Issue: Migrations not applied
**Solution:** Ensure `UseUsersAndPermissionsSystem` is called before `MapControllers()`. The extension method automatically applies migrations.

### Issue: Permissions not syncing
**Solution:** Verify that `PermissionsMeta.Controllers` is properly generated. Rebuild the project if needed to trigger the source generator.

### Issue: Role not found in database
**Solution:** Ensure the role is defined in the `AppRoleAndPermissionsBuilder` and the application has started at least once to synchronize the database.

### Issue: User has multiple roles
**Solution:** The current design supports one role per user (via `RoleId`). Implement a role-assignment service to manage role changes.

---

## Next Steps

1. Implement authentication to populate `AppUser` entities
2. Create authorization policies that check user roles
3. Implement a UI for managing roles and permissions (optional)
4. Monitor permission changes across deployments

---

## Related Documentation

- [Dappi Core Documentation](./README.md)
- [UsersAndPermissions API Reference](./Dappi.HeadlessCms.UsersAndPermissions/)
- Entity Framework Core Migrations: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/

