#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Parses a classified C# expression text into a Roslyn <see cref="SyntaxTree"/>
/// using <see cref="SourceCodeKind.Script"/> so single-expression bodies parse without
/// needing a surrounding method. Roslyn parse diagnostics are later surfaced as
/// <c>UNO2006</c> by the analyzer with the original XAML location.
/// </summary>
internal static class CSharpExpressionParser
{
	public static (SyntaxTree Tree, bool HasErrors) Parse(string innerCSharp)
	{
		var aliased = OperatorAliases.Replace(innerCSharp);
		var quoted = QuoteTransform.Transform(aliased);

		var options = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
		var tree = CSharpSyntaxTree.ParseText(quoted, options);
		var hasErrors = false;

		foreach (var diag in tree.GetDiagnostics())
		{
			if (diag.Severity == DiagnosticSeverity.Error)
			{
				hasErrors = true;
				break;
			}
		}

		// Expression bodies are single-statement. Script mode accepts multiple top-level
		// statements without error; promote that to a parse failure so UNO2006 can fire.
		if (!hasErrors)
		{
			var root = tree.GetRoot();
			var globalStatements = root.ChildNodes().OfType<GlobalStatementSyntax>().ToList();
			if (globalStatements.Count > 1)
			{
				hasErrors = true;
			}
		}

		return (tree, hasErrors);
	}
}
