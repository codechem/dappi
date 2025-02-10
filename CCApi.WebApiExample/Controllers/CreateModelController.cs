using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CCApi.WebApiExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateModelController : ControllerBase
    {
        private readonly string _entitiesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Entities");

        public CreateModelController()
        {
            
            if (!Directory.Exists(_entitiesFolderPath))
            {
                Directory.CreateDirectory(_entitiesFolderPath);
            }
        }

        [HttpPost]
        public IActionResult CreateModel([FromBody] ModelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ModelName) || string.IsNullOrWhiteSpace(request.PropertyName) || string.IsNullOrWhiteSpace(request.PropertyType))
            {
                return BadRequest("Model name, property name, and property type must be provided.");
            }

            try
            {
                var modelType = CreateModel(request.ModelName, request.PropertyName, request.PropertyType);

                if (modelType == null)
                {
                    return BadRequest("Failed to create dynamic model.");
                }

                var fileName = $"{modelType.Name}.cs";
                var filePath = Path.Combine(_entitiesFolderPath, fileName);
                var classCode = GenerateClassCode(modelType);
                
                System.IO.File.WriteAllText(filePath, classCode);

                return Ok(new
                {
                    Message = $"Model class '{modelType.Name}' created successfully.",
                    FilePath = filePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private Type CreateModel(string modelName, string propertyName, string propertyType)
        {
            try
            {
                
                var assemblyName = new AssemblyName("DynamicAssembly");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");

                
                var typeBuilder = moduleBuilder.DefineType(modelName, TypeAttributes.Public | TypeAttributes.Class);

                
                var ccControllerAttrConstructor = typeof(CCControllerAttribute).GetConstructor(Type.EmptyTypes);
                var customAttributeBuilder = new CustomAttributeBuilder(ccControllerAttrConstructor, new object[] { });
                typeBuilder.SetCustomAttribute(customAttributeBuilder);

                
                Type type = propertyType.ToLower() switch
                {
                    "string" => typeof(string),
                    "int" => typeof(int),
                    "decimal" => typeof(decimal),
                    "datetime" => typeof(DateTime),
                    _ => throw new InvalidOperationException($"Unsupported property type {propertyType}")
                };

                
                var fieldBuilder = typeBuilder.DefineField($"_{propertyName}", type, FieldAttributes.Private);
                var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, type, null);

                var getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, type, Type.EmptyTypes);
                var ilGenerator = getMethodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                ilGenerator.Emit(OpCodes.Ret);

                var setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { type });
                ilGenerator = setMethodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);
                ilGenerator.Emit(OpCodes.Ret);

                
                propertyBuilder.SetGetMethod(getMethodBuilder);
                propertyBuilder.SetSetMethod(setMethodBuilder);

                
                return typeBuilder.CreateType();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateModel: {ex.Message}");
                return null;
            }
        }

        private string GenerateClassCode(Type modelType)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine("using System.Text.Json.Serialization;");

            sb.AppendLine("using System;");
            sb.AppendLine("using CCApi.SourceGenerator.Attributes;");
            sb.AppendLine($"namespace CCApi.WebApiExample.Entities;");
            sb.AppendLine($"[CCController]");
            sb.AppendLine($"    public class {modelType.Name}");
            sb.AppendLine("    {");
            sb.AppendLine("    [Key]");
            sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
            sb.AppendLine("    public Guid Id { get; set; }");

            
            var properties = modelType.GetProperties();
            foreach (var prop in properties)
            {
                sb.AppendLine($"        public {prop.PropertyType.Name} {prop.Name} {{ get; set; }}");
            }

            sb.AppendLine("    }");

            return sb.ToString();
        }
    }

    public class ModelRequest
    {
        public string ModelName { get; set; }
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CCControllerAttribute : Attribute
    {
    }
}
