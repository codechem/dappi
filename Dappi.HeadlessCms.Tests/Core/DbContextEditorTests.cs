using Dappi.Core.Utils;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Core.Schema;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class DbContextEditorTests : IDisposable
    {
        private const string DomainModelNamespace = "MyApp.Models";
        private const string DomainModelName = "Product";
        private const string DbContextName = "TestDbContext";
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public DbContextEditorTests()
        {
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public async Task AddDbSetAsync_AddsDbSetProperty()
        {
            // Arrange
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
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
            
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);

            var editor = new DbContextEditor(_tempDir, DbContextName);
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
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");

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
                
            await File.WriteAllTextAsync(dbContextPath, expected);

            var editor = new DbContextEditor(_tempDir, DbContextName);
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
                
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            
            var editor = new DbContextEditor(_tempDir, DbContextName);
            var modelInfo = new DomainModelEntityInfo { Name = DomainModelName, Namespace = DomainModelNamespace };

            // Act
            editor.RemoveSetFromDbContext(modelInfo);
            await editor.SaveAsync();

            // Assert
            var updatedCode = await File.ReadAllTextAsync(dbContextPath);
            Assert.Equal(expected.ReplaceLineEndings(), updatedCode.ReplaceLineEndings());
            
        }

        [Fact]
        public async Task DbContextEditor_Should_Create_On_ModelCreating()
        {
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
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
            
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            var expected = "protected override void OnModelCreating(ModelBuilder modelBuilder)";
            
            var editor = new DbContextEditor(_tempDir, DbContextName);
            await editor.UpdateOnModelCreating("Product", "Category", "OneToOne", "Products", "Categories");
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(dbContextPath);
            Assert.Contains(expected, actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }

        [Fact]
        public async Task DbContextEditor_Should_Create_OneToOne_Relation()
        {
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
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
            
            const string modelName = "Product";
            const string relatedTo = "Category";
            const string relatedPropertyName = "Category";
            const string propertyName = "ProductCategory";
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            var editor = new DbContextEditor(_tempDir, DbContextName);
            await editor.UpdateOnModelCreating(modelName, relatedTo, "OneToOne", propertyName, relatedPropertyName);
            
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(dbContextPath);
            
            Assert.Contains($"modelBuilder.Entity<{modelName}>()", actual);
            Assert.Contains($".HasOne<{relatedTo}>(s => s.{propertyName})", actual);
            Assert.Contains($".WithOne(e => e.{relatedPropertyName ?? modelName})", actual);
            Assert.Contains($".HasForeignKey<{relatedTo}>(ad => ad.{relatedPropertyName ?? modelName}Id)", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }

        [Fact]
        public async Task DbContextEditor_Should_Create_OneToMany_Relation()
        {
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
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
            
            const string modelName = "Product";
            const string relatedTo = "Category";
            const string relatedPropertyName = "Category";
            const string propertyName = "ProductCategory";
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            var editor = new DbContextEditor(_tempDir, DbContextName);
            await editor.UpdateOnModelCreating(modelName, relatedTo, "OneToMany", propertyName, relatedPropertyName);
            
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(dbContextPath);
            
            Assert.Contains($"modelBuilder.Entity<{modelName}>()", actual);
            Assert.Contains($".HasMany<{relatedTo}>(s => s.{propertyName})", actual);
            Assert.Contains($".WithOne(e => e.{relatedPropertyName ?? modelName})", actual);
            Assert.Contains($".HasForeignKey(s => s.{relatedPropertyName ?? modelName}Id);", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }
        
        [Fact]
        public async Task DbContextEditor_Should_Create_ManyToOne_Relation()
        {
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
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
            
            const string modelName = "Product";
            const string relatedTo = "Category";
            const string relatedPropertyName = "Category";
            const string propertyName = "ProductCategory";
            
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            
            
            var editor = new DbContextEditor(_tempDir, DbContextName);
            await editor.UpdateOnModelCreating(modelName, relatedTo, "ManyToOne", propertyName, relatedPropertyName);
            
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(dbContextPath);
            
            Assert.Contains($"modelBuilder.Entity<{modelName}>()", actual);
            Assert.Contains($".HasOne<{relatedTo}>(s => s.{propertyName})", actual);
            Assert.Contains($".WithMany(e => e.{relatedPropertyName ?? $"{modelName.Pluralize()}"})", actual);
            Assert.Contains($".HasForeignKey(s => s.{propertyName}Id);", actual);
            Assert.Contains("base.OnModelCreating(modelBuilder);", actual);
        }
        
        [Fact]
        public async Task DbContextEditor_Should_Create_ManyToMany_Relation()
        {
            var dbContextPath = Path.Combine(_tempDir, $"{DbContextName}.cs");
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
            
            const string modelName = "Product";
            const string relatedTo = "Category";
            const string relatedPropertyName = "Category";
            const string propertyName = "ProductCategory";
            
            await File.WriteAllTextAsync(dbContextPath, dbContextCode);
            
            
            var editor = new DbContextEditor(_tempDir, DbContextName);
            await editor.UpdateOnModelCreating(modelName, relatedTo, "ManyToMany", propertyName, relatedPropertyName);
            
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(dbContextPath);
            
            Assert.Contains($"modelBuilder.Entity<{modelName}>()", actual);
            Assert.Contains($".HasMany(m => m.{propertyName})", actual);
            Assert.Contains($".WithMany(r => r.{relatedPropertyName})", actual);
            Assert.Contains($".UsingEntity(j => j.ToTable(\"{modelName}{relatedTo.Pluralize()}\"));", actual);
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