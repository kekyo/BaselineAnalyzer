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
public sealed class AsyncMethodNamingAnalyzerTests
{
    private DiagnosticAnalyzer analyzer;

    [SetUp]
    public void Setup()
    {
        analyzer = new AsyncMethodNamingAnalyzer();
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
    public async Task ReportsDiagnosticWhenAsyncMethodNameDoesNotEndWithAsync()
    {
        var testCode = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public Task FetchData()
        {
            return Task.CompletedTask;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0011", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("FetchData"));
    }

    [Test]
    public async Task DoesNotReportDiagnosticWhenAsyncMethodNameEndsWithAsync()
    {
        var testCode = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public Task FetchDataAsync()
        {
            return Task.CompletedTask;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported, as the method name ends with 'Async'.");
    }

    [Test]
    public async Task DoesNotReportDiagnosticForNonAsyncReturnTypes()
    {
        var testCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int GetNumber()
        {
            return 42;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported for methods that do not return an awaitable type.");
    }

    [Test]
    public async Task ReportsDiagnosticForMethodReturningIAsyncEnumerableWithoutAsyncSuffix()
    {
        var testCode = @"
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
        public IAsyncEnumerable<int> FetchItems()
        {
            yield return 42;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0011", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("FetchItems"));
    }

    [Test]
    public async Task ReportsDiagnosticForMethodReturningNonAwaitableWithAsyncSuffix()
    {
        var testCode = @"
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
        public int FetchItemsAsync()
        {
            return 123;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0012", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("FetchItemsAsync"));
    }

    [Test]
    public async Task DoesNotReportDiagnosticWhenAsyncMethodNameEndsWithNotAsyncInExtension()
    {
        var testCode = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    public static class TestClass
    {
        public static IAsyncEnumerable<int> Filter(this IAsyncEnumerable<int> enumerable)
        {
            return enumerable;
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported, as the method name ends with 'Async'.");
    }
}
