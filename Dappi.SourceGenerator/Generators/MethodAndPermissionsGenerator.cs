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
        public string ControllerName { get; set; } = string.Empty;
        public List<MethodRouteEntry> Methods { get; set; } = [];
    }

    protected override void Execute(
        SourceProductionContext context,
        (Compilation Compilation, ImmutableArray<SourceModel> CollectedData) input
    )
    {
        var (compilation, collectedData) = input;

        // User & Permissions system not referenced, skip generation
        if (!compilation.HasDappiSystem("UsersAndPermissions"))
        {
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

        var usersAndPermissionsControllerEntry = GetUsersAndPermissionsControllerEntry(
            compilation,
            httpMethodAttributesToVerb
        );

        if (usersAndPermissionsControllerEntry is null)
        {
            return;
        }

        var controllers = new List<ControllerEntry>() { usersAndPermissionsControllerEntry };

        foreach (var model in collectedData)
        {
            var controllerName = $"{model.ClassName}Controller";

            var controllerEntry = new ControllerEntry
            {
                ControllerName = controllerName,
                Methods = [],
            };

            var partialControllerActions = ScanForPartialControllerAndGetActions(
                compilation,
                model,
                httpMethodAttributesToVerb
            );

            var sourceGeneratedControllerActions = CreateSourceGeneratedControllerActions(model);

            var actionsToAdd = sourceGeneratedControllerActions
                .Concat(partialControllerActions)
                .ToArray();

            if (actionsToAdd.Length > 0)
            {
                controllerEntry.Methods.AddRange(actionsToAdd);
                controllers.Add(controllerEntry);
            }
        }

        var templateContent = EmbeddedResourceLoader.LoadEmbeddedTemplate(TemplateResourceName);
        var template = Template.Parse(templateContent);
        var output = template.Render(new { controllers });

        context.AddSource("MethodsAndPermissions.g.cs", output);
    }

    private static List<MethodRouteEntry> CreateSourceGeneratedControllerActions(SourceModel model)
    {
        return model
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
                        HttpRoute = $"DELETE/{model.ClassName}/{{id}}",
                    },
                    "Patch" => new MethodRouteEntry
                    {
                        MethodName = $"JsonPatch{model.ClassName}",
                        HttpRoute = $"PATCH/{model.ClassName}/{{id}}",
                    },
                    _ => null,
                };
            })
            .Where(x => x is not null)
            .OfType<MethodRouteEntry>()
            .ToList();
    }

    private static List<MethodRouteEntry> ScanForPartialControllerAndGetActions(
        Compilation compilation,
        SourceModel model,
        Dictionary<string, string> httpMethodAttributesToVerb
    )
    {
        var existingPartialController = FindPartialControllerForClass(compilation, model);

        if (existingPartialController is null)
        {
            return [];
        }

        var semanticModel = compilation.GetSemanticModel(existingPartialController.SyntaxTree);
        var partialSymbol = semanticModel.GetDeclaredSymbol(existingPartialController);
        if (partialSymbol is null)
        {
            return [];
        }

        var partialEntry = BuildControllerEntryFromSymbol(
            partialSymbol,
            httpMethodAttributesToVerb
        );

        return partialEntry is not null ? partialEntry.Methods : [];
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

    private static ControllerEntry? GetUsersAndPermissionsControllerEntry(
        Compilation compilation,
        IReadOnlyDictionary<string, string> httpMethodAttributesToVerb
    )
    {
        var possibleTypeNames = new[]
        {
            "Dappi.HeadlessCms.UsersAndPermissions.Api.UsersAndPermissionsController`1",
            "Dappi.HeadlessCms.UsersAndPermissions.Api.UsersAndPermissionsController",
        };

        INamedTypeSymbol? controllerSymbol = null;

        foreach (var name in possibleTypeNames)
        {
            controllerSymbol = compilation.GetTypeByMetadataName(name);
            if (controllerSymbol != null)
                break;
        }

        return controllerSymbol is null
            ? null
            : BuildControllerEntryFromSymbol(controllerSymbol, httpMethodAttributesToVerb);
    }

    private static ControllerEntry? BuildControllerEntryFromSymbol(
        INamedTypeSymbol controllerSymbol,
        IReadOnlyDictionary<string, string> httpMethodAttributesToVerb
    )
    {
        var controllerEntry = new ControllerEntry
        {
            ControllerName = controllerSymbol.Name,
            Methods = [],
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
                if (arg is { Kind: TypedConstantKind.Primitive, Value: string s })
                {
                    cleanedRoute = s.Trim('/');
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
        var name = attrSymbol?.Name;
        return name
            is "HttpGet"
                or "HttpPost"
                or "HttpPut"
                or "HttpDelete"
                or "HttpPatch"
                or "HttpHead"
                or "HttpOptions"
                or "HttpGetAttribute"
                or "HttpPostAttribute"
                or "HttpPutAttribute"
                or "HttpDeleteAttribute"
                or "HttpPatchAttribute"
                or "HttpHeadAttribute"
                or "HttpOptionsAttribute";
    }
}
