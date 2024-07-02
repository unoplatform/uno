#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Composition;

public partial class ContainerVisual : Visual
{
	private List<Visual>? _childrenInRenderOrder;
	private bool _hasCustomRenderOrder;

	internal bool IsChildrenRenderOrderDirty { get; set; }

	partial void InitializePartial()
	{
		Children.CollectionChanged += (s, e) => IsChildrenRenderOrderDirty = true;
	}

	private protected override IList<Visual> GetChildrenInRenderOrder()
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
