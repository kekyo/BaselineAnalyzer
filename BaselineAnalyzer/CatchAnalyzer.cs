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
public class CatchAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor bla0001 = new DiagnosticDescriptor(
        "BLA0001",
        "catch block should throw exception",
        "The catch block does not contain a throw statement",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Detects catch blocks that do not throw exceptions.");

    private static readonly DiagnosticDescriptor bla0002 = new DiagnosticDescriptor(
        "BLA0002",
        "avoid using rethrow with explicit caught exception",
        "Avoid using 'throw {0};' in catch blocks, use 'throw;' instead to preserve stack trace",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When rethrowing, do not specify caught exceptions.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(bla0001, bla0002);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(context =>
            AnalyzeCatchClause(context, (CatchClauseSyntax)context.Node),
            SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatchClause(
        SyntaxNodeAnalysisContext context, CatchClauseSyntax catchClause)
    {
        var walker = new ThrowWalker(context);
        walker.Visit(catchClause.Block);
        if (!walker.HasThrow)
        {
            var diagnostic = Diagnostic.Create(bla0001, catchClause.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        
        // Detect 'throw ex;' instead of 'throw;'
        if (catchClause.Declaration != null)
        {
            var catchExceptionIdentifier = catchClause.Declaration.Identifier;
            foreach (var statement in catchClause.Block.Statements)
            {
                if (statement is ThrowStatementSyntax throwStatement &&
                    throwStatement.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == catchExceptionIdentifier.Text)
                {
                    var diagnostic = Diagnostic.Create(bla0002, throwStatement.GetLocation(),
                        catchClause.Declaration.Identifier.Text);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private class ThrowWalker : CSharpSyntaxWalker
    {
        private readonly SyntaxNodeAnalysisContext context;

        public ThrowWalker(SyntaxNodeAnalysisContext context) =>
            this.context = context;
        
        public bool HasThrow { get; private set; }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            this.HasThrow = true;
            base.VisitThrowStatement(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node) =>
            AnalyzeCatchClause(this.context, node);
    }
}
