#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// A XAML attribute value identified by the classifier as carrying a C# expression
/// rather than a literal or a conventional markup extension. See data-model.md §1.
/// </summary>
internal sealed record XamlExpressionAttributeValue(
	string RawText,
	ExpressionKind Kind,
	string InnerCSharp,
	LinePositionSpan SourceSpan,
	bool IsCData);

internal enum ExpressionKind
{
	SimpleIdentifier,
	DottedPath,
	Compound,
	EventLambda,
	Explicit,
	ForcedThis,
	ForcedDataType,
	MarkupExtension,
}
