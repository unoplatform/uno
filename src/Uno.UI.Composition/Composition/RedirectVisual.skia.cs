#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override void Draw(in DrawingSession session, in SKMatrix initialTransform)
		{
			base.Draw(in session, initialTransform);
			Source?.Draw(session, initialTransform);
		}
	}
}
