////////////////////////////////////////////////////////////////////////////
//
// BaselineAnalyzer - Analyzer that is kind to C# beginners.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BaselineAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncMethodNamingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor bla0011 = new DiagnosticDescriptor(
        "BLA0011",
        "asynchronous method names must include the 'Async' suffix",
        "Asynchronous method '{0}' must include the 'Async' suffix",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Methods that return awaitable types are recommended to use 'Async' at the end of the method name.");

    private static readonly DiagnosticDescriptor bla0012 = new DiagnosticDescriptor(
        "BLA0012",
        "synchronous method names must not include the 'Async' suffix",
        "Synchronous method '{0}' must not include the 'Async' suffix",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Methods that return non-awaitable types are recommended not to use “Async” at the end of the method name.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(bla0011, bla0012);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Not extension method
        if (!(methodDeclaration.ParameterList.Parameters.FirstOrDefault() is ParameterSyntax firstParam &&
            firstParam.Modifiers.Any(SyntaxKind.ThisKeyword)))
        {
            // Get return type
            var returnType = context.SemanticModel.GetTypeInfo(methodDeclaration.ReturnType).Type;
            if (returnType == null)
            {
                return;
            }

            // Return type is Task or ValueTask
            if (returnType.IsType("System.Threading.Tasks", "Task") ||
                returnType.IsType("System.Threading.Tasks", "ValueTask"))
            {
                ReportDiagnosticIfNotAsync(methodDeclaration, context);
                return;
            }

            // Return type is IAsyncEnumerable<T>
            if (returnType.IsType("System.Collections.Generic", "IAsyncEnumerable"))
            {
                ReportDiagnosticIfNotAsync(methodDeclaration, context);
                return;
            }

            // Return type contains `GetAwaiter()` method
            if (returnType.GetMembers("GetAwaiter").FirstOrDefault() is IMethodSymbol { Parameters.Length: 0 })
            {
                ReportDiagnosticIfNotAsync(methodDeclaration, context);
                return;
            }
        }

        // Applied `Async` suffix
        if (methodDeclaration.Identifier.Text.EndsWith("Async"))
        {
            var diagnostic = Diagnostic.Create(bla0012, methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void ReportDiagnosticIfNotAsync(MethodDeclarationSyntax methodDeclaration, SyntaxNodeAnalysisContext context)
    {
        // Not applied `Async` suffix
        if (!methodDeclaration.Identifier.Text.EndsWith("Async"))
        {
            var diagnostic = Diagnostic.Create(bla0011, methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
