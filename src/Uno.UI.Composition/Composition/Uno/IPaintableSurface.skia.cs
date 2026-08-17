#nullable enable

using System.Numerics;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition
{
	/// <summary>
	/// A composition surface whose content is produced by a per-frame <see cref="Paint"/> callback into the neutral
	/// <see cref="IDrawingSession"/> (as opposed to <see cref="CompositionImageSurface"/>, which is backed by a fixed
	/// texture). A <see cref="CompositionSurfaceBrush"/> over one repaints it every frame, so the content stays live
	/// and resolution-independent. Backend-neutral — nothing SkiaSharp crosses this seam.
	/// </summary>
	internal interface IPaintableSurface
	{
		internal void Paint(IDrawingSession session, float opacity);
		internal Vector2 Size { get; }
	}
}
