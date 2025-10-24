using Dappi.Core.Utils;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Core.Schema;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class DbContextEditorTests : IDisposable
    {
        private const string DomainModelNamespace = "MyApp.Models";
        private const string DomainModelName = "Product";
        private const string ModelName = "Product";
        private const string RelatedTo = "Category";
        private const string RelatedPropertyName = "Category";
        private const string PropertyName = "ProductCategory";
        private const string DbContextName = "TestDbContext";
        private readonly string _tempDir;
        private readonly string _dbContextPath;
        private readonly DbContextEditor _dbContextEditor;
        public DbContextEditorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
            _dbContextEditor = new DbContextEditor(_tempDir, DbContextName);
        }

        [Fact]
        public async Task AddDbSetAsync_AddsDbSetProperty()
        {
            // Arrange
            const string dbContextCode = $$"""
                                      using Microsoft.EntityFrameworkCore;
                                      using MyApp.Models;

                                      namespace MyApp.Data
                                      {
                                          public class {{DbContextName}} : DbContext
                                          {
                                          }
                                      }
                                      """;
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            _dbContextEditor.AddDbSetToDbContext(modelInfo);
            await _dbContextEditor.SaveAsync();

            var updatedCode = await File.ReadAllTextAsync(_dbContextPath);

            // Assert
            Assert.Contains($"DbSet<{DomainModelName}>", updatedCode);
            Assert.Contains($"public DbSet<{DomainModelName}> {DomainModelName}s", updatedCode);
            Assert.Contains($"using {DomainModelNamespace};", updatedCode);
        }

        [Fact]
        public async Task AddDbSetAsync_DoesntAddDbSetPropertyIfItIsRegisteredAsADomainModelEntity()
        {
            // Arrange
            const string expected = $$"""

                                      using Microsoft.EntityFrameworkCore;
                                      using MyApp.Models;

                                      namespace MyApp.Data
                                      {
                                          public class {{DbContextName}} : DbContext
                                          {
                                              public DbSet<{{DomainModelName}}> {{DomainModelName}}s { get; set; }
                                          }
                                      }
                                      """;
            await File.WriteAllTextAsync(_dbContextPath, expected);
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            _dbContextEditor.AddDbSetToDbContext(modelInfo);
            await _dbContextEditor.SaveAsync();

            // Assert
            var updatedCode = await File.ReadAllTextAsync(_dbContextPath);

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
                                          public class {{DbContextName}} : DbContext
                                          {
                                              public DbSet<{{DomainModelName}}> {{DomainModelName}}s { get; set; }
                                          }
                                      }
                                      """;
            
            const string expected = $$"""
                                           using Microsoft.EntityFrameworkCore;
                                           using MyApp.Models;

                                           namespace MyApp.Data
                                           {
                                               public class {{DbContextName}} : DbContext
                                               {
                                               }
                                           }
                                           """;
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            _dbContextEditor.RemoveSetFromDbContext(modelInfo);
            await _dbContextEditor.SaveAsync();

            // Assert
            var updatedCode = await File.ReadAllTextAsync(_dbContextPath);
            Assert.Equal(expected.ReplaceLineEndings(), updatedCode.ReplaceLineEndings());
            
        }

        [Fact]
        public async Task DbContextEditor_Should_Create_On_ModelCreating()
        {
            const string dbContextCode = $$"""
                                           using Microsoft.EntityFrameworkCore;
                                           using MyApp.Models;

                                           namespace MyApp.Data
                                           {
                                               public class {{DbContextName}} : DbContext
                                               {
                                               }
                                           }
                                           """;
            
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            const string expected = "protected override void OnModelCreating(ModelBuilder modelBuilder)";
            
            await _dbContextEditor.UpdateOnModelCreating("Product", "Category", "OneToOne", "Products", "Categories");
            await _dbContextEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_dbContextPath);
            Assert.Contains(expected, actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }

        [Fact]
        public async Task DbContextEditor_Should_Create_OneToOne_Relation()
        { 
            const string dbContextCode = $$"""
                                         using Microsoft.EntityFrameworkCore;
                                         using MyApp.Models;

                                         namespace MyApp.Data
                                         {
                                             public class {{DbContextName}} : DbContext
                                             {
                                             }
                                         }
                                         """;
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            
            await _dbContextEditor.UpdateOnModelCreating(ModelName, RelatedTo, "OneToOne", PropertyName, RelatedPropertyName);
            await _dbContextEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_dbContextPath);
            Assert.Contains($"modelBuilder.Entity<{ModelName}>()", actual);
            Assert.Contains($".HasOne<{RelatedTo}>(s => s.{PropertyName})", actual);
            Assert.Contains($".WithOne(e => e.{RelatedPropertyName ?? ModelName})", actual);
            Assert.Contains($".HasForeignKey<{RelatedTo}>(ad => ad.{RelatedPropertyName ?? ModelName}Id)", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }

        [Fact]
        public async Task DbContextEditor_Should_Create_OneToMany_Relation()
        {
            const string dbContextCode = $$"""
                                           using Microsoft.EntityFrameworkCore;
                                           using MyApp.Models;

                                           namespace MyApp.Data
                                           {
                                               public class {{DbContextName}} : DbContext
                                               {
                                               }
                                           }
                                           """;
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            
            await _dbContextEditor.UpdateOnModelCreating(ModelName, RelatedTo, "OneToMany", PropertyName, RelatedPropertyName);
            await _dbContextEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_dbContextPath);
            Assert.Contains($"modelBuilder.Entity<{ModelName}>()", actual);
            Assert.Contains($".HasMany<{RelatedTo}>(s => s.{PropertyName})", actual);
            Assert.Contains($".WithOne(e => e.{RelatedPropertyName ?? ModelName})", actual);
            Assert.Contains($".HasForeignKey(s => s.{RelatedPropertyName ?? ModelName}Id);", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }
        
        [Fact]
        public async Task DbContextEditor_Should_Create_ManyToOne_Relation()
        {
            const string dbContextCode = $$"""
                                           using Microsoft.EntityFrameworkCore;
                                           using MyApp.Models;

                                           namespace MyApp.Data
                                           {
                                               public class {{DbContextName}} : DbContext
                                               {
                                               }
                                           }
                                           """;
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            
            await _dbContextEditor.UpdateOnModelCreating(ModelName, RelatedTo, "ManyToOne", PropertyName, RelatedPropertyName);
            await _dbContextEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_dbContextPath);
            Assert.Contains($"modelBuilder.Entity<{ModelName}>()", actual);
            Assert.Contains($".HasOne<{RelatedTo}>(s => s.{PropertyName})", actual);
            Assert.Contains($".WithMany(e => e.{RelatedPropertyName ?? $"{ModelName.Pluralize()}"})", actual);
            Assert.Contains($".HasForeignKey(s => s.{PropertyName}Id);", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }
        
        [Fact]
        public async Task DbContextEditor_Should_Create_ManyToMany_Relation()
        {
            const string dbContextCode = $$"""
                                           using Microsoft.EntityFrameworkCore;
                                           using MyApp.Models;

                                           namespace MyApp.Data
                                           {
                                               public class {{DbContextName}} : DbContext
                                               {
                                               }
                                           }
                                           """;
            await File.WriteAllTextAsync(_dbContextPath, dbContextCode);
            
            await _dbContextEditor.UpdateOnModelCreating(ModelName, RelatedTo, "ManyToMany", PropertyName, RelatedPropertyName);
            await _dbContextEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_dbContextPath);
            Assert.Contains($"modelBuilder.Entity<{ModelName}>()", actual);
            Assert.Contains($".HasMany(m => m.{PropertyName})", actual);
            Assert.Contains($".WithMany(r => r.{RelatedPropertyName})", actual);
            Assert.Contains($".UsingEntity(j => j.ToTable(\"{ModelName}{RelatedTo.Pluralize()}\"));", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }
        
#pragma warning disable CA1816
        public void Dispose()
#pragma warning restore CA1816
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}