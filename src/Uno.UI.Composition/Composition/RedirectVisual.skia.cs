#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override SKPath? Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Source is not null && session.Canvas is { } canvas)
			{
				Source.RenderRootVisual(canvas, null);
			}

			return null;
		}

		// What gets painted here is the Source's whole subtree, and that subtree paints even when the Source
		// visual itself doesn't (an element visual leaves the painting to its children).
		internal override bool CanPaint() => Source is not null;

		internal override bool TryGetLocalContentBounds(out SKRect localBounds)
		{
			localBounds = SKRect.Empty;

			if (Source is not { } source)
			{
				return true;
			}

			if (!source.TryGetSubtreeContentBoundsInRoot(out var content))
			{
				return false;
			}

			// Paint undoes the source parent's transform (see Visual.RenderRootVisual), so what lands in
			// this visual's local coordinates is the source subtree measured in its parent's space.
			if (!content.IsEmpty && source.Parent is { } parent)
			{
				if (!Matrix4x4.Invert(parent.TotalMatrix, out var invertedParentTotalMatrix))
				{
					return false;
				}

				content = invertedParentTotalMatrix.ToSKMatrix().MapRect(content);
			}

			if (ShadowState is not null)
			{
				return TryGetShadowSilhouetteBounds(content, out localBounds);
			}

			localBounds = content;
			return true;
		}

		internal override bool RequiresRepaintOnEveryFrame => true;
	}
}
