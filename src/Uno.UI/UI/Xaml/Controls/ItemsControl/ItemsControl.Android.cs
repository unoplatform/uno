using Android.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using System.Linq;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsControl
	{
		partial void InitializePartial()
		{

		}

		partial void RequestLayoutPartial()
		{
			RequestLayout();
		}

		/// <summary>
		/// Gets item for a particular 'display position.' For this purpose, if the source is grouped, it is treated as a flat list with the header items (groups themselves) placed contiguously after all of the group items.
		/// </summary>
		internal virtual object GetElementFromDisplayPosition(int displayPosition)
		{
			if (!IsGrouping)
			{
				return GetItems()?.ElementAtOrDefault(displayPosition);
			}

			if (displayPosition < NumberOfItems)
			{
				return GetItemFromIndex(displayPosition);
			}


			//The display item must be a group header
			return GetGroupAtDisplaySection(displayPosition - NumberOfItems).Group;
		}

		/// <summary>
		/// Get number of display items, including group headers if the source is grouped.
		/// </summary>
		internal virtual int GetDisplayItemCount()
		{
			return NumberOfItems + NumberOfDisplayGroups;
		}

		/// <summary>
		/// Get flattened item index, if the source is grouped, to supply to <see cref="GetElementFromDisplayPosition(int)"/>.
		/// </summary>
		internal virtual int GetDisplayIndexFromIndexPath(Uno.UI.IndexPath indexPath)
		{
			return GetIndexFromIndexPath(indexPath);
		}

		/// <summary>
		/// Get index for group header to supply to <see cref="GetElementFromDisplayPosition(int)"/>.
		/// </summary>
		/// <param name="groupIndex">The position of the group in the item source</param>
		internal virtual int GetGroupHeaderDisplayIndex(int groupIndex)
		{
			if (!IsGrouping)
			{
				throw new InvalidOperationException();
			}
			var itemsCount = NumberOfItems;
			return itemsCount + groupIndex;
		}

		/// <summary>
		/// Is the item at the display position a group header.
		/// </summary>
		internal virtual bool GetIsGroupHeader(int displayPosition)
		{
			if (!IsGrouping)
			{
				return false;
			}

			return displayPosition >= NumberOfItems;
		}
	}
}
