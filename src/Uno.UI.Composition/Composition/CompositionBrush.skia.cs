#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		internal virtual void Paint(SKCanvas canvas, float opacity, SKRect bounds) { }

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;

		/// <summary>
		/// How far beyond the painted bounds (in local/logical pixels) this brush samples the canvas
		/// — e.g. a backdrop blur reads neighbouring pixels. Used by dirty-rectangles rendering to
		/// expand the damage region so the sampled area is repainted; otherwise the effect would read
		/// stale pixels. 0 for brushes that only paint within their bounds.
		/// </summary>
		internal virtual float DirtyRegionSamplingMargin => 0;
	}
}
