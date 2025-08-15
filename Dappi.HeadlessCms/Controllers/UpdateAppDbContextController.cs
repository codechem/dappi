using System.Reflection;
using System.Text;
using Dappi.HeadlessCms.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Controllers;

[ApiExplorerSettings(GroupName = "Toolkit")]
[Route("api/update-db-context")]
[ApiController]
public class UpdateAppDbContextController : ControllerBase
{
    private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Entities");

    private readonly string _appDbContextFilePath =
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppDbContext.cs");

    [HttpGet]
    public IActionResult UpdateDbContext()
    {
        try
        {
            var newModels = GetNewModels();

            if (!newModels.Any())
            {
                return Ok("No new models found to add to AppDbContext.");
            }

            var appDbContextCode = System.IO.File.ReadAllText(_appDbContextFilePath);
            var updatedCode = AddDbSetsToAppDbContextClass(appDbContextCode, newModels);
            System.IO.File.WriteAllText(_appDbContextFilePath, updatedCode);

            return Ok(new
            {
                Message = "AppDbContext updated with new DbSets.",
                NewModels = newModels
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private string[] GetNewModels()
    {
        var modelNames = Directory.GetFiles(_entitiesFolderPath, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        var existingCode = System.IO.File.ReadAllText(_appDbContextFilePath);

        return modelNames.Where(name => !existingCode.Contains($"DbSet<{name}>")).ToArray();
    }

    private static string AddDbSetsToAppDbContextClass(string appDbContextCode, string[] missingModels)
    {
        var isInFileScopedNamespace = IsClassInFileScopedNamespace(appDbContextCode);
        var (classDeclarationStartPosition, classDeclarationEndPosition, codeInBetween) = GetCodeInClassScope(appDbContextCode, isInFileScopedNamespace);
        var padding = isInFileScopedNamespace ? new string('\t', 1) : new string('\t', 2);

        var dbContextScopedCodeBuilder = new StringBuilder(codeInBetween);

        foreach (var missingModelName in missingModels)
            dbContextScopedCodeBuilder.AppendLine(
                $"{Environment.NewLine}{padding}public DbSet<{missingModelName}> {missingModelName.Pluralize()} {{ get; set; }}");

        var dbContextBuilder = new StringBuilder(appDbContextCode);
        // Replace the last character to add appropriate padding.
        var endOfScopeReplacementCharacter = isInFileScopedNamespace ? "}" : new string('\t', 1) + "}";
        dbContextBuilder.Replace("}", endOfScopeReplacementCharacter, classDeclarationEndPosition, endOfScopeReplacementCharacter.Length);

        dbContextBuilder.Remove(classDeclarationStartPosition + 1, codeInBetween.Length);
        dbContextBuilder.Insert(classDeclarationStartPosition, dbContextScopedCodeBuilder.ToString());

        var apiAssemblyName = Assembly.GetEntryAssembly()!.GetName().Name;
        // Still, we assume that the Domain objects live in a folder called Entities.
        if (!appDbContextCode.Contains($"{apiAssemblyName}.Entities"))
            dbContextBuilder.Insert(0, $"using {apiAssemblyName}.Entities;{Environment.NewLine}");

        return dbContextBuilder.ToString();
    }

    private static (int ClassDeclarationStart, int ClassDeclarationEnd, string CodeInBetween) GetCodeInClassScope(string codeText, bool isInFileScopedNamespace)
    {
        const char startScopeSymbol = '{';
        const char endScopeSymbol = '}';

        var classDeclarationStartPosition =
            GetFirstDeclarationSymbolPositionForClass(codeText, startScopeSymbol, isInFileScopedNamespace);
        var classDeclarationEndPostion =
            GetLastDeclarationSymbolPositionForClass(codeText, endScopeSymbol, isInFileScopedNamespace);

        var startPosition = classDeclarationStartPosition + 1;
        var endPosition = classDeclarationEndPostion - 1;

        return (startPosition, endPosition, codeText[startPosition..endPosition]);
    }

    private static bool IsClassInFileScopedNamespace(string codeText)
    {
        return codeText.Split(Environment.NewLine)
            .Where(x => x.StartsWith("namespace"))
            .First()
            .EndsWith(';');
    }

    private static int GetFirstDeclarationSymbolPositionForClass(string classText, char symbol, bool isInFileScopedNamespace)
    {
        return isInFileScopedNamespace
            ? classText.IndexOf(symbol)
            : classText.IndexOf(symbol, classText.IndexOf(symbol) + 1);
    }

    private static int GetLastDeclarationSymbolPositionForClass(string classText, char symbol, bool isInFileScopedNamespace)
    {
        return isInFileScopedNamespace
            ? classText.LastIndexOf(symbol)
            : classText.LastIndexOf(symbol, classText.LastIndexOf(symbol) - 1);
    }
}