using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace Dappi.Portal.Api;

public record AppInfo(
    string Name,
    string Status,
    string? Error,
    string CmsUrl,
    string? RepoUrl,
    string? DokployUrl
);

public class Provisioner(Dokploy dokploy, IConfiguration config, ILogger<Provisioner> logger)
{
    private const string RepoPrefix = "Dappi Portal app, repo: ";

    private readonly ConcurrentDictionary<string, (string Status, string? Error)> _creating = new();

    private bool UseHttps => config.GetValue("Apps:UseHttps", true);

    public string CmsUrl(string name) =>
        $"{(UseHttps ? "https" : "http")}://{name}.{config["Apps:BaseDomain"]}";

    public bool IsCreating(string name) =>
        _creating.TryGetValue(name, out var app) && app.Error is null;

    public void StartCreate(string name, string repoUrl, string token)
    {
        _creating[name] = ("creating", null);
        Task.Run(async () =>
        {
            try
            {
                await Create(name, repoUrl, token);
                _creating.TryRemove(name, out _);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Creating {App} failed", name);
                _creating[name] = ("failed", e.Message.Replace(token, "***"));
            }
        });
    }

    public async Task<List<AppInfo>> List()
    {
        var apps = new List<AppInfo>();
        foreach (var project in (await dokploy.Get("project.all"))!.AsArray())
        {
            var description = (string?)project!["description"] ?? "";
            if (!description.StartsWith(RepoPrefix))
            {
                continue;
            }

            var name = (string)project["name"]!;
            var environment = project["environments"]?.AsArray().FirstOrDefault();
            var status = (string?)
                environment
                    ?["applications"]?.AsArray()
                    .FirstOrDefault()
                    ?["applicationStatus"] switch
            {
                "done" => "running",
                "running" => "deploying",
                "error" => "deploy failed",
                _ => "not deployed",
            };
            var dokployUrl =
                $"{dokploy.Url}/dashboard/project/{project["projectId"]}/environment/{environment?["environmentId"]}";

            apps.Add(
                new AppInfo(
                    name,
                    status,
                    null,
                    CmsUrl(name),
                    description[RepoPrefix.Length..],
                    dokployUrl
                )
            );
        }

        // An app that is being created (or failed) shows its step or error instead.
        foreach (var (name, (status, error)) in _creating)
        {
            apps.RemoveAll(a => a.Name == name);
            apps.Add(new AppInfo(name, status, error, CmsUrl(name), null, null));
        }

        return apps.OrderBy(a => a.Name).ToList();
    }

    public async Task Delete(string name)
    {
        var project = (await dokploy.Get("project.all"))!
            .AsArray()
            .FirstOrDefault(p =>
                (string?)p!["name"] == name
                && ((string?)p["description"] ?? "").StartsWith(RepoPrefix)
            );

        if (project is not null)
        {
            // Removing only the project leaves the app and database containers running.
            foreach (var environment in project["environments"]?.AsArray() ?? [])
            {
                foreach (var app in environment!["applications"]?.AsArray() ?? [])
                {
                    await dokploy.Post(
                        "application.delete",
                        new { applicationId = (string)app!["applicationId"]! }
                    );
                }

                foreach (var db in environment["postgres"]?.AsArray() ?? [])
                {
                    await dokploy.Post(
                        "postgres.remove",
                        new { postgresId = (string)db!["postgresId"]! }
                    );
                }
            }

            await dokploy.Post("project.remove", new { projectId = (string)project["projectId"]! });
        }

        _creating.TryRemove(name, out _);
    }

    private async Task Create(string name, string repoUrl, string token)
    {
        var gitUrl = new UriBuilder(repoUrl) { UserName = "oauth2", Password = token }
            .Uri
            .AbsoluteUri;
        var workDir = Path.Combine(
            Path.GetTempPath(),
            "dappi-portal",
            $"{name}-{Guid.NewGuid():N}"
        );

        try
        {
            _creating[name] = ("generating project", null);
            var projectDir = await GenerateProject(name, workDir);

            _creating[name] = ("pushing to GitLab", null);
            await Run("git", projectDir, "init", "-b", "main");
            await Run("git", projectDir, "add", "-A");
            await Run("git", projectDir, "commit", "-m", "Initial Dappi project");
            await Run("git", projectDir, "push", gitUrl, "main");

            _creating[name] = ("creating Dokploy project", null);
            var created = await dokploy.Post(
                "project.create",
                new { name, description = RepoPrefix + repoUrl }
            );
            var projectId = (string)created!["project"]!["projectId"]!;
            var environmentId = (string)created["environment"]!["environmentId"]!;

            _creating[name] = ("creating database", null);
            var dbPassword = RandomNumberGenerator.GetHexString(32);
            var db = await dokploy.Post(
                "postgres.create",
                new
                {
                    name = $"{name}-db",
                    appName = $"{name}-db",
                    databaseName = "app",
                    databaseUser = "app",
                    databasePassword = dbPassword,
                    dockerImage = "postgres:16",
                    projectId,
                    environmentId,
                }
            );
            await dokploy.Post("postgres.deploy", new { postgresId = (string)db!["postgresId"]! });

            _creating[name] = ("creating app", null);
            var app = await dokploy.Post(
                "application.create",
                new
                {
                    name,
                    appName = name,
                    projectId,
                    environmentId,
                }
            );
            var applicationId = (string)app!["applicationId"]!;

            // Dokploy rejects these calls unless every field is sent, even the unused ones.
            await dokploy.Post(
                "application.saveBuildType",
                new
                {
                    applicationId,
                    buildType = "dockerfile",
                    dockerfile = "Dockerfile",
                    dockerContextPath = ".",
                    dockerBuildStage = "",
                    herokuVersion = "24",
                    railpackVersion = "0.2.2",
                }
            );
            await dokploy.Post(
                "application.saveGitProvider",
                new
                {
                    applicationId,
                    customGitUrl = gitUrl,
                    customGitBranch = "main",
                    customGitBuildPath = "/",
                    customGitSSHKeyId = (string?)null,
                    watchPaths = Array.Empty<string>(),
                    enableSubmodules = false,
                }
            );
            await dokploy.Post(
                "application.saveEnvironment",
                new
                {
                    applicationId,
                    env = string.Join(
                        '\n',
                        $"Dappi__PostgresConnection=Host={db["appName"]};Port=5432;Database=app;Username=app;Password={dbPassword}",
                        $"Dappi__FrontendUrl={CmsUrl(name)}",
                        $"Authentication__Dappi__SecretKey={RandomNumberGenerator.GetHexString(64)}",
                        $"Dappi__Git__RepoUrl={repoUrl}",
                        $"Dappi__Git__Token={token}"
                    ),
                    buildArgs = "",
                    buildSecrets = "",
                    createEnvFile = false,
                }
            );
            await dokploy.Post(
                "domain.create",
                new
                {
                    applicationId,
                    host = new Uri(CmsUrl(name)).Host,
                    path = "/",
                    port = 8080,
                    https = UseHttps,
                    certificateType = UseHttps ? "letsencrypt" : "none",
                    domainType = "application",
                }
            );
            await dokploy.Post("application.update", new { applicationId, autoDeploy = true });

            _creating[name] = ("adding GitLab webhook", null);
            await AddGitLabWebhook(
                repoUrl,
                token,
                $"{dokploy.Url}/api/deploy/{app["refreshToken"]}"
            );

            _creating[name] = ("deploying", null);
            await dokploy.Post("application.deploy", new { applicationId });
        }
        finally
        {
            DeleteFolder(workDir);
        }
    }

