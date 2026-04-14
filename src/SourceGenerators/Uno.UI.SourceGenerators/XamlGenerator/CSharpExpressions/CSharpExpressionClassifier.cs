#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Lexical classifier: decides whether a XAML attribute value carries a C# expression
/// or a conventional markup extension / literal. Phase 2 surfaces only the opt-in
/// and WinAppSDK gates (UNO2020, UNO2099). Full classification lands in Phase 3 (T029).
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
		if (string.IsNullOrEmpty(rawText))
		{
			return ClassificationOutcome.FallThrough;
		}

		var trimmed = rawText!.Trim();
		var isOptInDirective = IsOptInDirective(trimmed);

		if (!isOptInDirective)
		{
			// Phase 2: only the opt-in directive forms are actively classified.
			// Phase 3 (T029) will extend this to bare-identifier, dotted-path, compound,
			// interpolation, and event-lambda forms.
			return ClassificationOutcome.FallThrough;
		}

		var location = CreateLocation(filePath, lineNumber, linePosition, rawText);

		if (!isFeatureEnabled)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				Diagnostics.OptInDirectiveWhenDisabled,
				location,
				attributeName));

			return ClassificationOutcome.HandledWithDiagnostic;
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

		// Phase 2 scaffold: we recognise the directive so the XAML reader has already
		// stopped treating it as a markup extension. Full parse/resolve/analyse/lower
		// lands in Phase 3 (T029..T041). Return "recognised" so the caller does not
		// fall through into literal-value assignment with the raw "{= ...}" string.
		return ClassificationOutcome.RecognisedPendingImplementation;
	}

	/// <summary>
	/// Detects the unambiguous opt-in directive forms: <c>{= ...}</c>, <c>{.Member}</c>,
	/// <c>{this.Member}</c>. These are never valid markup extensions, so the
	/// opt-in-off diagnostic (UNO2020) is safe to emit without further parsing.
	/// </summary>
	private static bool IsOptInDirective(string text)
	{
		if (text.Length < 3 || text[0] != '{' || text[text.Length - 1] != '}')
		{
			return false;
		}

		var inner = text.Substring(1, text.Length - 2);
		if (inner.Length == 0)
		{
			return false;
		}

		if (inner[0] == '=')
		{
			return true;
		}

		if (inner[0] == '.' && inner.Length > 1 && IsIdentifierStart(inner[1]))
		{
			return true;
		}

		const string ThisPrefix = "this.";
		if (inner.Length > ThisPrefix.Length
			&& inner.StartsWith(ThisPrefix, StringComparison.Ordinal)
			&& IsIdentifierStart(inner[ThisPrefix.Length]))
		{
			return true;
		}

		return false;
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
	/// Classifier recognised the attribute as a C# expression directive but lowering is not yet
	/// implemented (Phase 2 scaffold). The caller must skip the attribute to avoid mis-assigning
	/// the raw directive text as a literal value. Phase 3 replaces this outcome with full lowering.
	/// </summary>
	RecognisedPendingImplementation,
}
