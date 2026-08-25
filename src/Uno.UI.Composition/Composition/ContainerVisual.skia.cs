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

	internal Rect? GetArrangeClipPathInElementCoordinateSpace()
	{
		if (LayoutClip is not { isAncestorClip: var isAncestorClip, rect: var rect })
		{
			return default;
		}

		if (isAncestorClip)
		{
			Matrix4x4.Invert(TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = (Parent?.TotalMatrix ?? Matrix4x4.Identity) * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{
				rect = rect.Transform(childToParentTransform.ToMatrix3x2());
			}
		}

		return rect;
	}

	internal override void ApplyPrePaintingClipping(IDrawingSession session)
	{
		base.ApplyPrePaintingClipping(session);
		if (GetArrangeClipPathInElementCoordinateSpace() is { } rect)
		{
			session.ClipRect(rect, antialias: true);
		}
	}

	internal override IGeometry? GetPrePaintingClipping()
	{
		var baseClip = base.GetPrePaintingClipping();
		if (GetArrangeClipPathInElementCoordinateSpace() is not { } rect)
		{
			return baseClip;
		}

		var arrangeClip = GeometryFactory.Current.CreateRectangleGeometry(rect);
		return baseClip is null
			? arrangeClip
			: baseClip.Combine(arrangeClip, GeometryCombineMode.Intersect);
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
