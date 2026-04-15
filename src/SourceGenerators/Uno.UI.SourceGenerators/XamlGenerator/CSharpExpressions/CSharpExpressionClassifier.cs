#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Lexical classifier: decides whether a XAML attribute value carries a C# expression
/// or a conventional markup extension / literal. Handles opt-in / WinAppSDK gating and
/// surfaces the full set of Phase 3 forms (directives, compound, interpolation, lambda)
/// for the generator hook to lower.
/// </summary>
internal static class CSharpExpressionClassifier
{
	/// <summary>
	/// Pre-check invoked from <c>XamlFileGenerator</c> before the markup-extension branch.
	/// Returns <c>true</c> when the classifier has fully handled the member (diagnostic emitted,
	/// no further processing required). Returns <c>false</c> when the member should fall
	/// through to the existing markup-extension pipeline.
	/// </summary>
	public static ClassificationOutcome TryClassify(
		string? rawText,
		string attributeName,
		string filePath,
		int lineNumber,
		int linePosition,
		bool isFeatureEnabled,
		bool isWinAppSdk,
		GeneratorExecutionContext context)
	{
		var kind = Classify(rawText);
		if (kind is null)
		{
			return ClassificationOutcome.FallThrough;
		}

		var location = CreateLocation(filePath, lineNumber, linePosition, rawText!);
		var isDirective = kind is ExpressionKind.Explicit
			or ExpressionKind.ForcedDataType
			or ExpressionKind.ForcedThis;

		if (!isFeatureEnabled)
		{
			// Only diagnose unambiguous directive forms when the feature is off.
			// Non-directive shapes (compound, interpolation, lambda) reach this hook
			// only because the XAML reader's T035a carve-out bypassed markup-extension
			// parsing for them; staying silent here preserves opt-in semantics.
			if (isDirective)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.OptInDirectiveWhenDisabled,
					location,
					attributeName));

