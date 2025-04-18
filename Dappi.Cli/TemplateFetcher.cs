using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dappi.Cli;

public class TemplateFetcher
{
    public async Task<string> GetDappiTemplate()
    {
        string owner = "codechem";
        string repo = "dappi";
        string branch = "main"; // change if needed
        string zipUrl = $"https://github.com/{owner}/{repo}/archive/refs/heads/{branch}.zip";
        string outputFile = $"{repo}-{branch}.zip";

        Console.WriteLine($"Downloading {zipUrl}...");

        using (HttpClient client = new HttpClient())
        {
            using (HttpResponseMessage response = await client.GetAsync(zipUrl))
            {
                response.EnsureSuccessStatusCode();

                await using (var fs = new FileStream(outputFile, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                Console.WriteLine($"Downloaded repository to: {Path.GetFullPath(outputFile)}");
            }
        }
        
        return string.Empty;
    }
}