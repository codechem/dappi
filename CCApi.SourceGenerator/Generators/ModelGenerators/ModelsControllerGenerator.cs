using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CCApi.SourceGenerator.Generators.ModelGenerators
{
    [Generator]
    public class ModelsControllerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(context.CompilationProvider, (context, compilation) => GenerateController(context, compilation));
        }

        private void GenerateController(SourceProductionContext context, Compilation compilation)
        {
            var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";

            var sourceText = SourceText.From(
$@"using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using {rootNamespace}.Extensions;

namespace {rootNamespace}.Controllers
{{
    [ApiExplorerSettings(GroupName = ""Toolkit"")]
    [Route(""api/models"")]
    [ApiController]
    public partial class ModelsController : ControllerBase
    {{
        private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), ""Entities"");
        private readonly string _controllersFolderPath = Path.Combine(Directory.GetCurrentDirectory(), ""Controllers"");

        public ModelsController()
        {{
            if (!Directory.Exists(_entitiesFolderPath))
            {{
                Directory.CreateDirectory(_entitiesFolderPath);
            }}
        }}

        [HttpGet]
        public IActionResult GetAllModels()
        {{
            try
            {{
                if (!Directory.Exists(_entitiesFolderPath))
                {{
                    return NotFound(""Entities directory not found."");
                }}

                var modelNames = Directory.GetFiles(_entitiesFolderPath, ""*.cs"")
                                          .Select(Path.GetFileNameWithoutExtension)
                                          .ToList();

                return Ok(modelNames);
            }}
            catch (Exception ex)
            {{
                return StatusCode(500, $""Internal server error: {{ex.Message}}"");
            }}
        }}

        [HttpPost]
        public IActionResult CreateModel([FromBody] ModelRequest request)
        {{
            if (string.IsNullOrWhiteSpace(request.ModelName))
            {{
                return BadRequest(""Model name must be provided."");
            }}

            // Validate model name
            if (!IsValidClassName(request.ModelName))
            {{
                return BadRequest($""The model name '{{request.ModelName}}' is not a valid C# class name."");
            }}
            var modelNames = Directory.GetFiles(_entitiesFolderPath, ""*.cs"")
                                      .Select(Path.GetFileNameWithoutExtension)
                                      .ToList();

            if (modelNames.Contains(request.ModelName))
            {{
                return BadRequest($""A model with the name '{{request.ModelName}}' already exists."");
            }}

            try
            {{
                var modelType = CreateModel(request.ModelName);
                if (modelType == null)
                {{
                    return BadRequest(""Failed to create dynamic model."");
                }}

                var fileName = $""{{modelType.Name}}.cs"";
                var filePath = Path.Combine(_entitiesFolderPath, fileName);
                var classCode = GenerateClassCode(modelType);

                System.IO.File.WriteAllText(filePath, classCode);

                return Ok(new
                {{
                    Message = $""Model class '{{modelType.Name}}' created successfully."",
                    FilePath = filePath
                }});
            }}
            catch (Exception ex)
            {{
                return StatusCode(500, $""Internal server error: {{ex.Message}}"");
            }}
        }}

        [HttpDelete(""{{modelName}}"")]
        public IActionResult DeleteModel(string modelName)
        {{
            if (string.IsNullOrWhiteSpace(modelName))
            {{
                return BadRequest(""Model name must be provided."");
            }}

            var modelFilePath = Path.Combine(_entitiesFolderPath, $""{{modelName}}.cs"");

            try
            {{
                if (!System.IO.File.Exists(modelFilePath))
                {{
                    return NotFound(""Model file not found."");
                }}

                System.IO.File.Delete(modelFilePath);

                var dbContextFilePath = Path.Combine(Directory.GetCurrentDirectory(), ""Data"", ""AppDbContext.cs"");

                string dbContextContent = System.IO.File.ReadAllText(dbContextFilePath);
                string pattern = $@""\s*public\s+DbSet<{{modelName}}>\s+{{modelName}}s\s+\{{{{\s+get; set;\s+\}}}}"";
                dbContextContent = Regex.Replace(dbContextContent, pattern, string.Empty, RegexOptions.Multiline);

                System.IO.File.WriteAllText(dbContextFilePath, dbContextContent);

                var controllerFilePath = Path.Combine(_controllersFolderPath, $""{{modelName}}Controller.cs"");

                if (System.IO.File.Exists(controllerFilePath))
                {{
                    System.IO.File.Delete(controllerFilePath);
                }}          
                
                return Ok(new
                {{
                    Message = $""Model '{{modelName}}' deleted successfully."",
                    FilePath = modelFilePath
                }});
            }}
            catch (Exception ex)
            {{
                return StatusCode(500, $""Internal server error: {{ex.Message}}"");
            }}
        }}

        [HttpPut(""{{modelName}}"")]
        public IActionResult AddField(string modelName, [FromBody] FieldRequest request)
        {{
            if (string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(request.FieldName) || string.IsNullOrWhiteSpace(request.FieldType))
            {{
                return BadRequest(""Model name, field name, and field type must be provided."");
            }}

            try
            {{
                var modelFilePath = Path.Combine(_entitiesFolderPath, $""{{modelName}}.cs"");

                if (!System.IO.File.Exists(modelFilePath))
                {{
                    return NotFound(""Model class not found."");
                }}

                var existingCode = System.IO.File.ReadAllText(modelFilePath);

                var updatedCode = AddFieldToClass(existingCode, request.FieldName, request.FieldType);

                System.IO.File.WriteAllText(modelFilePath, updatedCode);

                return Ok(new
                {{
                    Message = $""Field '{{request.FieldName}}' of type '{{request.FieldType}}' added successfully to '{{modelName}}' model."",
                    FilePath = modelFilePath
                }});
            }}
            catch (Exception ex)
            {{
                return StatusCode(500, $""Internal server error: {{ex.Message}}"");
            }}
        }}

        private string AddFieldToClass(string classCode, string fieldName, string fieldType)
        {{
            if (PropertyCheckExtensions.PropertyExists(classCode, fieldName, fieldType))
            {{
                throw new InvalidOperationException($""The property '{{fieldName}}' of type '{{fieldType}}' already exists in the class."");
            }}
            var propertyCode = $""    public {{fieldType}} {{fieldName}} {{{{ get; set; }}}}"";
            var insertPosition = classCode.LastIndexOf(""}}"", StringComparison.Ordinal); 
            var updatedCode = classCode.Insert(insertPosition, Environment.NewLine + propertyCode + Environment.NewLine);

            return updatedCode;
        }}

        private bool IsValidClassName(string name)
        {{
            if (string.IsNullOrEmpty(name))
            {{
                return false;
            }}
            if (!(char.IsLetter(name[0]) || name[0] == '_'))
            {{
                return false;
            }}
            for (int i = 1; i < name.Length; i++)
            {{
                if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_'))
                {{
                    return false;
                }}
            }}

            var reservedKeywords = new HashSet<string>(StringComparer.Ordinal)
            {{
                ""abstract"", ""as"", ""base"", ""bool"", ""break"", ""byte"", ""case"", ""catch"",
                ""char"", ""checked"", ""class"", ""const"", ""continue"", ""decimal"", ""default"",
                ""delegate"", ""do"", ""double"", ""else"", ""enum"", ""event"", ""explicit"", ""extern"",
                ""false"", ""finally"", ""fixed"", ""float"", ""for"", ""foreach"", ""goto"", ""if"",
                ""implicit"", ""in"", ""int"", ""interface"", ""internal"", ""is"", ""lock"", ""long"",
                ""namespace"", ""new"", ""null"", ""object"", ""operator"", ""out"", ""override"", ""params"",
                ""private"", ""protected"", ""public"", ""readonly"", ""ref"", ""return"", ""sbyte"",
                ""sealed"", ""short"", ""sizeof"", ""stackalloc"", ""static"", ""string"", ""struct"",
                ""switch"", ""this"", ""throw"", ""true"", ""try"", ""typeof"", ""uint"", ""ulong"",
                ""unchecked"", ""unsafe"", ""ushort"", ""using"", ""virtual"", ""void"", ""volatile"", ""while""
            }};

            if (reservedKeywords.Contains(name))
            {{
                return false;
            }}
            return true;
        }}

        private Type CreateModel(string modelName)
        {{
            try
            {{
                var assemblyName = new AssemblyName(""DynamicAssembly"");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule(""TempModule"");

                var typeBuilder = moduleBuilder.DefineType(
                    modelName, 
                    System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

                var ccControllerAttrConstructor = typeof(CCControllerAttribute).GetConstructor(Type.EmptyTypes);
                var customAttributeBuilder = new CustomAttributeBuilder(ccControllerAttrConstructor, new object[] {{ }});
                typeBuilder.SetCustomAttribute(customAttributeBuilder);

                return typeBuilder.CreateType();
            }}
            catch (Exception ex)
            {{
                Console.WriteLine($""Error in CreateModel: {{ex.Message}}"");
                return null;
            }}
        }}

        private string GenerateClassCode(Type modelType)
        {{
            var sb = new StringBuilder();
            sb.AppendLine(""using System.ComponentModel.DataAnnotations;"");
            sb.AppendLine(""using System.ComponentModel.DataAnnotations.Schema;"");
            sb.AppendLine(""using System.Text.Json.Serialization;"");
            sb.AppendLine(""using System;"");
            sb.AppendLine(""using CCApi.SourceGenerator.Attributes;"");
            sb.AppendLine(""namespace CCApi.WebApiExample.Entities;"");
            sb.AppendLine(""[CCController]"");
            sb.AppendLine($""public class {{modelType.Name}}"");
            sb.AppendLine(""{{"");
            sb.AppendLine(""    [Key]"");
            sb.AppendLine(""    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]"");
            sb.AppendLine(""    public Guid Id {{ get; set; }}"");

            sb.AppendLine(""}}"");

            return sb.ToString();
        }}

        [HttpGet(""fields/{{modelName}}"")]
        public IActionResult GetModelFields(string modelName)
        {{
            var modelFilePath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), ""Entities""), $""{{modelName}}.cs"");
            if (!System.IO.File.Exists(modelFilePath))
            {{
                return NotFound($""Model '{{{{modelName}}}}' not found."");
            }}

            var modelCode = System.IO.File.ReadAllText(modelFilePath);
            var fieldData = ExtractFieldsFromModel(modelCode);

            return Ok(fieldData);
        }}

        private List<object> ExtractFieldsFromModel(string classCode)
        {{
            var fieldList = new List<object>();
            var propertyPattern = new Regex(@""public\s+([\w<>\[\]]+)\s+(\w+)\s*\{{\s*get;\s*set;\s*}}"", RegexOptions.Multiline);

            var matches = propertyPattern.Matches(classCode);
            foreach (Match match in matches)
            {{
                if (match.Groups.Count == 3)
                {{
                    var fieldType = match.Groups[1].Value;
                    var fieldName = match.Groups[2].Value;
                    fieldList.Add(new {{ FieldName = fieldName, FieldType = fieldType }});
                }}
            }}
            return fieldList;
        }}
    }}

    public class ModelRequest
    {{
        public string ModelName {{ get; set; }}
    }}

    public class FieldRequest
    {{
        public string FieldName {{ get; set; }}
        public string FieldType {{ get; set; }}
    }}

    [AttributeUsage(AttributeTargets.Class)]
    public class CCControllerAttribute : Attribute
    {{
    }}
}}", Encoding.UTF8);
            context.AddSource("ModelsController.cs", sourceText);
        }
    }
}
