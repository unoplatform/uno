#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		/// <summary>
		/// Only used when <see cref="SupportsRender"/> is true and is not
		/// supported by all brushes.
		/// </summary>
		internal virtual void Render(SKCanvas canvas, SKRect bounds) { }

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;
	}
}
