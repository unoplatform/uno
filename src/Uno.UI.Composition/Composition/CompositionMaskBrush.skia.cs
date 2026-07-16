#nullable enable

using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionMaskBrush : CompositionBrush
	{
		internal override bool RequiresRepaintOnEveryFrame => Source is not null && Mask is not null && (Source.RequiresRepaintOnEveryFrame || Mask.RequiresRepaintOnEveryFrame);

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (Source is null || Mask is null)
			{
				return true;
			}

			// The first SaveLayer + Clear creates a clean offscreen surface for the source, without having to
			// manage an SKSurface ourselves. The second layer with DstIn keeps only the source pixels covered
			// by the mask's alpha, producing the masking effect. Layer paints are opaque (alpha only modulates).
			session.SaveLayer(antialias: true);
			session.ClipRect(bounds, antialias: true);
			session.Clear(global::Windows.UI.Colors.Transparent);
			Source.TryPaint(session, opacity, bounds);
			session.SaveLayer(antialias: true, blendMode: BlendMode.DstIn);
			Mask.TryPaint(session, opacity, bounds);
			session.Restore();
			session.Restore();
			return true;
		}

		internal override bool CanPaint() => (Source?.CanPaint() ?? false) || (Mask?.CanPaint() ?? false);
	}
}