				return ClassificationOutcome.HandledWithDiagnostic;
			}

			return ClassificationOutcome.FallThrough;
		}

		if (isWinAppSdk)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				Diagnostics.CSharpExpressionsNotSupportedOnWinAppSDK,
				location,
				filePath,
				attributeName));

			return ClassificationOutcome.HandledWithDiagnostic;
		}

		return ClassificationOutcome.RecognisedPendingImplementation;
	}

	/// <summary>
	/// Pure classification entry point: maps a raw XAML attribute value to an
	/// <see cref="ExpressionKind"/>, or <c>null</c> when the value is not a C# expression
	/// (literal text or a conventional markup extension that must fall through to the
	/// existing pipeline). Has no side effects and does not require a generator context;
	/// designed to be unit-tested in isolation.
	/// </summary>
	public static ExpressionKind? Classify(string? rawText)
	{
		if (string.IsNullOrEmpty(rawText))
		{
			return null;
		}

		var trimmed = rawText!.Trim();
		if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
		{
			return null;
		}

		var inner = trimmed.Substring(1, trimmed.Length - 2);
		if (inner.Length == 0)
		{
			return null;
		}

		if (inner[0] == '=')
		{
			return ExpressionKind.Explicit;
		}

		// `.Member` directive (ForcedDataType) and `this.Member` directive (ForcedThis) are
		// syntactic sugar for simple-path forms. When the body contains a compound marker
		// (operators, parens, etc.) the attribute is a compound expression that just happens
		// to start with a directive-looking prefix — classify as Compound so lowering lands
		// on the helper-method path.
		var innerIsCompound = ContainsCompoundMarker(inner);

		if (inner[0] == '.' && inner.Length > 1 && IsIdentifierStart(inner[1]))
		{
			return innerIsCompound ? ExpressionKind.Compound : ExpressionKind.ForcedDataType;
		}

		const string ThisPrefix = "this.";
		if (inner.Length > ThisPrefix.Length
			&& inner.StartsWith(ThisPrefix, StringComparison.Ordinal)
			&& IsIdentifierStart(inner[ThisPrefix.Length]))
		{
			return innerIsCompound ? ExpressionKind.Compound : ExpressionKind.ForcedThis;
		}

		if (LooksLikeMarkupExtension(inner))
		{
			return null;
		}

		return innerIsCompound
			? ExpressionKind.Compound
			: (inner.IndexOf('.') >= 0 ? ExpressionKind.DottedPath : ExpressionKind.SimpleIdentifier);
	}

	/// <summary>
	/// Extracts the inner C# expression text from a classified XAML attribute value,
	/// stripping the outer braces and any directive prefix (<c>=</c>, <c>.</c>, or
	/// <c>this.</c>). Returns <c>null</c> when <paramref name="rawText"/> isn't a
	/// braced C# expression form.
	/// </summary>
	public static string? ExtractInnerCSharp(string? rawText, ExpressionKind kind)
	{
		if (string.IsNullOrEmpty(rawText))
		{
			return null;
		}

		var trimmed = rawText!.Trim();
		if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
		{
			return null;
		}

		var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();

		return kind switch
		{
			ExpressionKind.Explicit => inner.Length > 1 ? inner.Substring(1).TrimStart() : string.Empty,
			ExpressionKind.ForcedDataType => inner.Length > 1 ? inner.Substring(1) : string.Empty,
			_ => inner,
		};
	}

	private static bool LooksLikeMarkupExtension(string inner)
	{
		// prefix:Name form at start — e.g. x:Bind, local:FooExtension. Always markup ext.
		if (TryReadIdentifier(inner, 0, out var prefixEnd)
			&& prefixEnd < inner.Length
			&& inner[prefixEnd] == ':'
			&& prefixEnd + 1 < inner.Length
			&& IsIdentifierStart(inner[prefixEnd + 1]))
		{
			return true;
		}

		// Leading identifier that matches a known markup extension name, followed by
		// whitespace or end-of-content.
		if (TryReadIdentifier(inner, 0, out var idEnd))
		{
			var name = inner.Substring(0, idEnd);
			if (IsKnownMarkupExtensionName(name)
				&& (idEnd == inner.Length || char.IsWhiteSpace(inner[idEnd])))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsKnownMarkupExtensionName(string name)
	{
		switch (name)
		{
			case "Binding":
			case "TemplateBinding":
			case "StaticResource":
			case "ThemeResource":
			case "RelativeSource":
			case "CustomResource":
				return true;
			default:
				return false;
		}
	}

	private static ExpressionKind ClassifyInner(string inner)
	{
		// Compound markers outside string literals.
		if (ContainsCompoundMarker(inner))
		{
			return ExpressionKind.Compound;
		}

		// No operators / parens / interpolation — must be a (possibly dotted) path.
		return inner.IndexOf('.') >= 0
			? ExpressionKind.DottedPath
			: ExpressionKind.SimpleIdentifier;
	}

	private static bool ContainsCompoundMarker(string inner)
	{
		var i = 0;
		var len = inner.Length;

		while (i < len)
		{
			var c = inner[i];

			if (c == '$' && i + 1 < len && inner[i + 1] == '\'')
			{
				// Interpolated string literal — compound by definition.
				return true;
			}

			if (c == '\'')
			{
				SkipSingleQuoted(inner, ref i);
				continue;
			}

			switch (c)
			{
				case '+':
				case '-':
				case '*':
				case '/':
				case '%':
				case '<':
				case '>':
				case '=':
				case '!':
				case '&':
				case '|':
				case '^':
				case '~':
				case '?':
				case '(':
				case ',':
					return true;
			}

			i++;
		}

		return false;
	}

	private static void SkipSingleQuoted(string inner, ref int i)
	{
		var len = inner.Length;
		i++;
		while (i < len)
		{
			var c = inner[i];
			if (c == '\\' && i + 1 < len)
			{
				i += 2;
				continue;
			}
			if (c == '\'')
			{
				i++;
				return;
			}
			i++;
		}
	}

	private static bool TryReadIdentifier(string s, int start, out int end)
	{
		end = start;
		if (start >= s.Length || !IsIdentifierStart(s[start]))
		{
			return false;
		}
		end = start + 1;
		while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_'))
		{
			end++;
		}
		return true;
	}

	private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

	private static Location CreateLocation(string filePath, int lineNumber, int linePosition, string rawText)
	{
		// XAML is 1-based in the parser; Roslyn LinePosition is 0-based.
		var line = Math.Max(0, lineNumber - 1);
		var column = Math.Max(0, linePosition - 1);
		var start = new LinePosition(line, column);
		var end = new LinePosition(line, column + rawText.Length);
		return Location.Create(filePath, new TextSpan(0, rawText.Length), new LinePositionSpan(start, end));
	}
}

/// <summary>
/// Outcome of <see cref="CSharpExpressionClassifier.TryClassify"/>.
/// </summary>
internal enum ClassificationOutcome
{
	/// <summary>Classifier did not own this attribute; fall through to the existing pipeline.</summary>
	FallThrough,

	/// <summary>Classifier emitted a diagnostic and consumed the attribute; skip further processing.</summary>
	HandledWithDiagnostic,

	/// <summary>
	/// Classifier recognised the attribute as a C# expression form. The caller must now parse,
	/// analyse, lower and emit the expression, or emit a follow-up diagnostic if the form is
	/// not yet supported.
	/// </summary>
	RecognisedPendingImplementation,
}
