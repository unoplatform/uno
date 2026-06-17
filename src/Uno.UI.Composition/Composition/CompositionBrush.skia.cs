#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		internal virtual void Paint(SKCanvas canvas, float opacity, SKRect bounds) { }

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;
	}
}
