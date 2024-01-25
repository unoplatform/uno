using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public abstract class SkiaVisual(Compositor compositor) : Visual(compositor)
{
	internal override void Draw(in DrawingSession session)
	{
		Invalidate(session.Surface.Canvas);
	}

	protected abstract void Invalidate(SKCanvas canvas);
}
