#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;


namespace Microsoft.UI.Composition;

public partial class ContainerVisual : Visual
{
	private List<Visual>? _childrenInRenderOrder;
	private bool _hasCustomRenderOrder;
	private int? _subtreeVisualCount;

	private (Rect rect, bool isAncestorClip)? _layoutClip;

	private GCHandle _gcHandle;

	partial void InitializePartial()
	{
		Children.CollectionChanged += (s, e) =>
		{
			IsChildrenRenderOrderDirty = true;

			var parent = this;
			while (parent is not null && parent._subtreeVisualCount is not null)
			{
				parent._subtreeVisualCount = null;
				parent = parent.Parent;
			}

			InvalidateParentChildrenPicture(true);
			// A child added/removed changes this container's own silhouette too.
			InvalidateParentShadowCaches(includeSelf: true);

			// We need to force a redraw because at this point it's not necessarily true that
			// a visual in the composition tree was changed, only that it was added/removed,
			// so it's possible that no InvalidatePaint() calls were fired in response to this change, 
			// so we need to force a new frame even though no paint invalidations happened just so that
			// already-clean added/removed visuals are reflected in the UI
			CompositionTarget?.RequestNewFrame();

			if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Reset
				&& e.OldItems is not null)
			{
				foreach (var i in e.OldItems)
				{
					if (i is CompositionObject compositionObject)
					{
						compositionObject.StopAllAnimations();
					}
					// A removed visual won't be visited next frame; damage its (and its descendants') old area so
					// the partial-repaint path clears where it used to be.
					if (CompositionTarget is { } removalTarget && i is Visual removedVisual)
					{
						removedVisual.ContributeRemovalDamage(removalTarget);
					}
				}
			}
		};

		_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		Handle = GCHandle.ToIntPtr(_gcHandle);
	}

	internal IntPtr Handle { get; private set; }

	internal WeakReference? Owner { get; set; }

	internal string? OwnerDebugName => Owner?.Target?.GetType().Name;

	/// <summary>
	/// Layout clipping is usually applied in the element's coordinate space.
	/// However, for Panels and ScrollViewer headers specifically, WinUI applies clipping in the parent's coordinate space.
	/// So, isAncestorClip will be set to true for Panels and ScrollViewer headers, indicating that clipping is in parent's coordinate space.
	/// </summary>
	internal (Rect rect, bool isAncestorClip)? LayoutClip
	{
		get => _layoutClip;
		set => SetObjectProperty(ref _layoutClip, value);
	}

	internal bool IsChildrenRenderOrderDirty { get; set; }

	private protected override List<Visual> GetChildrenInRenderOrder()
	{
		if (IsChildrenRenderOrderDirty)
		{
			ResetRenderOrder();
		}

		return !_hasCustomRenderOrder ? Children.InnerList : _childrenInRenderOrder!;
	}

	internal void ResetRenderOrder()
	{
		_childrenInRenderOrder?.Clear();
		_hasCustomRenderOrder = false;
		if (Children.InnerList.Any(c => c.ZIndex != 0))
		{
			_childrenInRenderOrder ??= new List<Visual>();
			// We need to sort children in ZIndex order
			foreach (var child in Children.InnerList.OrderBy(c => c.ZIndex))
			{
				_childrenInRenderOrder.Add(child);
			}
			_hasCustomRenderOrder = true;
		}
		IsChildrenRenderOrderDirty = false;
	}

	/// <remarks>This does NOT take the clipping into account.</remarks>
	internal virtual bool HitTest(Point relativeLocation) => new Rect(0, 0, Size.X, Size.Y).Contains(relativeLocation);

	internal Rect? GetArrangeClipPathInElementCoordinateSpace() => GetArrangeClip(out _, out _);

	/// <summary>
	/// The arrange clip in this visual's coordinates. When an ancestor clip is mapped here by something that is
	/// not axis aligned, the returned rect is only the BOUNDING BOX of the real clip: <paramref name="mapping"/>
	/// is then the transform and <paramref name="source"/> the rect it applies to, so a caller that can express a
	/// shape maps those itself rather than using the box.
	/// </summary>
	private Rect? GetArrangeClip(out Rect source, out Matrix3x2? mapping)
	{
		source = default;
		mapping = null;
		if (LayoutClip is not { isAncestorClip: var isAncestorClip, rect: var rect })
		{
			return default;
		}

		source = rect;
		if (isAncestorClip)
		{
			Matrix4x4.Invert(TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = (Parent?.TotalMatrix ?? Matrix4x4.Identity) * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{
				var matrix = childToParentTransform.ToMatrix3x2();
				if (matrix.M12 != 0 || matrix.M21 != 0)
				{
					mapping = matrix;
				}

				rect = rect.Transform(matrix);
			}
		}

		return rect;
	}

	/// <summary>The arrange clip as a shape, which a rotated ancestor clip needs: its bounding box would let
	/// roughly the corners through.</summary>
	private IGeometry CreateArrangeClipGeometry(Rect rect, Rect source, Matrix3x2? mapping)
	{
		if (mapping is not { } matrix)
		{
			return GeometryFactory.Current.CreateRectangleGeometry(rect);
		}

		// From the SOURCE rect, mapped once. Re-deriving it from the bounding box would inflate it again by the
		// same factor the box already cost.
		var localGeometry = GeometryFactory.Current.CreateRectangleGeometry(source);
		var transformed = localGeometry.Transform(matrix);
		localGeometry.Release();
		return transformed;
	}

	internal override void ApplyPrePaintingClipping(IDrawingSession session)
	{
		base.ApplyPrePaintingClipping(session);
		if (GetArrangeClip(out var source, out var mapping) is { } rect)
		{
			if (mapping is null)
			{
				session.ClipRect(rect);
			}
			else
			{
				var clip = CreateArrangeClipGeometry(rect, source, mapping);
				session.ClipPath(clip);
				clip.Release();
			}
		}
	}

	private protected override Rect? GetLocalCullClipBounds()
	{
		var baseBounds = base.GetLocalCullClipBounds();
		if (GetArrangeClipPathInElementCoordinateSpace() is not { } arrangeRect)
		{
			return baseBounds;
		}

		return baseBounds is { } b ? Intersect(b, arrangeRect) : arrangeRect;
	}

	internal override IGeometry? GetPrePaintingClipping()
	{
		var baseClip = base.GetPrePaintingClipping();
		if (GetArrangeClip(out var arrangeSource, out var arrangeMapping) is not { } rect)
		{
			return baseClip;
		}

		var arrangeClip = CreateArrangeClipGeometry(rect, arrangeSource, arrangeMapping);
		return baseClip is null
			? arrangeClip
			: IntersectOwned(baseClip, arrangeClip);
	}

	internal override bool SetMatrixDirtyFromAncestor()
	{
		if (base.SetMatrixDirtyFromAncestor())
		{
			// We use InnerList to avoid boxing the enumerator.
			// Currently, VisualCollection.GetEnumerator returns IEnumerator<Visual> instead of a concrete struct type to match WinUI API surface.
			foreach (var child in Children.InnerList)
			{
				child.SetMatrixDirtyFromAncestor();
			}

			return true;
		}

		return false;
	}

	internal override int GetSubTreeVisualCount()
	{
		if (_subtreeVisualCount is { } count)
		{
			return count;
		}
		var acc = 0;
		foreach (var visual in Children.InnerList)
		{
			acc += visual.GetSubTreeVisualCount();
		}
		_subtreeVisualCount ??= Children.Count + acc;

		return _subtreeVisualCount.Value;
	}
}
