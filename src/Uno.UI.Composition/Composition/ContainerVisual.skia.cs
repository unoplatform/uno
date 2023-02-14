#nullable enable
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UI.Composition;

public partial class ContainerVisual : Visual
{
	private List<Visual>? _childrenInRenderOrder;
	private bool _hasCustomRenderOrder;

	internal bool IsChildrenRenderOrderDirty { get; set; }

	internal IList<Visual> GetChildrenInRenderOrder()
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

	internal override void Render(SKSurface surface)
	{
		var compositor = this.Compositor;
		var children = GetChildrenInRenderOrder();
		var childrenCount = children.Count;
		for (int i = 0; i < childrenCount; i++)
		{
			compositor.RenderVisual(surface, children[i]);
		}
	}

}
