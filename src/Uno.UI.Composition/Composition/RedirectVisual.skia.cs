#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override void Draw(in DrawingSession session)
		{
			base.Draw(in session);
			Source?.Draw(session);
		}
	}
}
