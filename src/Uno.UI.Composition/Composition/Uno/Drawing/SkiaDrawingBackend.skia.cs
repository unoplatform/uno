#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IDrawingBackend"/>, backed by SkiaSharp.</summary>
internal sealed class SkiaDrawingBackend : IDrawingBackend
{
	public IPathBuilder CreatePathBuilder() => new SkiaPathBuilder();
}
