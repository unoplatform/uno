#nullable enable

using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

/// <summary>
/// A live, resolution-independent composition surface backed by a parsed <see cref="ISvgDocument"/>: each frame it
/// replays the retained vector into the drawing session at the paint bounds (via <see cref="IPaintableSurface"/>), so
/// SVG scales crisply at any size with no intermediate rasterization. Produced by <c>SvgImageSource</c> and consumed
/// like any other composition surface (e.g. by an <c>Image</c>'s surface brush).
/// </summary>
internal sealed class CompositionSvgSurface : CompositionObject, ICompositionSurface, IPaintableSurface
{
	private readonly ISvgDocument _document;

	public CompositionSvgSurface(ISvgDocument document) => _document = document;

	Vector2 IPaintableSurface.Size
	{
		get
		{
			var size = _document.SourceSize;
			return new Vector2((float)size.Width, (float)size.Height);
		}
	}

	void IPaintableSurface.Paint(IDrawingSession session, float opacity, Rect bounds)
		=> _document.Render(session, new Size(bounds.Width, bounds.Height));

	private protected override void DisposeInternal()
	{
		_document.Dispose();
		base.DisposeInternal();
	}
}
