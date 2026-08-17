#nullable enable

using Uno.UI.Composition.Drawing;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override IGeometry? Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Source is not null)
			{
				Source.RenderRootVisual(session.Session, null);
			}

			return null;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
		internal override bool RequiresRepaintOnEveryFrame => true;
	}
}
