using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dappi.Cli.Exceptions;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Dappi.Cli.Helpers;

public static class TemplateFetcher
{
    public static async Task<(string physicalPath, string tagName)> GetDappiTemplate(bool usePreRelease, ILogger logger)
    {
        var release = await GetRelease(usePreRelease);
        logger.LogDebug("Got dappi release {DappiRelease}. Is Pre-release: {IsPreRelease}", release.TagName, usePreRelease);

        var projectTemplateFilename =
            Path.Combine(Constants.TemplatesFileRoot, $"{Constants.TemplateName}.{release.TagName}.zip");

        if (File.Exists(projectTemplateFilename))
        {
            logger.LogDebug("Template folder exists on machine {TemplateFileName}", projectTemplateFilename);
            return (projectTemplateFilename, release.TagName);
        }

        if (!Directory.Exists(Constants.TemplatesFileRoot))
        {
            logger.LogDebug("Creating template root directory because it doesn't exists {TemplateFileRoot}", Constants.TemplatesFileRoot);
            Directory.CreateDirectory(Constants.TemplatesFileRoot);
        }

        DownLoadZipFile(release.ZipballUrl, projectTemplateFilename, logger);

        return (projectTemplateFilename, release.TagName);
    }

    private static async Task<Release> GetRelease(bool usePreRelease)
    {
        var github = new GitHubClient(new ProductHeaderValue(Constants.CliCommandName));
        
        if (!usePreRelease)
        {
            try
            {
                return await github.Repository.Release.GetLatest(Constants.DappiRepoOwner, Constants.DappiRepoName);
            }
            catch (ApiException e)
            {
                throw new DappiReleaseDoesNotExistException("The last release cannot be found", e);
            }
        }

        var releases = await github.Repository.Release
            .GetAll(Constants.DappiRepoOwner, Constants.DappiRepoName);
        var release = releases
            .Where(rel => rel.Prerelease)
            .OrderByDescending(rel => rel.PublishedAt)
            .FirstOrDefault();

        return release ?? throw new DappiReleaseDoesNotExistException("The last pre-release cannot be found");
    }

    private static void DownLoadZipFile(string zipUrl, string filePath, ILogger logger)
    {
#pragma warning disable SYSLIB0014
        using var webClient = new WebClient();
#pragma warning restore SYSLIB0014
        webClient.Headers.Add("Accept-Language", " en-US");
        webClient.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
        webClient.Headers.Add("User-Agent", Constants.CliCommandName);

        logger?.LogDebug("Start download {ZipUrl}", zipUrl);
        Console.WriteLine($"Start download zip file:{zipUrl}");
        Console.WriteLine($"Downloading...");

        webClient.DownloadFile(zipUrl, filePath);
        logger?.LogDebug("Downloaded {ZipUrl} to {FilePath}", zipUrl, filePath);
    }
}