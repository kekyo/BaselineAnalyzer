////////////////////////////////////////////////////////////////////////////
//
// BaselineAnalyzer - Analyzer that is kind to C# beginners.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BaselineAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TaskBlockingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "BLA0021",
        "Asynchronous code blocking operations should be avoided",
        "Using '{0}' in asynchronous methods may cause deadlock",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "Result may cause a DANGER deadlock and is not recommended for asynchronous methods.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        var memberSymbol = symbolInfo.Symbol;

        if ((memberSymbol?.ContainingType?.IsType("System.Threading.Tasks", "Task") ?? false) &&
            (memberSymbol.Name == "Wait" || memberSymbol.Name == "Result"))
        {
            var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation(), memberSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}