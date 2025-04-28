using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;
using FileMode = System.IO.FileMode;

namespace Dappi.Cli;

public class TemplateFetcher
{
    private static readonly string TemplatesFileRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $".{Path.DirectorySeparatorChar}Dappi");
    private static readonly string TemplateName = Path.Combine(TemplatesFileRoot, "MyCompany.MyProject.WebApi");

    public async Task<string> GetDappiTemplate()
    {
        var projectTemplateFilename = Path.Combine(TemplatesFileRoot, $"{TemplateName}.zip");
        var zipUrl = Path.Combine("https://api.github.com/repos/codechem/dappi/zipball/main");
        
        if (File.Exists(projectTemplateFilename))
            return projectTemplateFilename;
        
        if (!Directory.Exists(TemplatesFileRoot))
            Directory.CreateDirectory(TemplatesFileRoot);
        
        DownLoadHelper.DownLoadZipFile(zipUrl, projectTemplateFilename);
     
        return projectTemplateFilename;
    }
    
    public class DownLoadHelper
    {
        public static void DownLoadZipFile(string zip_url, string filePath)
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Accept-Language", " en-US");
                webClient.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
                webClient.Headers.Add("User-Agent", "MiConsolesApplicationes");

                Console.WriteLine($"Start download zip file:{zip_url}");
                Console.WriteLine($"Downloading...");
                
                try
                {
                    webClient.DownloadFile(zip_url, filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

                Console.WriteLine($"Download success and save as {filePath}");
            }
        }
    }
}