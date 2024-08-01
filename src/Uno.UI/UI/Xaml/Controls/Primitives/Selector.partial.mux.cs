using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	// Selects the next item in the list.
	private protected void SelectNext(ref int index)
	{
		var count = Items.Count;
		if (count > 0)
		{
			int internalSelectedIndex = index + 1;
			if (internalSelectedIndex <= count - 1)
			{
				SelectItemHelper(ref internalSelectedIndex, 1);
				if (internalSelectedIndex != -1)
				{
					index = internalSelectedIndex;
				}
			}
		}
	}

	// Selects the previous item in the list.
	private protected void SelectPrev(ref int index)
	{
		var count = Items.Count;
		if (count > 0)
		{
			int internalSelectedIndex = index - 1;
			if (internalSelectedIndex >= 0)
			{
				SelectItemHelper(ref internalSelectedIndex, -1);
				if (internalSelectedIndex != -1)
				{
					index = internalSelectedIndex;
				}
			}
		}
	}

	// Given a direction, searches through list for next available item to select.
	private void SelectItemHelper(ref int index, int increment)
	{
		var items = Items;
		var count = items.Count;
		bool isSelectable = false;

		for (; index > -1 && index < count; index += increment)
		{
			var item = items[index];
			isSelectable = IsSelectableHelper(item);
			if (isSelectable)
			{
				var container = ContainerFromIndex(index);
				isSelectable = IsSelectableHelper(container);
				if (isSelectable)
				{
					break;
				}
			}
		}

		if (!isSelectable)
		{
			// If no selectable item was found, set index to -1 so selection will not be updated.
			index = -1;
		}
	}

	internal void SetAllowCustomValues(bool allow)
	{
		m_customValuesAllowed = allow;
	}
}
