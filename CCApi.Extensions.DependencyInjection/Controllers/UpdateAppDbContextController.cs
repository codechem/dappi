using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;

namespace CCApi.Extensions.DependencyInjection.Controllers;

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
        const string dbSetKeyword = "public DbSet";
        var dbContextBuilder = new StringBuilder(appDbContextCode);
        var dbSets = appDbContextCode.Split(Environment.NewLine)
            .Where(x => x.Contains(dbSetKeyword))
            .Select(x => x.Trim())
            .ToList();

        var dbSetsCharactersCountBeforeInsert = dbSets.Sum(dbSetLine => dbSetLine.Length);
        dbSets.AddRange(missingModels.Select(model =>
          $"public DbSet<{model}> {model}s {{ get; set; }}")
        );

        var (replaceIndex, padding) = GetTextReplacementIndexAndPaddding(dbSetKeyword, appDbContextCode);
        dbContextBuilder.Remove(replaceIndex, dbSetsCharactersCountBeforeInsert);
        dbContextBuilder.Insert(replaceIndex, string.Join(string.Empty, dbSets.Select(x => padding + x)));

        var apiAssemblyName = Assembly.GetEntryAssembly()!.GetName().Name;
        if (!appDbContextCode.Contains($"{apiAssemblyName}.Entities"))
            // Still, we assume that the Domain objects live in a folder called Entities.
            dbContextBuilder.Insert(0, $"using {apiAssemblyName}.Entities;{Environment.NewLine}");

        return dbContextBuilder.ToString();
    }

    private static (int keywordIndex, string padding) GetTextReplacementIndexAndPaddding(string dbSetKeyword, string dbContextCode)
    {
        const char curlyBracketSymbol = '{';
        var notFirstDbSet = dbContextCode.Contains(dbSetKeyword, StringComparison.CurrentCulture);

        var isInFileScopedNamespace = dbContextCode.Split(Environment.NewLine)
            .First(x => x.StartsWith("namespace"))
            .EndsWith(';');

        // If we have a file-scoped namespace we need one tab for padding, otherwise two tabs.
        var padding = isInFileScopedNamespace 
            ? $"{Environment.NewLine}{new string('\t', 1)}" 
            : $"{Environment.NewLine}{new string('\t', 2)}";
        
        // Basically, if we have a block scoped namespace decl. we need to get the index of the second '{' character 
        // and add 1 to get to not overwrite the bracket.
        var replacementIndex = isInFileScopedNamespace 
            ? dbContextCode.IndexOf(curlyBracketSymbol) + 1
            : dbContextCode.IndexOf(curlyBracketSymbol, dbContextCode.IndexOf(curlyBracketSymbol) + 1) + 1;

        if (notFirstDbSet)
        {
            int keywordIndex = dbContextCode.IndexOf(dbSetKeyword);
            return (keywordIndex, padding);
        } 
        else
        {
            return (replacementIndex, padding);
        }
    }
}