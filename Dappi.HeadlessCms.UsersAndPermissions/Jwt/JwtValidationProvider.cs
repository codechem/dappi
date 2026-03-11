using System.Security.Claims;
using Dappi.Core.Abstractions.Auth;
using Dappi.HeadlessCms.UsersAndPermissions.Core;
using Dappi.HeadlessCms.UsersAndPermissions.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Dappi.HeadlessCms.UsersAndPermissions.Jwt;

/// <summary>
/// Describes an JWT provider whose tokens your API should accept directly —
/// no exchange endpoint required. The token the client already holds (e.g. a Strapi
/// JWT backed by Google, or any other system) is validated on every request by
/// ASP.NET Core's JWT bearer middleware using the parameters you supply here.
///
/// <para>
/// Subclass once per external system and register it with
/// <c>services.AddExternalJwtProvider&lt;TUser, TProvider&gt;()</c>.
/// </para>
///
/// <para>
/// Because external tokens are validated by middleware, no sign-in callback to
/// Dappi is needed. However, you almost certainly want to ensure the caller is
/// represented as a local user in the Dappi database. Override
/// <see cref="OnUserAuthenticatedAsync"/> for this: it is called the first time
/// a valid token arrives and lets you create/sync the user.
/// </para>
/// </summary>
/// <typeparam name="TUser">The application user type.</typeparam>
public abstract class JwtValidationProvider<TUser>
    where TUser : AppUser, new()
{
    /// <summary>
    /// Provides the JWT bearer scheme name and expected issuer (<c>iss</c> claim) for
    /// this external provider.
    ///
    /// <para>
    /// The returned instance is registered as a singleton in the DI container so that
    /// the Dappi scheme-selector policy can resolve all providers at runtime and route
    /// each incoming token to the correct named bearer scheme based on its issuer.
    /// </para>
    ///
    /// <example>
    /// <code>
    /// public override SchemaAndIssuerProvider SchemaAndIssuerProvider => new()
    /// {
    ///     Schema = "StrapiGoogle",
    ///     Issuer = "https://strapi.mycompany.com"
    /// };
    /// </code>
    /// </example>
    /// </summary>
    public abstract SchemaAndIssuerProvider SchemaAndIssuerProvider { get; }

    /// <summary>
    /// Return the <see cref="TokenValidationParameters"/> used by the JWT bearer
    /// middleware when validating incoming tokens for this provider.
    /// </summary>
    public abstract TokenValidationParameters BuildValidationParameters();

    /// <summary>
    /// Called by the <c>OnTokenValidated</c> event after a token from this provider
    /// has been successfully validated by the middleware.
    ///
    /// <para>
    /// Use this to find-or-create the user in the Dappi database so that your
    /// permission system has a local identity to work with. If you don't need
    /// local user provisioning you can leave this as a no-op (the default).
    /// </para>
    ///
    /// <para>
    /// A future sign-in callback (OAuth redirect flow) will be added separately;
    /// this hook covers the bearer-token-only scenario.
    /// </para>
    /// </summary>
    /// <param name="principal">The validated <see cref="ClaimsPrincipal"/>.</param>
    /// <param name="context">
    /// Helper that gives access to <see cref="UserManager{TUser}"/>
    /// and <see cref="DbContext"/> for user provisioning.
    /// </param>
    public virtual Task OnUserAuthenticatedAsync(
        ClaimsPrincipal principal,
        ExternalUserSyncContext<TUser> context
    ) => Task.CompletedTask;
}
