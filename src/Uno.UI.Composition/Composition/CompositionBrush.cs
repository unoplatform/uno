using SkiaSharp;
#nullable enable


namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush : CompositionObject
	{
		internal CompositionBrush()
		{
		}

		internal CompositionBrush(Compositor compositor) : base(compositor)
		{
		}

		internal virtual void Paint(SKCanvas canvas, float opacity, SKRect bounds) { }

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;

		internal virtual float DamageRegionSamplingMargin => 0;
	}
}
