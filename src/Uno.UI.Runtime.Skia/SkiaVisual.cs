using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

/// <summary>
/// A Visual that allows users to directly draw on the Skia Canvas used by Uno to render a window.
/// </summary>
public abstract class SkiaVisual(Compositor compositor) : Visual(compositor)
{
	internal override void Draw(in DrawingSession session) => RenderOverride(session.Canvas);

	/// <summary>
	/// Queue a rendering cycle that will call <see cref="RenderOverride"/>.
	/// </summary>
	public void Invalidate() => Compositor.InvalidateRender(this);

	/// <summary>
	/// The SkiaSharp drawing logic goes here.
	/// </summary>
	protected abstract void RenderOverride(SKCanvas canvas);
}
