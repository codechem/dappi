using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CCApi.WebApiExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateAppDbContextController : ControllerBase
    {
        private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Entities");
        private readonly string _appDbContextFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppDbContext.cs");

        [HttpGet]
        public IActionResult UpdateDbContext()
        {
            try
            {
                // Scan for new model filenames
                var newModels = GetNewModels();

                if (!newModels.Any())
                {
                    return Ok("No new models found to add to AppDbContext.");
                }

                // Read the current AppDbContext code
                var appDbContextCode = System.IO.File.ReadAllText(_appDbContextFilePath);

                // Find the AppDbContext class and add missing DbSet<> properties inside the class
                var updatedCode = AddDbSetsToAppDbContextClass(appDbContextCode, newModels);

                // Write the updated content back
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
            // Get all .cs files inside the Entities folder (without extensions)
            var modelNames = Directory.GetFiles(_entitiesFolderPath, "*.cs")
                                      .Select(Path.GetFileNameWithoutExtension)
                                      .ToList();

            // Read the existing AppDbContext file
            var existingCode = System.IO.File.ReadAllText(_appDbContextFilePath);

            // Find models that are not yet registered as DbSets
            return modelNames.Where(name => !existingCode.Contains($"DbSet<{name}>")).ToArray();
        }

        private string AddDbSetsToAppDbContextClass(string appDbContextCode, string[] missingModels)
        {
            var sb = new StringBuilder(appDbContextCode);

            // Find the position inside the class (right before the closing curly brace of AppDbContext)
            var classEndIndex = appDbContextCode.LastIndexOf("}", StringComparison.Ordinal);

            // Insert DbSet<T> declarations inside the class
            sb.Insert(classEndIndex, Environment.NewLine);

            foreach (var model in missingModels)
            {
                sb.Insert(classEndIndex, $"    public DbSet<{model}> {model}s {{ get; set; }}{Environment.NewLine}");
            }

            return sb.ToString();
        }
    }
}
