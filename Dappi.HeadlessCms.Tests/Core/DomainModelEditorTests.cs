using System.Reflection;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Tests.TestData;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class DomainModelEditorTests : IDisposable
    {
        private const string DomainModelName = "Product";
        private const string TestPropertyName = "ProductName";
        private readonly string? _assemblyName;
        private readonly string _tempDir;
        private readonly string _enumTempDir;
        private readonly DomainModelEditor _domainModelEditor;
        private readonly string _filePath;
        public DomainModelEditorTests()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            _assemblyName = assembly.GetName().Name;
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _enumTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _domainModelEditor = new DomainModelEditor(_tempDir , _enumTempDir);
            _filePath = Path.Combine(_tempDir, $"{DomainModelName}.cs");
            
            Directory.CreateDirectory(_tempDir);
        }
       
        [Fact]
        public async Task DomainModelEditor_Should_Add_Using_Statements()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);
            await _domainModelEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Contains("using System.ComponentModel.DataAnnotation", actual);
            Assert.Contains("using System.ComponentModel.DataAnnotations.Schema", actual);
            Assert.Contains("using Dappi.Core.Attributes", actual);
            Assert.Contains("using Dappi.HeadlessCms.Model", actual);
        }

        [Fact]
        public async Task DomainModelEditor_Should_Generate_Class_With_CCController_Attribute()
        {
            var expected = $$"""
                             using System.ComponentModel.DataAnnotations;
                             using System.ComponentModel.DataAnnotations.Schema;
                             using Dappi.HeadlessCms.Models;
                             using Dappi.Core.Attributes;
                             using Dappi.HeadlessCms.Core.Attributes;
                             using Dappi.Core.Enums;

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

            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);
            await _domainModelEditor.SaveAsync();
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_Should_Generate_Class_With_IAuditable_Props()
        {
             var expected = $$"""
                              using System.ComponentModel.DataAnnotations;
                              using System.ComponentModel.DataAnnotations.Schema;
                              using Dappi.HeadlessCms.Models;
                              using Dappi.Core.Attributes;
                              using Dappi.HeadlessCms.Core.Attributes;
                              using Dappi.Core.Enums;

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
             var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = true };
            _domainModelEditor.CreateEntityModel(modelRequest);
            await _domainModelEditor.SaveAsync();
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Theory]
        [ClassData(typeof(ValidPropertyTypes))]
        public async Task DomainModelEditor_Should_Add_Required_Property(string type)
        { 
            var expected = $$"""
                             using System.ComponentModel.DataAnnotations;
                             using System.ComponentModel.DataAnnotations.Schema;
                             using Dappi.HeadlessCms.Models;
                             using Dappi.Core.Attributes;
                             using Dappi.HeadlessCms.Core.Attributes;
                             using Dappi.Core.Enums;

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
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);
            Property property = new Property
            {
                DomainModel = DomainModelName,
                Name = TestPropertyName,
                Type = type,
                IsRequired = true,
            };
            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();
            
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }
        
        [Theory]
        [ClassData(typeof(ValidPropertyTypes))]
        public async Task DomainModelEditor_Should_Add_Optional_Property(string type)
        {

             var expected = $$"""
                              using System.ComponentModel.DataAnnotations;
                              using System.ComponentModel.DataAnnotations.Schema;
                              using Dappi.HeadlessCms.Models;
                              using Dappi.Core.Attributes;
                              using Dappi.HeadlessCms.Core.Attributes;
                              using Dappi.Core.Enums;

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
             Property property = new()
             {
                 DomainModel = DomainModelName, Name = TestPropertyName, Type = type, IsRequired = false,
             };
             var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);
            _domainModelEditor.AddProperty(property);
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