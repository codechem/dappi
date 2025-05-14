using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace Dappi.Cli;

public static class TemplateFetcher
{
    public static async Task<(string physicalPath, string tagName)> GetDappiTemplate(bool usePreRelease)
    {
        var release = await GetRelease(usePreRelease);
        var projectTemplateFilename = Path.Combine(Constants.TemplatesFileRoot, $"{Constants.TemplateName}.{release.TagName}.zip");
        
        if (File.Exists(projectTemplateFilename))
            return (projectTemplateFilename, release.TagName);

        if (!Directory.Exists(Constants.TemplatesFileRoot))
            Directory.CreateDirectory(Constants.TemplatesFileRoot);

        DownLoadZipFile(release.ZipballUrl, projectTemplateFilename);

        return (projectTemplateFilename, release.TagName);
    }

    private static async Task<Release> GetRelease(bool usePreRelease)
    {
        var github = new GitHubClient(new ProductHeaderValue(Constants.CliCommandName));
        
        if (!usePreRelease)
            return await github.Repository.Release.GetLatest(Constants.DappiRepoOwner, Constants.DappiRepoName);
       
        var releases = await github.Repository.Release
            .GetAll(Constants.DappiRepoOwner, Constants.DappiRepoName);
        var release = releases
            .Where(rel => rel.Prerelease)
            .OrderByDescending(rel => rel.PublishedAt)
            .FirstOrDefault();
            
        return release;

    }

    private static void DownLoadZipFile(string zipUrl, string filePath)
    {
        using var webClient = new WebClient();
        webClient.Headers.Add("Accept-Language", " en-US");
        webClient.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
        webClient.Headers.Add("User-Agent", Constants.CliCommandName);

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