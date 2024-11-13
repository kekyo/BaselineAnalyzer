using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace BaselineAnalyzer;

[Parallelizable(ParallelScope.All)]
public class CatchAnalyzerTests
{
    private DiagnosticAnalyzer analyzer;

    [SetUp]
    public void Setup()
    {
        this.analyzer = new CatchAnalyzer();
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
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics.ToArray();
    }

    [Test]
    public async Task DetectsMissingRethrowInCatchBlock()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0001", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("The catch block does not contain a throw statement"));
    }

    [Test]
    public async Task DoesNotDetectWhenRethrowIsPresent()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported.");
    }

    [Test]
    public async Task DetectsMissingRethrowInComplexCatchBlock1()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static bool Flag;
    
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                if (Flag)
                {
                    Console.WriteLine(""Flag is on"");
                    throw;
                }
                else
                {
                    Console.WriteLine(""Flag is off"");
                }
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported, as a throw statement is present.");
    }

    [Test]
    public async Task DetectsMissingRethrowInComplexCatchBlock2()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static bool Flag;
    
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                if (Flag)
                {
                    Console.WriteLine(""Flag is on"");
                }
                else
                {
                    Console.WriteLine(""Flag is off"");
                    throw;
                }
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported, as a throw statement is present.");
    }

    [Test]
    public async Task DetectsMissingRethrowInComplexCatchBlock3()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static bool Flag;
    
        public void TestMethod()
        {
            try
            {
                try
                {
                    // Do something
                }
                catch (Exception ex)
                {
                    if (Flag)
                    {
                        Console.WriteLine(""""Flag is on"""");
                        throw;
                    }
                    else
                    {
                        Console.WriteLine(""""Flag is off"""");
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported, as a throw statement is present.");
    }

    [Test]
    public async Task DetectsMissingRethrowInComplexCatchBlock4()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static bool Flag;
    
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                try
                {
                    // Do something
                }
                catch (Exception ex)
                {
                    if (Flag)
                    {
                        Console.WriteLine(""Flag is on"");
                    }
                    else
                    {
                        Console.WriteLine(""Flag is off"");
                    }
                }
                throw;
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0001", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("The catch block does not contain a throw statement"));
    }

    [Test]
    public async Task DetectsMissingRethrowInComplexCatchBlock5()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static bool Flag;
    
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                try
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (Flag)
                    {
                        Console.WriteLine(""Flag is on"");
                    }
                    else
                    {
                        Console.WriteLine(""Flag is off"");
                    }
                }
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0001", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("The catch block does not contain a throw statement"));
    }

    [Test]
    public async Task DetectsMissingRethrowInCatchBlock6()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch
            {
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0001", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("The catch block does not contain a throw statement"));
    }

    [Test]
    public async Task DetectsMissingRethrowInCatchBlock7()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch
            {
                throw;
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsEmpty(diagnostics, "No diagnostics should be reported, as a throw statement is present.");
    }

    [Test]
    public async Task DetectsRethrowWithCurrentException()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Do something
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}";
    
        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.IsNotEmpty(diagnostics, "Expected a diagnostic to be reported.");
        var diagnostic = diagnostics.First();
        Assert.AreEqual("BLA0002", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage().Contains("Avoid using 'throw ex;' in catch blocks, use 'throw;' instead to preserve stack trace"));
    }
}
