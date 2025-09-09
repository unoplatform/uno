#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using SkiaSharp;
using Windows.Foundation;
using Uno.Extensions;

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

			if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Reset
				&& e.OldItems is not null)
			{
				foreach (var i in e.OldItems)
				{
					if (i is CompositionObject compositionObject)
					{
						compositionObject.StopAllAnimations();
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

	/// <returns>true if a ViewBox exists</returns>
	internal bool GetArrangeClipPathInElementCoordinateSpace(SKPath dst) // TODO: Do not use SKPath here, bad for perf and prevents usage for IDirectManipulationHandler.IsInBoundsForResume
	{
		if (LayoutClip is not { isAncestorClip: var isAncestorClip, rect: var rect })
		{
			return false;
		}

		var clipRect = rect.ToSKRect();
		dst.AddRect(clipRect);
		if (isAncestorClip)
		{
			Matrix4x4.Invert(TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = Parent!.TotalMatrix * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{
				dst.Transform(childToParentTransform.ToSKMatrix());
			}
		}

		return true;
	}

	internal Rect? GetArrangeClipPathInElementCoordinateSpace()
	{
		if (LayoutClip is not { isAncestorClip: var isAncestorClip, rect: var rect })
		{
			return default;
		}

		if (isAncestorClip)
		{
			Matrix4x4.Invert(TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = Parent!.TotalMatrix * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{
				rect = rect.Transform(childToParentTransform.ToMatrix3x2());
			}
		}

		return rect;
	}

	private static SKPath _sparePrePaintingClippingPath = new SKPath();

	internal override bool GetPrePaintingClipping(SKPath dst) // TODO: Do not use SKPath here, bad for perf and prevents usage for IDirectManipulationHandler.IsInBoundsForResume
	{
		var prePaintingClipPath = _sparePrePaintingClippingPath;

		prePaintingClipPath.Rewind();

		if (base.GetPrePaintingClipping(dst))
		{
			// TODO: SKPath-less
			//if (GetArrangeClipPathInElementCoordinateSpace() is {} clipping)
			//{
			//	dst.AddRect(clipping.ToSKRect());
			//}

			if (GetArrangeClipPathInElementCoordinateSpace(prePaintingClipPath))
			{
				dst.Op(prePaintingClipPath, SKPathOp.Intersect, dst);
			}

			return true;
		}
		else
		{
			// TODO: SKPath-less
			//if (GetArrangeClipPathInElementCoordinateSpace() is {} clipping)
			//{
			//	dst.Reset();
			//	dst.AddRect(clipping.ToSKRect());

			//	return true;
			//}

			if (GetArrangeClipPathInElementCoordinateSpace(prePaintingClipPath))
			{
				dst.Reset();
				dst.AddPath(prePaintingClipPath);

				return true;
			}
			else
			{
				return false;
			}
		}
	}

	internal override bool SetMatrixDirty()
	{
		if (base.SetMatrixDirty())
		{
			// We use InnerList to avoid boxing the enumerator.
			// Currently, VisualCollection.GetEnumerator returns IEnumerator<Visual> instead of a concrete struct type to match WinUI API surface.
			foreach (var child in Children.InnerList)
			{
				child.SetMatrixDirty();
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
