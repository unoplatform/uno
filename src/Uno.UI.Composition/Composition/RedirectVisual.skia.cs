#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override void Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Source is not null && session.Canvas is not null)
			{
				Source.RenderRootVisual(session.Surface, default, null);
			}
		}
	}
}
