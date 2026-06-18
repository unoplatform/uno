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
		/// How far (in local pixels) beyond the painted bounds this brush samples the surface — e.g. a backdrop
		/// blur reads neighbouring pixels. Damage-region rendering expands the damage region by this margin
		/// so the sampled area stays fresh; otherwise the effect would read stale pixels. 0 by default.
		/// </summary>
		internal virtual float DamageRegionSamplingMargin => 0;
	}
}
