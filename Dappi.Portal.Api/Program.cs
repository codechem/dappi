using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dappi.Portal.Api;

var builder = WebApplication.CreateBuilder(args);
foreach (var key in new[] { "Portal:Password", "Dokploy:Url", "Dokploy:ApiKey", "Apps:BaseDomain" })
{
    if (string.IsNullOrEmpty(builder.Configuration[key]))
    {
        throw new Exception($"Set {key} in configuration (user secrets or environment variables).");
    }
}

builder.Services.AddSingleton<Dokploy>();
builder.Services.AddSingleton<Provisioner>();

var app = builder.Build();

// One shared admin password, checked with HTTP Basic auth: the browser shows its own login box.
var credentials = Encoding.UTF8.GetBytes($"admin:{app.Configuration["Portal:Password"]}");
var expected = Encoding.UTF8.GetBytes($"Basic {Convert.ToBase64String(credentials)}");
app.Use(
    async (context, next) =>
    {
        var actual = Encoding.UTF8.GetBytes(context.Request.Headers.Authorization.ToString());
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Dappi Portal\"";
            context.Response.StatusCode = 401;
            return;
        }

        await next();
    }
);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/apps", (Provisioner provisioner) => provisioner.List());

app.MapPost(
    "/api/apps",
    async (CreateAppRequest request, Provisioner provisioner) =>
    {
        if (!Regex.IsMatch(request.Name, "^[a-z][a-z0-9-]{1,30}[a-z0-9]$"))
        {
            return Results.BadRequest(
                "Use 3-32 lowercase letters, digits or hyphens, starting with a letter."
            );
        }

        if (
            !Uri.TryCreate(request.RepoUrl, UriKind.Absolute, out var repo)
            || !repo.Scheme.StartsWith("http")
        )
        {
            return Results.BadRequest("Enter the GitLab repository's https URL.");
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Results.BadRequest("Enter a project access token.");
        }

        if ((await provisioner.List()).Any(a => a.Name == request.Name))
        {
            return Results.BadRequest($"An app named {request.Name} already exists.");
        }

        // Only scheme, host and path: a pasted "#" or "?..." breaks the git URL.
        var repoUrl = repo.GetLeftPart(UriPartial.Path).TrimEnd('/');
        provisioner.StartCreate(request.Name, repoUrl, request.Token.Trim());
        return Results.Accepted();
    }
);

app.MapDelete(
    "/api/apps/{name}",
    async (string name, Provisioner provisioner) =>
    {
        if (provisioner.IsCreating(name))
        {
            return Results.BadRequest("Wait until the app is created, then delete it.");
        }

        await provisioner.Delete(name);
        return Results.NoContent();
    }
);

app.Run();

record CreateAppRequest(string Name, string RepoUrl, string Token);
