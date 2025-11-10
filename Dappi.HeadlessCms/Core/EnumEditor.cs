using System.Reflection;
using Dappi.HeadlessCms.Core.Extensions;
using Dappi.HeadlessCms.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dappi.HeadlessCms.Core
{
    public class EnumEditor(string enumFolderPath)
    {
        private bool HasChanges { get; set; }
        private string _currentCode = string.Empty;
        private string _enumName = string.Empty;
        public void CreateEnum(string enumName)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            
            _enumName = enumName;
            var enumDeclaration = SyntaxFactory.EnumDeclaration(enumName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            
            var namesSpaceDeclaration = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(assemblyName + ".Enums"))
                .AddMembers(enumDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddMembers(namesSpaceDeclaration);
            
            _currentCode = compilationUnit.NormalizeWhitespace().ToFullString();
            HasChanges = true;
        }

        public void UpdateEnum(string enumName , EnumFieldRequest request)
        {
            _enumName = enumName;
            var filePath = Path.Combine(enumFolderPath, $"{enumName}.cs");
            var syntaxTree = string.IsNullOrWhiteSpace(_currentCode)
                ? RoslynHelpers.GetSyntaxTreeFromSource(filePath)
                : CSharpSyntaxTree.ParseText(_currentCode);

            var root = syntaxTree.GetCompilationUnitRoot();
            var enumNode = root.DescendantNodes().OfType<EnumDeclarationSyntax>().FirstOrDefault();
            if (enumNode is null)
            {
                throw new Exception("Failed to find enum declaration");
            }

            if (enumNode.Members.Any(m => m.Identifier.Text == request.Name))
            {
                throw new ArgumentException($"A member with the name '{request.Name}' already exists.");
            }
            if (request.Value is not null && enumNode.Members.Any(m => 
                    m.EqualsValue?.Value is LiteralExpressionSyntax literal &&
                    literal.Token.Value != null &&
                    literal.Token.Value.Equals(request.Value.Value)))
            {
                throw new ArgumentException($"A member with the value '{request.Value}' already exists.");
            }
            
            
            var newMember = SyntaxFactory.EnumMemberDeclaration(request.Name);
            if (request.Value is not null)
            {
                newMember = newMember.WithEqualsValue(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((int)request.Value))
                ));
            }
            var newNode = enumNode.AddMembers(newMember);
            
            var newRoot = root.ReplaceNode(enumNode, newNode);
            _currentCode = newRoot.NormalizeWhitespace().ToFullString();
            HasChanges = true;
        }

        public async Task SaveAsync()
        {
            var path = Path.Combine(enumFolderPath, $"{_enumName}.cs");
            await File.WriteAllTextAsync(path, _currentCode);
        }
    }
}