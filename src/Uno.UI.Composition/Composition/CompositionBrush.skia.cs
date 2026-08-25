#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		/// <summary>
		/// Paints this brush onto a backend-neutral <see cref="IDrawingSession"/>. Returns true when the brush
		/// handled the paint (including "nothing to paint"); false only when the brush cannot paint at all.
		/// </summary>
		internal virtual bool TryPaint(IDrawingSession session, float opacity, Rect bounds) => false;

		/// <summary>
		/// Realizes any offscreen content this brush needs (e.g. an effect graph's rasterized sources) BEFORE it is
		/// painted into a caller-owned offscreen. Called by <c>EffectGraphParser.RasterizeSource</c> ahead of opening
		/// its <see cref="IDrawingFactory.RenderOffscreen"/> so a nested effect brush's own offscreen passes run first,
		/// sequentially, rather than re-entering <c>RenderOffscreen</c> while the outer one is still open. Nested
		/// (re-entrant) offscreen rendering is not part of the drawing contract — a backend may hold per-pass scratch
		/// that a nested pass corrupts — so it must be avoided. No-op for brushes that don't rasterize offscreen.
		/// </summary>
		internal virtual void PrepareForOffscreenRasterization(IDrawingFactory factory, Rect bounds) { }

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;

		internal virtual float DamageRegionSamplingMargin => 0;
	}
}
