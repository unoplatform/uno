#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using SkiaSharp;

namespace Microsoft.UI.Composition;

public partial class ContainerVisual : Visual
{
	private List<Visual>? _childrenInRenderOrder;
	private bool _hasCustomRenderOrder;

	private (Rect rect, bool isAncestorClip)? _layoutClip;

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

	partial void InitializePartial()
	{
		Children.CollectionChanged += (s, e) => IsChildrenRenderOrderDirty = true;
	}

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
	internal virtual bool HitTest(Point point) => new Rect(0, 0, Size.X, Size.Y).Contains(point);

	/// <returns>true if a ViewBox exists</returns>
	internal bool GetArrangeClipPathInElementCoordinateSpace(SKPath dst)
	{
		if (LayoutClip is not { isAncestorClip: var isAncestorClip, rect: var rect })
		{
			return false;
		}

		dst.Rewind();
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

	private protected override void ApplyPrePaintingClipping(in SKCanvas canvas)
	{
		base.ApplyPrePaintingClipping(in canvas);
		using (SkiaHelper.GetTempSKPath(out var prePaintingClipPath))
		{
			if (GetArrangeClipPathInElementCoordinateSpace(prePaintingClipPath))
			{
				canvas.ClipPath(prePaintingClipPath, antialias: true);
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
}
