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

    private class ControllerEntry
    {
        public string Controller_name { get; set; } = string.Empty;
        public List<MethodRouteEntry> Methods { get; set; } = new();
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

        // Aggregate all controllers with their methods
        var controllers = new List<ControllerEntry>();

        foreach (var model in collectedData)
        {
            var controllerName = $"{model.ClassName}Controller";

            var controllerEntry = new ControllerEntry
            {
                Controller_name = controllerName,
                Methods = new List<MethodRouteEntry>(),
            };

            var existingPartialController = FindPartialControllerForClass(compilation, model);

            if (existingPartialController is not null)
            {
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
                        var routeArg = x
                            .HttpAttr?.ArgumentList?.Arguments.FirstOrDefault()
                            ?.ToFullString();
                        var cleanedRoute = routeArg
                            ?.Replace("/", string.Empty)
                            .Replace("\"", string.Empty)
                            .Trim();

                        var verb = httpMethodAttributesToVerb[x.HttpAttr!.Name.ToString()!];

                        return new MethodRouteEntry
                        {
                            MethodName = x.Method.Identifier.Text,
                            HttpRoute = $"{verb}/{cleanedRoute}",
                        };
                    })
                    .ToList();

                controllerEntry.Methods.AddRange(methodsWithHttp);
            }

            var resolvedActions = model
                .CrudActions.Select(action =>
                {
                    var actionName = action.ToString();

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
                            HttpRoute = $"DELETE/{model.ClassName}{{id}}",
                        },
                        "Patch" => new MethodRouteEntry
                        {
                            MethodName = $"JsonPatch{model.ClassName}",
                            HttpRoute = $"PATCH/{model.ClassName}/{{id}}",
                        },
                        _ => null,
                    };
                })
                .Where(x => x is not null)!
                .ToList();

            controllerEntry.Methods.AddRange(resolvedActions);

            if (controllerEntry.Methods.Count > 0)
            {
                controllers.Add(controllerEntry);
            }
        }

        var templateContent = EmbeddedResourceLoader.LoadEmbeddedTemplate(TemplateResourceName);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            foreach (var message in template.Messages)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "DAPPMETHODS001",
                            title: "Methods & Permissions template error",
                            messageFormat: message.Message,
                            category: "SourceGenerator",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true
                        ),
                        Location.None
                    )
                );
            }
            return;
        }

        var output = template.Render(new { controllers });

        context.AddSource("MethodsAndPermissions.g.cs", output);
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
