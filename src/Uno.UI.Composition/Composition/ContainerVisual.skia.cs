#nullable enable
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ContainerVisual : Visual
{
	private List<Visual>? _childrenInRenderOrder;
	private bool _hasCustomRenderOrder;

	internal bool IsChildrenRenderOrderDirty { get; set; }

	partial void InitializePartial()
	{
		Children.CollectionChanged += (s, e) => IsChildrenRenderOrderDirty = true;
	}

	internal List<Visual> GetChildrenInRenderOrder()
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
		if (Children.Any(c => c.ZIndex != 0))
		{
			_childrenInRenderOrder ??= new List<Visual>();
			// We need to sort children in ZIndex order
			foreach (var child in Children.OrderBy(c => c.ZIndex))
			{
				_childrenInRenderOrder.Add(child);
			}
			_hasCustomRenderOrder = true;
		}
		IsChildrenRenderOrderDirty = false;
	}

	internal override void Draw(in DrawingSession session, SKMatrix initialTransform)
	{
		base.Draw(in session, initialTransform);

		var children = GetChildrenInRenderOrder();
		var childrenCount = children.Count;
		for (var i = 0; i < childrenCount; i++)
		{
			children[i].Render(in session, initialTransform);
		}
	}

	protected internal override bool SetMatrixDirty()
	{
		if (base.SetMatrixDirty())
		{
			foreach (var child in Children)
			{
				child.SetMatrixDirty();
			}

			return true;
		}

		return false;
	}
}
