using System.ComponentModel;
using System.Diagnostics;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.Models;
using Microsoft.Extensions.Options;

namespace Dappi.HeadlessCms.Services;

// Pushes the model changes made in the CMS to git instead of migrating in place.
// The hosting platform redeploys on push and the new instance applies the migration on startup.
public class GitPublishService(IOptions<DappiGitOptions> options)
{
    private readonly DappiGitOptions _options = options.Value;

    public bool IsEnabled => _options.IsEnabled;

    // Returns false when there was nothing new to push.
    public async Task<bool> PublishAsync(
        string migrationName,
        string dbContextName,
        bool modelChanged
    )
    {
        var projectDir = Directory.GetCurrentDirectory();
        var repoDir = (await Git(projectDir, "rev-parse", "--show-toplevel")).Trim();

        // After a failed push the changes are already committed, so don't add a second migration.
        var hasChanges =
            await Git(projectDir, ["status", "--porcelain", "--", .. CmsFolders()]) != "";
        if (hasChanges && modelChanged)
        {
            await Run(
                "dotnet",
                projectDir,
                "ef",
                "migrations",
                "add",
                migrationName,
                "--context",
                dbContextName
            );
        }

        await Git(repoDir, "checkout", "-B", _options.Branch);
        await Git(projectDir, ["add", "-A", "--", .. CmsFolders()]);
        if (await Git(repoDir, "diff", "--cached", "--name-only") != "")
        {
            await Git(repoDir, "commit", "-m", "CMS: publish model changes");
        }

        try
        {
            await Git(repoDir, "pull", "--rebase", RemoteUrl, _options.Branch);
        }
        catch (GitPublishException e)
        {
            if (Directory.Exists(Path.Combine(repoDir, ".git", "rebase-merge")))
            {
                await Git(repoDir, "rebase", "--abort");
            }

            throw new GitPublishException(
                $"Could not get the latest changes from the repository, nothing was pushed. {e.Message}"
            );
        }

        if ((await Git(repoDir, "rev-list", "--count", "FETCH_HEAD..HEAD")).Trim() == "0")
        {
            return false;
        }

        await Git(repoDir, "push", RemoteUrl, $"HEAD:{_options.Branch}");
        return true;
    }

    // Only the files the CMS edits get published, never uploads or other files the app writes at runtime.
    private static string[] CmsFolders() =>
        new[] { "Entities", "Enums", "Data", "Migrations" }.Where(Directory.Exists).ToArray();

    private string RemoteUrl =>
        new UriBuilder(_options.RepoUrl!) { UserName = "oauth2", Password = _options.Token }
            .Uri
            .AbsoluteUri;

    // safe.directory: the checkout can be owned by a different user than the app (e.g. in a container).
    private Task<string> Git(string dir, params string[] args) =>
        Run("git", dir, ["-c", "safe.directory=*", .. args]);

    private async Task<string> Run(string fileName, string dir, params string[] args)
    {
        var startInfo = new ProcessStartInfo(fileName, args)
        {
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_AUTHOR_NAME"] = "Dappi CMS";
        startInfo.Environment["GIT_AUTHOR_EMAIL"] = "cms@dappi.local";
        startInfo.Environment["GIT_COMMITTER_NAME"] = "Dappi CMS";
        startInfo.Environment["GIT_COMMITTER_EMAIL"] = "cms@dappi.local";

        using var process = Start(startInfo);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var message = (error + await output).Trim().Replace(_options.Token!, "***");
            throw new GitPublishException($"{fileName} failed: {message}");
        }

        return await output;
    }

    private static Process Start(ProcessStartInfo startInfo)
    {
        try
        {
            return Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            throw new GitPublishException($"{startInfo.FileName} is not installed on the server.");
        }
    }
}
