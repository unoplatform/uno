#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

[Flags]
internal enum RichEditTextGeometryHitKind
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
	Selection = 1 << 8,
	Unloaded = 1 << 9,
	ClippedAbove = 1 << 10,
	ClippedBelow = 1 << 11,
	ClippedLeft = 1 << 12,
	ClippedRight = 1 << 13,
}

internal readonly record struct RichEditTextGeometryHitResult(
	Rect Rect,
	RichEditTextGeometryHitKind Kind)
{
	// Native WinUI 3 returned zero for text, caret, final EOP, clipping, bidi, selection,
	// inline-object, empty, unloaded, transform, and PointOptions combinations.
	internal int NativeHit => 0;
}
