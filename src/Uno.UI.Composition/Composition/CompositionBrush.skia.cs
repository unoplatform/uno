#nullable enable

using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		internal virtual void Paint(SKCanvas canvas, float opacity, SKRect bounds) { }

		/// <summary>
		/// Paints this brush onto a backend-neutral <see cref="IDrawingSession"/>. Returns false when this
		/// brush hasn't been migrated off the <see cref="Paint(SKCanvas, float, SKRect)"/> path yet, so callers
		/// fall back to it. Transitional hook while the pipeline moves off direct SkiaSharp access.
		/// </summary>
		internal virtual bool TryPaint(IDrawingSession session, float opacity, Rect bounds) => false;

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;
	}
}
