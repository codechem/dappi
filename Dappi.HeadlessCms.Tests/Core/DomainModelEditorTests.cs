using System.Reflection;
using Dappi.Core.Attributes;
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
                                 [CcController]
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
                                  [{{CcControllerAttribute.ShortName}}]
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
                                 [{{CcControllerAttribute.ShortName}}]
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
                                  [{{CcControllerAttribute.ShortName}}]
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
                DomainModel = DomainModelName,
                Name = TestPropertyName,
                Type = type,
                IsRequired = false,
            };
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);
            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();
            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Add_Length_Attribute_With_Both_Min_And_Max()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Description",
                Type = "string",
                IsRequired = false,
                Min = "10",
                Max = "100"
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }

                                 [Length(10, 100)]
                                 public string? Description { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Add_MinLength_Attribute_When_Only_Min_Specified()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Username",
                Type = "string",
                IsRequired = true,
                Min = "5",
                Max = null
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }

                                 [MinLength(5)]
                                 public string Username { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Add_MaxLength_Attribute_When_Only_Max_Specified()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "ShortCode",
                Type = "string",
                IsRequired = false,
                Min = null,
                Max = "20"
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }

                                 [MaxLength(20)]
                                 public string? ShortCode { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Not_Add_Length_Attributes_When_Neither_Min_Nor_Max_Specified()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Title",
                Type = "string",
                IsRequired = false,
                Min = null,
                Max = null
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }
                                 public string? Title { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Theory]
        [InlineData("int", "1", "100")]
        public async Task DomainModelEditor_AddProperty_Should_Add_Range_Attribute_For_Int_Types(string type, string min, string max)
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = type,
                IsRequired = false,
                Min = min,
                Max = max
            };

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Contains($"[Range({min}, {max})]", actual);
            Assert.Contains($"public {type}? Name", actual);
        }

        [Theory]
        [InlineData("float", "-101.5", "142.5")]
        [InlineData("double", "-123.92", "128.29")]
        public async Task DomainModelEditor_AddProperty_Should_Add_Range_Attribute_For_Decimal_Types(string type, string min, string max)
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = type,
                IsRequired = false,
                Min = min,
                Max = max
            };

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Contains($"[Range({min}, {max})]", actual);
            Assert.Contains($"public {type}? Name", actual);
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Add_Range_With_Min_Only_For_Numeric_Types()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = "int",
                IsRequired = false,
                Min = "18",
                Max = null
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }

                                 [Range(18, int.MaxValue)]
                                 public int? Name { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Add_Range_With_Max_Only_For_Numeric_Types()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = "int",
                IsRequired = false,
                Min = null,
                Max = "100"
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }

                                 [Range(int.MinValue, 100)]
                                 public int? Name { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_AddProperty_Should_Not_Add_Range_Attribute_When_Neither_Min_Nor_Max_Specified()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var property = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = "int",
                IsRequired = false,
                Min = null,
                Max = null
            };

            var expected = $$"""
                         using System.ComponentModel.DataAnnotations;
                         using System.ComponentModel.DataAnnotations.Schema;
                         using Dappi.HeadlessCms.Models;
                         using Dappi.Core.Attributes;
                         using Dappi.HeadlessCms.Core.Attributes;
                         using Dappi.Core.Enums;

                         namespace {{_assemblyName}}.Entities
                         {
                             [{{CcControllerAttribute.ShortName}}]
                             public class {{DomainModelName}}
                             {
                                 [Key]
                                 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
                                 public Guid Id { get; set; }
                                 public int? Name { get; set; }
                             }
                         }
                         """;

            _domainModelEditor.AddProperty(property);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
        }

        [Fact]
        public async Task DomainModelEditor_UpdateProperty_Should_Update_String_Length_To_Both_Properties()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var originalProperty = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = "string",
                IsRequired = false,
            };

            _domainModelEditor.AddProperty(originalProperty);
            await _domainModelEditor.SaveAsync();

            var updatedProperty = new Property
            {
                DomainModel = DomainModelName,
                Name = "Name",
                Type = "string",
                IsRequired = false,
                Min = "3",
                Max = "55"
            };

            _domainModelEditor.UpdateProperty(DomainModelName, "Name", updatedProperty);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Contains("[Length(3, 55)]", actual);
        }

        [Fact]
        public async Task DomainModelEditor_UpdateProperty_Should_Change_String_From_MinLength_To_Length()
        {
            var modelRequest = new ModelRequest { ModelName = DomainModelName, IsAuditableEntity = false };
            _domainModelEditor.CreateEntityModel(modelRequest);

            var originalProperty = new Property
            {
                DomainModel = DomainModelName,
                Name = "Code",
                Type = "string",
                IsRequired = false,
                Min = "1"
            };
            _domainModelEditor.AddProperty(originalProperty);
            await _domainModelEditor.SaveAsync();

            var updatedProperty = new Property
            {
                DomainModel = DomainModelName,
                Name = "Code",
                Type = "string",
                IsRequired = false,
                Min = "5",
                Max = "15"
            };

            _domainModelEditor.UpdateProperty(DomainModelName, "Code", updatedProperty);
            await _domainModelEditor.SaveAsync();

            var actual = await File.ReadAllTextAsync(_filePath);
            Assert.Contains("[Length(5, 15)]", actual);
            Assert.DoesNotContain("[MinLength", actual);
        }

#pragma warning disable CA1816
        public void Dispose()
#pragma warning restore CA1816
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}