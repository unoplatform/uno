#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents;

[Flags]
internal enum TextGeometryPositionKind
{
	None = 0,
	Text = 1 << 0,
	Caret = 1 << 1,
	FinalEndOfParagraph = 1 << 2,
	InlineObject = 1 << 3,
	StructuredMath = 1 << 4,
	RightToLeft = 1 << 5,
	LeadingEdge = 1 << 6,
	TrailingEdge = 1 << 7,
}

internal readonly record struct TextGeometryPositionInfo(
	Rect CharacterRect,
	Rect CaretRect,
	TextGeometryPositionKind Kind);

internal readonly record struct TextVisualLineInfo(
	int Start,
	int Length,
	int LineIndex,
	Rect Bounds,
	double Baseline,
	bool IsFirst,
	bool IsLast);
