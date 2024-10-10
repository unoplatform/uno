#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override void Paint(in PaintingSession session)
		{
			base.Paint(in session);
			Source?.Paint(session);
		}
	}
}
