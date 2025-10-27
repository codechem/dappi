using System.Reflection;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class DomainModelEditorTests : IDisposable
    {
        private const string DomainModelName = "Product";
        private const string TestPropertyName = "ProductName";
        private readonly string? _assemblyName;
        private readonly string _tempDir;
        private readonly DomainModelEditor _domainModelEditor;
        private readonly string _filePath;
        public DomainModelEditorTests()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            _assemblyName = assembly.GetName().Name;
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _domainModelEditor = new DomainModelEditor(_tempDir);
            _filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            
            Directory.CreateDirectory(_tempDir);
        }
       
        [Fact]
        public async Task DomainModelEditor_Should_Add_Using_Statements()
        {
            _domainModelEditor.CreateEntityModel(DomainModelName, true);
            await _domainModelEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Contains("using System.ComponentModel.DataAnnotation", actual);
            Assert.Contains("using System.ComponentModel.DataAnnotations.Schema", actual);
            Assert.Contains("using Dappi.SourceGenerator.Attributes", actual);
            Assert.Contains("using Dappi.HeadlessCms.Model", actual);
        }

        [Fact]
        public async Task DomainModelEditor_Should_Generate_Class_With_CCController_Attribute()
        {
           
            var expected = $$"""
                             using System.ComponentModel.DataAnnotations;
                             using System.ComponentModel.DataAnnotations.Schema;
                             using Dappi.SourceGenerator.Attributes;
                             using Dappi.HeadlessCms.Models;

                             namespace {{_assemblyName}}.Entities
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

            _domainModelEditor.CreateEntityModel(DomainModelName);
            await _domainModelEditor.SaveAsync();
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_Should_Generate_Class_With_IAuditable_Props()
        {
             string expected = $$"""
                                      using System.ComponentModel.DataAnnotations;
                                      using System.ComponentModel.DataAnnotations.Schema;
                                      using Dappi.SourceGenerator.Attributes;
                                      using Dappi.HeadlessCms.Models;

                                      namespace {{_assemblyName}}.Entities
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
            
            _domainModelEditor.CreateEntityModel(DomainModelName, true);
            await _domainModelEditor.SaveAsync();
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Theory]
        [ClassData(typeof(PropertyTestData))]
        public async Task DomainModelEditor_Should_Add_Required_Property(string type)
        { 
            var expected = $$"""
                             using System.ComponentModel.DataAnnotations;
                             using System.ComponentModel.DataAnnotations.Schema;
                             using Dappi.SourceGenerator.Attributes;
                             using Dappi.HeadlessCms.Models;

                             namespace {{_assemblyName}}.Entities
                             {
                                 [CCController]
                                 public class {{DomainModelName}}
                                 {
                                     [Key]
                                     [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                     public Guid Id { get; set; }
                                     public {{type}} {{TestPropertyName}} { get; set; }
                                 }
                             }
                             """;
            
            _domainModelEditor.CreateEntityModel(DomainModelName);
            _domainModelEditor.AddProperty(TestPropertyName, type , DomainModelName , true);
            await _domainModelEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }
        
        [Theory]
        [ClassData(typeof(PropertyTestData))]
        public async Task DomainModelEditor_Should_Add_Optional_Property(string type)
        {
             var expected = $$"""
                              using System.ComponentModel.DataAnnotations;
                              using System.ComponentModel.DataAnnotations.Schema;
                              using Dappi.SourceGenerator.Attributes;
                              using Dappi.HeadlessCms.Models;

                              namespace {{_assemblyName}}.Entities
                              {
                                  [CCController]
                                  public class {{DomainModelName}}
                                  {
                                      [Key]
                                      [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                      public Guid Id { get; set; }
                                      public {{type}}? {{TestPropertyName}} { get; set; }
                                  }
                              }
                              """;
            
            _domainModelEditor.CreateEntityModel(DomainModelName);
            _domainModelEditor.AddProperty(TestPropertyName, type , DomainModelName);
            await _domainModelEditor.SaveAsync();
            var actual = await File.ReadAllTextAsync(_filePath);
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