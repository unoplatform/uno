#nullable enable

using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		internal override void Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Source is not null)
			{
				Source.RenderRootVisual(session.Session, null);
			}
		}

		// What gets painted here is the Source's whole subtree, and that subtree paints even when the Source
		// visual itself doesn't (an element visual leaves the painting to its children).
		internal override bool CanPaint() => Source is not null;

		internal override bool TryGetLocalContentBounds(out Rect localBounds)
		{
			localBounds = default;

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
			if (!IsRectEmpty(content) && source.Parent is { } parent)
			{
				if (!Matrix4x4.Invert(parent.TotalMatrix, out var invertedParentTotalMatrix))
				{
					return false;
				}

				content = content.Transform(invertedParentTotalMatrix.ToMatrix3x2());
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
