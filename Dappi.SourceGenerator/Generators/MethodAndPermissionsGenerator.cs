using System.Collections.Immutable;
using Dappi.Core.Utils;
using Dappi.SourceGenerator.Models;
using Dappi.SourceGenerator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Dappi.SourceGenerator.Generators;

[Generator]
public class MethodAndPermissionsGenerator : BaseSourceModelToSourceOutputGenerator
{
    private const string TemplateResourceName =
        "Dappi.SourceGenerator.Templates.MethodsAndPermissionsTemplate.tpl";

    private class MethodRouteEntry
    {
        public string MethodName { get; set; } = string.Empty;
        public string HttpRoute { get; set; } = string.Empty;
    }

    protected override void Execute(
        SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input
    )
    {
        var (compilation, collectedData) = input;

        var httpMethodAttributesToVerb = new Dictionary<string, string>
        {
            { "HttpGet", "GET" },
            { "HttpPost", "POST" },
            { "HttpPut", "PUT" },
            { "HttpDelete", "DELETE" },
            { "HttpPatch", "PATCH" },
            { "HttpHead", "HEAD" },
            { "HttpOptions", "OPTIONS" },
        };

        foreach (var model in collectedData)
        {
            var existingPartialController = FindPartialControllerForClass(compilation, model);

            if (existingPartialController is null)
                continue;

            var methodsWithHttp = existingPartialController
                .Members.OfType<MethodDeclarationSyntax>()
                .Select(method => new
                {
                    Method = method,
                    HttpAttr = method
                        .AttributeLists.SelectMany(al => al.Attributes)
                        .FirstOrDefault(IsHttpMethodAttribute),
                })
                .Where(x => x.HttpAttr is not null)
                .Select(x =>
                {
                    var route = x
                        .HttpAttr?.ArgumentList?.Arguments.FirstOrDefault()
                        ?.ToFullString();
                    return new MethodRouteEntry
                    {
                        MethodName = x.Method.Identifier.Text,
                        HttpRoute =
                            httpMethodAttributesToVerb[x.HttpAttr?.Name.ToString()!]
                            + "/"
                            + route?.Replace("/", "").Replace("\"", "").Trim(),
                    };
                })
                .ToList();

            var resolvedActions = model
                .CrudActions.Select(action =>
                {
                    var actionName = action.ToString();

                    // TODO: It would be a good idea to move this logic to the base generator so it can be re used in the CrudGenerator, but a refactor of the CrudGenerator is needed first
                    // TODO: Cleanup HttpRoute to not be this shitty
                    return actionName switch
                    {
                        "Get" => new MethodRouteEntry
                        {
                            MethodName = $"Get{model.ClassName.Pluralize()}",
                            HttpRoute = $"GET/{model.ClassName}",
                        },
                        "GetOne" => new MethodRouteEntry
                        {
                            MethodName = $"Get{model.ClassName}",
                            HttpRoute = $"GET/{model.ClassName}/{{id}}",
                        },
                        "GetAll" => new MethodRouteEntry
                        {
                            MethodName = $"GetAll{model.ClassName.Pluralize()}",
                            HttpRoute = $"GET/{model.ClassName}/get-all",
                        },
                        "Create" => new MethodRouteEntry
                        {
                            MethodName = "Create",
                            HttpRoute = $"POST/{model.ClassName}",
                        },
                        "Update" => new MethodRouteEntry
                        {
                            MethodName = "Update",
                            HttpRoute = $"PUT/{model.ClassName}/{{id}}",
                        },
                        "Delete" => new MethodRouteEntry
                        {
                            MethodName = "Delete",
                            HttpRoute = $"DELETE/{model.ClassName}/{{id}}",
                        },
                        "Patch" => new MethodRouteEntry
                        {
                            MethodName = $"JsonPatch{model.ClassName}",
                            HttpRoute = $"PATCH/{model.ClassName}/{{id}}",
                        },
                        _ => throw new NotSupportedException(
                            "Unsupported CRUD action: " + actionName
                        ),
                    };
                })
                .Where(x => x is not null)
                .ToList();

            var allMethods = methodsWithHttp.Concat(resolvedActions).ToList();
            var templateContent = EmbeddedResourceLoader.LoadEmbeddedTemplate(TemplateResourceName);
            var template = Template.Parse(templateContent);

            var output = template.Render(
                new { ControllerName = $"{model.ClassName}Controller", Methods = allMethods }
            );

            context.AddSource($"{model.ClassName}.MethodsAndPermissions.cs", output);
        }
    }

    private static ClassDeclarationSyntax? FindPartialControllerForClass(
        Compilation compilation,
        SourceModel model
    )
    {
        return compilation
            .SyntaxTrees.SelectMany(t => t.GetRoot().DescendantNodes())
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(cds =>
                cds.Identifier.Text == $"{model.ClassName}Controller"
                && cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
            );
    }

    private static bool IsHttpMethodAttribute(AttributeSyntax attr)
    {
        var httpMethodAttributes = new HashSet<string>
        {
            "HttpGet",
            "HttpPost",
            "HttpPut",
            "HttpDelete",
            "HttpPatch",
            "HttpHead",
            "HttpOptions",
        };

        var name = attr.Name.ToString();
        return httpMethodAttributes.Any(h =>
            name == h
            || name == $"{h}Attribute"
            || name.EndsWith($".{h}")
            || name.EndsWith($".{h}Attribute")
        );
    }
}
