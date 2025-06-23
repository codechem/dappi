using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Core.Schema;

namespace Dappi.Tests.HeadlessCmsTests.Core
{
    public class DbContextEditorTests : IDisposable
    {
        private const string DomainModelNamespace = "MyApp.Models";
        private const string DomainModelName = "Product";
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public DbContextEditorTests()
        {
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public async Task AddDbSetAsync_AddsDbSetProperty()
        {
            // Arrange
            const string dbContextName = "TestDbContext";
            var dbContextPath = Path.Combine(_tempDir, $"{dbContextName}.cs");
     
            await File.WriteAllTextAsync(dbContextPath, """
                                                        "
                                                                using Microsoft.EntityFrameworkCore;
                                                        
                                                                namespace MyApp.Data
                                                                {{
                                                                    public class TestDbContext : DbContext
                                                                    {{
                                                                    }}
                                                                }}
                                                                "
                                                        """);

            var editor = new DbContextEditor(_tempDir, "TestDbContext");
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            editor.AddDbSetToDbContext(modelInfo);
            await editor.SaveAsync();

            var updatedCode = await File.ReadAllTextAsync(dbContextPath);

            // Assert
            Assert.Contains($"DbSet<{DomainModelName}>", updatedCode);
            Assert.Contains($"public DbSet<{DomainModelName}> {DomainModelName}s", updatedCode);
            Assert.Contains($"using {DomainModelNamespace};", updatedCode);
        }

        [Fact]
        public async Task AddDbSetAsync_DoesntAddDbSetPropertyIfItIsRegisteredAsADomainModelEntity()
        {
            // Arrange
            const string dbContextName = "TestDbContext";
            var dbContextPath = Path.Combine(_tempDir, $"{dbContextName}.cs");

            const string expected = $$"""

                                      using Microsoft.EntityFrameworkCore;
                                      using MyApp.Models;

                                      namespace MyApp.Data
                                      {
                                          public class TestDbContext : DbContext
                                          {
                                              public DbSet<{{DomainModelName}}> {{DomainModelName}}s { get; set; }
                                          }
                                      }
                                      """;
                
            await File.WriteAllTextAsync(dbContextPath, expected);

            var editor = new DbContextEditor(_tempDir, "TestDbContext");
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            editor.AddDbSetToDbContext(modelInfo);
            await editor.SaveAsync();

            // Assert
            var updatedCode = await File.ReadAllTextAsync(dbContextPath);

            Assert.Equal(expected, updatedCode);
        }

        [Fact]
        public async Task RemoveDbSetAsync_RemovesDbSetProperty()
        {
            // Arrange
            const string dbContextCode = $$"""
                                      using Microsoft.EntityFrameworkCore;
                                      using MyApp.Models;

                                      namespace MyApp.Data
                                      {
                                          public class AppDbContext : DbContext
                                          {
                                              public DbSet<{{DomainModelName}}> {{DomainModelName}}s { get; set; }
                                          }
                                      }
                                      """;
            
            const string expected = """
                                           using Microsoft.EntityFrameworkCore;
                                           using MyApp.Models;

                                           namespace MyApp.Data
                                           {
                                               public class AppDbContext : DbContext
                                               {
                                               }
                                           }
                                           """;
                
            const string dbContextName = "TestDbContext";
            var dbContextPath = Path.Combine(_tempDir, $"{dbContextName}.cs");
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            
            var editor = new DbContextEditor(dbContextPath, dbContextName);
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            editor.RemoveSetFromDbContext(modelInfo);
            await editor.SaveAsync();

            // Assert
            var updatedCode = await File.ReadAllTextAsync(dbContextPath);
            Assert.Equal(expected, updatedCode);
            
        }
#pragma warning disable CA1816
        public void Dispose()
#pragma warning restore CA1816
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}