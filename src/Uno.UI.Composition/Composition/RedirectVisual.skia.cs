#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override SKPath? Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Source is not null && session.Canvas is { } canvas)
			{
				Source.RenderRootVisual(canvas, null);
			}

			return null;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
		internal override bool RequiresRepaintOnEveryFrame => true;
	}
}
