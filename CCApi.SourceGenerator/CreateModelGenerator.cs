using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CCApi.SourceGenerator
{
    [Generator]
    public class CreateModelGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(
           context.CompilationProvider,
           (context, compilation) => GenerateController(context, compilation)
       );
        }

        private void GenerateController(SourceProductionContext context, Compilation compilation)
        {
            var rootNamespace = compilation.AssemblyName ?? "DefaultNamespace";

            var sourceText = SourceText.From($@"
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CCApi.WebApiExample.Controllers
{{
    [Route(""api/[controller]"")]
    [ApiController]
    public partial class CreateModelController : ControllerBase
    {{
        private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), ""Entities"");

        public CreateModelController()
        {{
            if (!Directory.Exists(_entitiesFolderPath))
            {{
                Directory.CreateDirectory(_entitiesFolderPath);
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
    }}

    public class ModelRequest
    {{
        public string ModelName {{ get; set; }}
    }}

    [AttributeUsage(AttributeTargets.Class)]
    public class CCControllerAttribute : Attribute
    {{
    }}
}}
", Encoding.UTF8);
            context.AddSource("MigrationControllerGenerator.cs", sourceText);
        }
    }
}
