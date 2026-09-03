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

	internal static FontDetails Create(IFont fontHandle, float fontSize) => new(fontHandle, fontSize, 1.0f);
}
