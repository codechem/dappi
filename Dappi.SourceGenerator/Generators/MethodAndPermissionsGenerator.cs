using System.Collections.Immutable;
using Dappi.Core.Utils;
using Dappi.SourceGenerator.Extensions;
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

        if (!compilation.HasDappiSystem("UsersAndPermissions"))
        {
            // User & Permissions system not referenced, skip generation
            return;
        }

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

        TryAddUsersAndPermissionsController(
            compilation,
            httpMethodAttributesToVerb,
            controllers,
            context
        );

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

    private void TryAddUsersAndPermissionsController(
        Compilation compilation,
        IReadOnlyDictionary<string, string> httpMethodAttributesToVerb,
        List<ControllerEntry> controllers,
        SourceProductionContext context
    )
    {
        // Adjust this to match the real namespace of your controller
        var possibleTypeNames = new[]
        {
            "Dappi.HeadlessCms.UsersAndPermissions.Api.UsersAndPermissionsController`1",
            "Dappi.HeadlessCms.UsersAndPermissions.Api.UsersAndPermissionsController",
        };

        INamedTypeSymbol? controllerSymbol = null;

        foreach (var name in possibleTypeNames)
        {
            controllerSymbol = compilation.GetTypeByMetadataName(name);
            if (controllerSymbol is not null)
                break;
        }

        if (controllerSymbol is null)
        {
            return;
        }

        var entry = BuildControllerEntryFromSymbol(controllerSymbol, httpMethodAttributesToVerb);
        if (entry is null || entry.Methods.Count == 0)
            return;

        var existing = controllers.FirstOrDefault(c =>
            string.Equals(c.Controller_name, entry.Controller_name, StringComparison.Ordinal)
        );

        if (existing is null)
        {
            controllers.Add(entry);
            return;
        }

        foreach (var m in entry.Methods)
        {
            var alreadyExists = existing.Methods.Any(em =>
                string.Equals(em.MethodName, m.MethodName, StringComparison.Ordinal)
                && string.Equals(em.HttpRoute, m.HttpRoute, StringComparison.OrdinalIgnoreCase)
            );

            if (!alreadyExists)
            {
                existing.Methods.Add(m);
            }
        }
    }

    private ControllerEntry? BuildControllerEntryFromSymbol(
        INamedTypeSymbol controllerSymbol,
        IReadOnlyDictionary<string, string> httpMethodAttributesToVerb
    )
    {
        var controllerEntry = new ControllerEntry
        {
            Controller_name = controllerSymbol.Name,
            Methods = new List<MethodRouteEntry>(),
        };

        foreach (var member in controllerSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (
                member.MethodKind != MethodKind.Ordinary
                || member.DeclaredAccessibility != Accessibility.Public
            )
                continue;

            var httpAttr = member
                .GetAttributes()
                .FirstOrDefault(a => IsHttpMethodAttributeSymbol(a.AttributeClass));

            if (httpAttr is null)
                continue;

            var attrClassName = httpAttr.AttributeClass?.Name;
            if (attrClassName is null)
                continue;

            if (
                !httpMethodAttributesToVerb.TryGetValue(
                    attrClassName.Replace("Attribute", string.Empty),
                    out var verb
                )
            )
            {
                continue;
            }

            string? cleanedRoute = null;
            if (httpAttr.ConstructorArguments.Length > 0)
            {
                var arg = httpAttr.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string s)
                {
                    cleanedRoute = s.Replace("/", string.Empty).Trim();
                }
            }

            var routePart = cleanedRoute ?? string.Empty;

            controllerEntry.Methods.Add(
                new MethodRouteEntry
                {
                    MethodName = member.Name,
                    HttpRoute = string.IsNullOrWhiteSpace(routePart) ? verb : $"{verb}/{routePart}",
                }
            );
        }

        return controllerEntry.Methods.Count == 0 ? null : controllerEntry;
    }

    private static bool IsHttpMethodAttributeSymbol(INamedTypeSymbol? attrSymbol)
    {
        if (attrSymbol is null)
            return false;

        var name = attrSymbol.Name; // e.g. HttpGetAttribute or HttpGet
        return name == "HttpGet"
            || name == "HttpPost"
            || name == "HttpPut"
            || name == "HttpDelete"
            || name == "HttpPatch"
            || name == "HttpHead"
            || name == "HttpOptions"
            || name == "HttpGetAttribute"
            || name == "HttpPostAttribute"
            || name == "HttpPutAttribute"
            || name == "HttpDeleteAttribute"
            || name == "HttpPatchAttribute"
            || name == "HttpHeadAttribute"
            || name == "HttpOptionsAttribute";
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
