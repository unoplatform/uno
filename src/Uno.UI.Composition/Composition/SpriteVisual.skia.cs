#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

using Color = global::Windows.UI.Color;

namespace Microsoft.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		internal override void Paint(in PaintingSession session) => Brush?.Paint(session.Canvas, session.Opacity, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));

		internal override bool CanPaint() => Brush?.CanPaint() ?? false;
		internal override bool RequiresRepaintOnEveryFrame => Brush?.RequiresRepaintOnEveryFrame ?? false;
	}
}
