#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents;

internal static class ParsedTextExtensions
{
	// The centres of the first and last lines. A Y clamped between them hits the nearest line by X, where GetIndexAt
	// sends a point below the text to its end. GetRectForIndex clamps its index, so int.MaxValue yields the last
	// line's rect; Math.Max covers layouts whose rects are not ordered by line (structured math).
	internal static (double Min, double Max) GetLineCentreRange(this IParsedText parsedText)
	{
		var first = parsedText.GetRectForIndex(0).GetMidY();
		return (first, Math.Max(first, parsedText.GetRectForIndex(int.MaxValue).GetMidY()));
	}

	internal static double ClampYToLines(this IParsedText parsedText, double y)
	{
		var (min, max) = parsedText.GetLineCentreRange();
		return Math.Clamp(y, min, max);
	}
}
