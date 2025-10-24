using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class DomainModelEditorTests : IDisposable
    {
        private const string DomainModelName = "Product";
        private const string TestPropertyName = "ProductName";
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public DomainModelEditorTests()
        {
            Directory.CreateDirectory(_tempDir);
        }
       
        [Fact]
        public async Task DomainModelEditor_Should_Add_Using_Statements()
        {
            var filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            DomainModelEditor editor = new(_tempDir);
            editor.CreateEntityModel(DomainModelName, true);
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(filePath);

            Assert.Contains("using System.ComponentModel.DataAnnotation", actual);
            Assert.Contains("using System.ComponentModel.DataAnnotations.Schema", actual);
            Assert.Contains("using Dappi.SourceGenerator.Attributes", actual);
            Assert.Contains("using Dappi.HeadlessCms.Model", actual);
        }

        [Fact]
        public async Task DomainModelEditor_Should_Generate_Class_With_CCController_Attribute()
        {
            const string expected = $$"""
                                      using System.ComponentModel.DataAnnotations;
                                      using System.ComponentModel.DataAnnotations.Schema;
                                      using Dappi.SourceGenerator.Attributes;
                                      using Dappi.HeadlessCms.Models;

                                      namespace ReSharperTestRunner.Entities
                                      {
                                          [CCController]
                                          public class {{DomainModelName}}
                                          {
                                              [Key]
                                              [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                              public Guid Id { get; set; }
                                          }
                                      }
                                      """;
            var filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            DomainModelEditor editor = new(_tempDir);

            editor.CreateEntityModel(DomainModelName);
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_Should_Generate_Class_With_IAuditable_Props()
        {
            const string expected = $$"""
                                      using System.ComponentModel.DataAnnotations;
                                      using System.ComponentModel.DataAnnotations.Schema;
                                      using Dappi.SourceGenerator.Attributes;
                                      using Dappi.HeadlessCms.Models;

                                      namespace ReSharperTestRunner.Entities
                                      {
                                          [CCController]
                                          public class {{DomainModelName}} : {{nameof(IAuditableEntity)}}
                                          {
                                              [Key]
                                              [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                              public Guid Id { get; set; }
                                              public DateTime? CreatedAtUtc { get; set; }
                                              public DateTime? UpdatedAtUtc { get; set; }
                                              public string? CreatedBy { get; set; }
                                              public string? UpdatedBy { get; set; }
                                          }
                                      }
                                      """;
            
            var filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            DomainModelEditor editor = new(_tempDir);
            editor.CreateEntityModel(DomainModelName, true);
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_Should_Add_Required_Property()
        {
            const string expected = $$"""
                                      using System.ComponentModel.DataAnnotations;
                                      using System.ComponentModel.DataAnnotations.Schema;
                                      using Dappi.SourceGenerator.Attributes;
                                      using Dappi.HeadlessCms.Models;

                                      namespace ReSharperTestRunner.Entities
                                      {
                                          [CCController]
                                          public class {{DomainModelName}}
                                          {
                                              [Key]
                                              [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                              public Guid Id { get; set; }
                                              public string {{TestPropertyName}} { get; set; }
                                          }
                                      }
                                      """;
            
            var filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            DomainModelEditor editor = new(_tempDir);
            editor.CreateEntityModel(DomainModelName);
            editor.AddProperty(TestPropertyName, "string" , DomainModelName , true);
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }
        
        [Fact]
        public async Task DomainModelEditor_Should_Add_Optional_Property()
        {
            const string expected = $$"""
                                      using System.ComponentModel.DataAnnotations;
                                      using System.ComponentModel.DataAnnotations.Schema;
                                      using Dappi.SourceGenerator.Attributes;
                                      using Dappi.HeadlessCms.Models;

                                      namespace ReSharperTestRunner.Entities
                                      {
                                          [CCController]
                                          public class {{DomainModelName}}
                                          {
                                              [Key]
                                              [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                              public Guid Id { get; set; }
                                              public string? {{TestPropertyName}} { get; set; }
                                          }
                                      }
                                      """;
            
            var filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            DomainModelEditor editor = new(_tempDir);
            editor.CreateEntityModel(DomainModelName);
            editor.AddProperty(TestPropertyName, "string" , DomainModelName);
            await editor.SaveAsync();
            var actual = await File.ReadAllTextAsync(filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }
        
#pragma warning disable CA1816
        public void Dispose()
#pragma warning restore CA1816
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}