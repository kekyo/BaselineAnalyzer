using Microsoft.CodeAnalysis;

namespace BaselineAnalyzer;

internal static class Utilities
{
    public static bool IsType(this ITypeSymbol symbol, string nsName, string typeBaseName) =>
        symbol.ContainingNamespace.ToDisplayString() == nsName &&
        symbol.Name == typeBaseName;
}