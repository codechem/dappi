using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Dappi.Cli.Helpers;

/// <summary>
/// Used to rename a solution, code from https://gist.github.com/hikalkan/014eecdae7c8d2677b192fcebcf5ba29?tdsourcetag=s_pcqq_aiomsg
/// </summary>
public class SolutionRenamer
{
    private readonly string _folder;

    private readonly string? _companyNamePlaceHolder;
    private readonly string? _projectNamePlaceHolder;
    private readonly string? _moduleNamePlaceholder;

    private readonly string? _companyName;
    private readonly string? _projectName;
    private readonly string? _moduleName;
    /// <summary>
    /// Creates a new <see cref="SolutionRenamer"/>.
    /// </summary>
    /// <param name="folder">Solution folder (which includes .sln file)</param>
    /// <param name="companyNamePlaceHolder">Company name placeholder (can be null if there is not a company name placeholder)</param>
    /// <param name="projectNamePlaceHolder">Project name placeholder</param>
    /// <param name="companyName">Company name. Can be null if new solution will not have a company name prefix. Should be null if <see cref="companyNamePlaceHolder"/> is null</param>
    /// <param name="projectName">Project name</param>
    private SolutionRenamer(string folder, string? companyNamePlaceHolder, string? projectNamePlaceHolder, string? companyName, string projectName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            companyName = null;
        }

        if (!Directory.Exists(folder))
        {
            throw new Exception("There is no folder: " + folder);
        }

        folder = folder.Trim('\\');

        if (companyNamePlaceHolder == null && companyName != null)
        {
            throw new Exception("Can not set companyName if companyNamePlaceHolder is null.");
        }

        _folder = folder;

        _companyNamePlaceHolder = companyNamePlaceHolder;
        _projectNamePlaceHolder = projectNamePlaceHolder ?? throw new ArgumentNullException(nameof(projectNamePlaceHolder));

        _companyName = companyName;
        _projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
    }

    public SolutionRenamer(string folder,
        string? companyNamePlaceHolder, string? projectNamePlaceHolder, string? moduleNamePlaceholder,
        string? companyName, string? projectName, string? moduleName)
        : this(folder, companyNamePlaceHolder, projectNamePlaceHolder, companyName, projectName!)
    {
        if (moduleNamePlaceholder.IsNullOrWhiteSpace() && !moduleName.IsNullOrWhiteSpace())
        {
            throw new Exception($"Can not set {nameof(moduleName)} if {nameof(moduleNamePlaceholder)} is null.");
        }

        _moduleNamePlaceholder = moduleNamePlaceholder;
        _moduleName = moduleName;
    }

    public void Run()
    {
        if (_companyNamePlaceHolder != null)
        {
            if (_companyName != null)
            {
                RenameDirectoryRecursively(_folder, _companyNamePlaceHolder, _companyName);
                RenameAllFiles(_folder, _companyNamePlaceHolder, _companyName);
                ReplaceContent(_folder, _companyNamePlaceHolder, _companyName);
            }
            else
            {
                RenameDirectoryRecursively(_folder, _companyNamePlaceHolder + "." + _projectNamePlaceHolder, _projectNamePlaceHolder);
                RenameAllFiles(_folder, _companyNamePlaceHolder + "." + _projectNamePlaceHolder, _projectNamePlaceHolder);
                ReplaceContent(_folder, _companyNamePlaceHolder + "." + _projectNamePlaceHolder, _projectNamePlaceHolder);
            }
        }

        RenameDirectoryRecursively(_folder, _projectNamePlaceHolder, _projectName);
        RenameAllFiles(_folder, _projectNamePlaceHolder, _projectName);
        ReplaceContent(_folder, _projectNamePlaceHolder, _projectName);

        if (!_moduleNamePlaceholder.IsNullOrWhiteSpace() && !_moduleName.IsNullOrWhiteSpace())
        {
            RenameDirectoryRecursively(_folder, _moduleNamePlaceholder, _moduleName);
            RenameAllFiles(_folder, _moduleNamePlaceholder, _moduleName);
            ReplaceContent(_folder, _moduleNamePlaceholder, _moduleName);
        }

    }

    private static void RenameDirectoryRecursively(string directoryPath, string? placeHolder, string? name)
    {
        var subDirectories = Directory.GetDirectories(directoryPath, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var subDirectory in subDirectories)
        {
            var newDir = subDirectory;
            if (subDirectory.Contains(placeHolder!))
            {
                newDir = subDirectory.Replace(placeHolder!, name);
                Directory.Move(subDirectory, newDir);
            }

            RenameDirectoryRecursively(newDir, placeHolder, name);
        }
    }

    private static void RenameAllFiles(string directory, string? placeHolder, string? name)
    {
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (file.Contains(placeHolder!))
            {
                File.Move(file, file.Replace(placeHolder!, name));
            }
        }
    }

    private static void ReplaceContent(string rootPath, string? placeHolder, string? name)
    {
        var skipExtensions = new[]
        {
            ".exe", ".dll", ".bin", ".suo", ".png", "jpg", "jpeg", ".pdb", ".obj"
        };

        var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (skipExtensions.Contains(Path.GetExtension(file)))
            {
                continue;
            }

            var fileSize = GetFileSize(file);
            if (fileSize < placeHolder?.Length)
            {
                continue;
            }

            var encoding = GetEncoding(file);

            var content = File.ReadAllText(file, encoding);
            var newContent = content.Replace(placeHolder!, name);
            if (newContent != content)
            {
                File.WriteAllText(file, newContent, encoding);
            }
        }
    }

    private static long GetFileSize(string file)
    {
        return new FileInfo(file).Length;
    }

    private static Encoding GetEncoding(string filename)
    {
        // Read the BOM
        var bom = new byte[4];
        using (var file = new FileStream(filename, FileMode.Open)) file.ReadExactly(bom, 0, 4);

        // Analyze the BOM
#pragma warning disable SYSLIB0001
        if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
#pragma warning restore SYSLIB0001
        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
        if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
        if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
        if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
        return Encoding.ASCII;
    }
}