using System;
using System.IO;

namespace Dappi.Cli;

public static class Constants
{
    public const string ProjectNamePlaceholder = "MyCompany.MyProject";

    public static readonly string TemplatesFileRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            $".{Path.DirectorySeparatorChar}Dappi");
    
    public static readonly string TemplateName = Path.Combine(TemplatesFileRoot, ProjectNamePlaceholder);
    
    public const string CliCommandName = "dappi";

    public const string DappiRepoOwner = "codechem";
    
    public const string DappiRepoName = "dappi";
}