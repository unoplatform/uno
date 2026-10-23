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
				// A parent that flattens Z has no inverse. Its 2D part maps these bounds back only when Z reaches neither X, Y
				// nor perspective, and the plane has no perspective of its own; otherwise the bounds aren't known.
				var parentTotalMatrix = parent.TotalMatrix;
				Matrix3x2 parentToLocal;
				if (Matrix4x4.Invert(parentTotalMatrix, out var invertedParentTotalMatrix))
				{
					parentToLocal = invertedParentTotalMatrix.ToMatrix3x2();
				}
				else if (parentTotalMatrix is not { M31: 0, M32: 0, M34: 0 }
					|| !parentTotalMatrix.IsPlanarAffine()
					|| !Matrix3x2.Invert(parentTotalMatrix.ToMatrix3x2(), out parentToLocal))
				{
					return false;
				}

				content = content.Transform(parentToLocal);
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