    // Runs `dappi init` and adds a Dockerfile, .dockerignore and .gitignore. Returns the project root.
    private static async Task<string> GenerateProject(string name, string workDir)
    {
        var dotnetName = string.Concat(
            name.Split('-').Select(part => char.ToUpper(part[0]) + part[1..])
        ); // my-blog -> MyBlog
        await Run(
            "dappi",
            workDir,
            "init",
            "--name",
            dotnetName,
            "--path",
            workDir,
            "--use-prerelease"
        );

        var root = Path.Combine(workDir, dotnetName);
        var csproj = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories).Single();
        var webApiDir = Path.GetRelativePath(root, Path.GetDirectoryName(csproj)!)
            .Replace('\\', '/');

        File.WriteAllText(Path.Combine(root, "Dockerfile"), Dockerfile(webApiDir));
        File.WriteAllText(Path.Combine(root, ".dockerignore"), "**/bin/\n**/obj/\n");
        File.WriteAllText(Path.Combine(root, ".gitignore"), "bin/\nobj/\n.vs/\n.idea/\n*.user\n");
        return root;
    }

    // The SDK image, because Publish in the CMS runs `dotnet ef` and git inside the container.
    // --no-launch-profile stops launchSettings.json from changing the port and environment.
    private static string Dockerfile(string webApiDir) =>
        $"""
            FROM mcr.microsoft.com/dotnet/sdk:9.0
            RUN dotnet tool install --global dotnet-ef --version 9.0.6
            ENV PATH="$PATH:/root/.dotnet/tools" ASPNETCORE_HTTP_PORTS=8080
            WORKDIR /src
            COPY . .
            RUN dotnet build "{webApiDir}" -c Release
            WORKDIR /src/{webApiDir}
            EXPOSE 8080
            ENTRYPOINT ["dotnet", "run", "-c", "Release", "--no-build", "--no-launch-profile"]

            """;

    // A push to main calls Dokploy's deploy webhook.
    private static async Task AddGitLabWebhook(string repoUrl, string token, string webhookUrl)
    {
        var repo = new Uri(repoUrl);
        using var gitLab = new HttpClient
        {
            BaseAddress = new Uri($"{repo.GetLeftPart(UriPartial.Authority)}/api/v4/"),
        };
        gitLab.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);

        var projectPath = Uri.EscapeDataString(repo.AbsolutePath.Trim('/').Replace(".git", ""));
        var response = await gitLab.PostAsJsonAsync(
            $"projects/{projectPath}/hooks",
            new
            {
                url = webhookUrl,
                push_events = true,
                push_events_branch_filter = "main",
            }
        );
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Adding the GitLab webhook failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}"
            );
        }
    }

    private static async Task Run(string program, string dir, params string[] args)
    {
        Directory.CreateDirectory(dir);
        var startInfo = new ProcessStartInfo(program, args)
        {
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_AUTHOR_NAME"] = "Dappi Portal";
        startInfo.Environment["GIT_AUTHOR_EMAIL"] = "portal@dappi.local";
        startInfo.Environment["GIT_COMMITTER_NAME"] = "Dappi Portal";
        startInfo.Environment["GIT_COMMITTER_EMAIL"] = "portal@dappi.local";

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new Exception($"{program} {args[0]} failed: {(error + await output).Trim()}");
        }
    }

    private void DeleteFolder(string dir)
    {
        try
        {
            // git makes its object files read-only, and Directory.Delete can't remove those on Windows.
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(dir, recursive: true);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Could not delete {Folder}", dir);
        }
    }
}
