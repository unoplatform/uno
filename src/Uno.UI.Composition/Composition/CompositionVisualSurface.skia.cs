#nullable enable

using System;
using System.Numerics;
using Uno.Disposables;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface, IPaintableSurface
	{
		void IPaintableSurface.Paint(global::Uno.UI.Composition.Drawing.IDrawingSession session, float opacity)
		{
			if (SourceVisual is not null)
			{
				int save = session.Save();
				// Note that this is applied before the SourceOffset translates the canvas' matrix, so
				var size = (this as IPaintableSurface).Size;
				session.ClipRect(new global::Windows.Foundation.Rect(0, 0, size.X, size.X), antialias: true);

				SourceVisual.RenderRootVisual(session, SourceOffset);
				session.RestoreToCount(save);
			}
		}

		Vector2 IPaintableSurface.Size => SourceSize switch
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
