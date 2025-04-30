using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace Dappi.Cli;

public static class TemplateFetcher
{
    public static async Task<string> GetDappiTemplate()
    {
        var release = await GetRelease();
        var projectTemplateFilename = Path.Combine(Constants.TemplatesFileRoot, $"{Constants.TemplateName}.{release.TagName}.zip");
        
        if (File.Exists(projectTemplateFilename))
            return projectTemplateFilename;

        if (!Directory.Exists(Constants.TemplatesFileRoot))
            Directory.CreateDirectory(Constants.TemplatesFileRoot);

        DownLoadZipFile(release.ZipballUrl, projectTemplateFilename);

        return projectTemplateFilename;
    }

    private static async Task<Release> GetRelease()
    {
        var github = new GitHubClient(new ProductHeaderValue(Constants.CliCommandName));
        
        // TODO: Remove this constant - just easier for now to switch on/off while we're testing.
        if (Constants.UsePrerelease)
        {
            var releases = await github.Repository.Release
                .GetAll(Constants.DappiRepoOwner, Constants.DappiRepoName);
            var release = releases
                .Where(rel => rel.Prerelease)
                .OrderByDescending(rel => rel.PublishedAt)
                .FirstOrDefault();
            
            return release;
        }
        
        return await github.Repository.Release.GetLatest(Constants.DappiRepoOwner, Constants.DappiRepoName);
    }

    private static void DownLoadZipFile(string zipUrl, string filePath)
    {
        using var webClient = new WebClient();
        webClient.Headers.Add("Accept-Language", " en-US");
        webClient.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
        webClient.Headers.Add("User-Agent", "MiConsolesApplicationes");

        Console.WriteLine($"Start download zip file:{zipUrl}");
        Console.WriteLine($"Downloading...");

        try
        {
            webClient.DownloadFile(zipUrl, filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }

        Console.WriteLine($"Download success and save as {filePath}");
    }
}