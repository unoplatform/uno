#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.Disposables;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface, ISkiaSurface
	{
		void ISkiaSurface.Paint(SKCanvas canvas, float opacity)
		{
			if (SourceVisual is not null)
			{
				int save = canvas.Save();
				// Note that this is applied before the SourceOffset translates the canvas' matrix, so
				canvas.ClipRect(new SKRect(0, 0, (this as ISkiaSurface).Size.X, (this as ISkiaSurface).Size.X), antialias: true);

				SourceVisual.RenderRootVisual(canvas, SourceOffset);
				canvas.RestoreToCount(save);
			}
		}

		Vector2 ISkiaSurface.Size => SourceSize switch
		{
			{ X: > 0.0f, Y: > 0.0f } => SourceSize,
			_ => SourceVisual switch
			{
				{ Size: { X: > 0.0f, Y: > 0.0f } } => SourceVisual.Size,
				_ => new Vector2(1000, 1000)
			}
		};
	}
}
