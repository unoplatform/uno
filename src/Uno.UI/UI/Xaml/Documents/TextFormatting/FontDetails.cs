#nullable enable

using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

// The text layer talks only to the neutral <see cref="IFont"/> handle: it shapes runs (<see cref="IFont.Shape"/>),
// serves metrics/coverage, and turns glyphs into drawables. The shaper (HarfBuzz) is an implementation detail of the
// handle, so nothing here references a Skia or HarfBuzz type. Font resolution (family/style → handle) is owned by the
// backend's <see cref="IFontProvider"/>.
internal record FontDetails(IFont FontHandle, float FontSize, float FontScaleX)
{
	internal float LineHeight => FontHandle.Descent - FontHandle.Ascent;

	// CCompositeFontFamily::GetTextLineBoundsMetrics — returns the baseline and line spacing for this
	// font constrained by TextLineBounds. Full yields the unconstrained metrics.
	internal (float baseline, float lineSpacing) GetTextLineBoundsMetrics(TextLineBounds textLineBounds)
	{
		var baseline = -FontHandle.Ascent;
		var lineSpacing = LineHeight;
		var capHeight = FontHandle.CapHeight;

		return textLineBounds switch
		{
			TextLineBounds.TrimToCapHeight => (capHeight, lineSpacing - baseline + capHeight),
			TextLineBounds.TrimToBaseline => (baseline, baseline),
			TextLineBounds.Tight => (capHeight, capHeight),
			_ => (baseline, lineSpacing),
		};
	}

	internal static FontDetails Create(IFont fontHandle, float fontSize) => new(fontHandle, fontSize, 1.0f);
}
