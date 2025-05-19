using System.Collections.Generic;
using System.IO;

namespace Dappi.Cli.Helpers;

public static class RenameHelper
{
    public static void RenameFolders(
        string folderToProcess, string placeholder, string projectName, List<string>? excludedSubFolders)
    {
        excludedSubFolders ??= [];
        
        //Delete ExcludeFolders
        foreach (var excludeFolder in excludedSubFolders)
        {
            var directoryToDel = Path.Combine(folderToProcess, excludeFolder);
            if (Directory.Exists(directoryToDel))
            {
                Directory.Delete(directoryToDel, true);
            }
        }

        //Rename Folder
        var (companyNamePlaceholder, projectNamePlaceholder, moduleNamePlaceholder) = placeholder.NameParse();
        var (newCompanyName, newProjectName, newModuleName) = projectName.NameParse();

        var slnRenamer = new SolutionRenamer(folderToProcess,
            companyNamePlaceholder, projectNamePlaceholder, moduleNamePlaceholder,
            newCompanyName, newProjectName, newModuleName);

        slnRenamer.Run();
    }
}