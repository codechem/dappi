namespace Dappi.Core.Abstractions.Auth;

/// <summary>
/// Describes the JWT bearer scheme name and expected token issuer (<c>iss</c> claim)
/// for a single external authentication provider.
///
/// <para>
/// An instance is returned by the <c>SchemaAndIssuerProvider</c> property of an
/// <c>JwtValidationProvider&lt;TUser&gt;</c> subclass and is registered as a singleton
/// in the DI container by <c>AddExternalJwtProvider&lt;TUser, TProvider&gt;</c>.
/// </para>
///
/// <para>
/// At runtime the Dappi scheme-selector policy reads all registered
/// <see cref="SchemaAndIssuerProvider"/> singletons and matches the incoming
/// token's <c>iss</c> claim against <see cref="Issuer"/> to decide which named
/// JWT bearer scheme should validate the request.
/// </para>
/// </summary>
public class SchemaAndIssuerProvider
{
    /// <summary>
    /// The unique name used when registering the JWT bearer authentication scheme
    /// (e.g. <c>"StrapiGoogle"</c>). Must match the scheme name passed to
    /// <c>AddJwtBearer(schemeName, ...)</c> and should be distinct from
    /// <c>Dappi.UsersAndPermissions</c> and every other external provider.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// The <c>iss</c> (issuer) claim value that tokens from this provider carry
    /// (e.g. <c>"https://strapi.mycompany.com"</c>).
    /// The scheme selector compares this value against the incoming token's issuer
    /// to route the request to the correct bearer scheme without fully validating
    /// the token first.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
}
