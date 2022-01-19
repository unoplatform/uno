#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Composition;

public partial class ContainerVisual : Visual
{
	private List<Visual> _childrenInRenderOrder = new List<Visual>();
	private bool _hasCustomRenderOrder = false;

	internal bool IsChildrenRenderOrderDirty { get; set; }

	internal IEnumerable<Visual> GetChildrenInRenderOrder()
	{
		if (IsChildrenRenderOrderDirty)
		{
			ResetRenderOrder();
		}

		return !_hasCustomRenderOrder ? Children : _childrenInRenderOrder;
	}

	internal void ResetRenderOrder()
	{
		_childrenInRenderOrder.Clear();
		_hasCustomRenderOrder = false;
		if (Children.Any(c => c.ZIndex != 0))
		{
			// We need to sort children in ZIndex order
			foreach (var child in Children.OrderBy(c => c.ZIndex))
			{
				_childrenInRenderOrder.Add(child);
			}
			_hasCustomRenderOrder = true;
		}
		IsChildrenRenderOrderDirty = false;
	}
}
