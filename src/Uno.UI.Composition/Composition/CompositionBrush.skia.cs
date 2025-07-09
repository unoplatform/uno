#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		/// <summary>
		/// Must be overriden by all supported brushes even if <see cref="SupportsRender"/> is true.
		/// </summary>
		internal virtual void UpdatePaint(SKPaint paint, SKRect bounds)
		{
		}

		/// <summary>
		/// Only used when <see cref="SupportsRender"/> is true and is not
		/// supported by all brushes.
		/// </summary>
		internal virtual void Render(SKCanvas canvas, SKRect bounds) { }

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;

		/// <summary>
		/// Uses <see cref="Render"/> during the render cycle instead of using <see cref="UpdatePaint"/>
		/// to fill an SKPaint with what needs to be drawn ahead of time
		/// and then using that SKPaint later during the render cycle.
		/// This cannot be true for all brushes since some brushes don't draw their own content but
		/// are effectively arbitrary shaders that read pixels and map them to new values
		/// </summary>
		internal virtual bool SupportsRender => false;
	}
}
