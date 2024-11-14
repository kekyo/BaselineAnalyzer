////////////////////////////////////////////////////////////////////////////
//
// BaselineAnalyzer - Analyzer that is kind to C# beginners.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System.Linq;

namespace BaselineAnalyzer;

[Parallelizable(ParallelScope.All)]
public sealed class TaskBlockingAnalyzerTests
{
    private DiagnosticAnalyzer analyzer;

    [SetUp]
    public void Setup()
    {
        analyzer = new TaskBlockingAnalyzer();
    }

    private async Task<Diagnostic[]> GetDiagnosticsAsync(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = AppDomain.CurrentDomain.GetAssemblies().
            Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location)).
            Select(a => MetadataReference.CreateFromFile(a.Location)).
            Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create("TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics.ToArray();
    }

    [Test]
    public async Task ReportsDiagnosticForTaskWait()
    {
        var testCode = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Task task = Task.CompletedTask;
            task.Wait();
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0021", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("Wait"));
    }

    [Test]
    public async Task ReportsDiagnosticForTaskResult1()
    {
        var testCode = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Task<int> task = Task.FromResult(42);
            var result = task.Result;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0021", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("Result"));
    }

    [Test]
    public async Task ReportsDiagnosticForTaskResult2()
    {
        var testCode = @"
using System.Threading.Tasks;
using System.IO;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(Stream stream)
        {
            var buffer = new byte[10];
            stream.ReadAsync(buffer, 0, 10).Wait();
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0021", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("Wait"));
    }
}