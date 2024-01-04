#if UNO_REFERENCE_API || __MACOS__
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		private int PageSize
		{
			get
			{
				if (VirtualizingPanel is null)
				{
					return 0;
				}

				var layouter = VirtualizingPanel.GetLayouter();
				var firstVisibleIndex = layouter.FirstVisibleIndex;
				var lastVisibleIndex = layouter.LastVisibleIndex;
				if (lastVisibleIndex == -1)
				{
					return 0;
				}

				return lastVisibleIndex - firstVisibleIndex + 1;
			}
		}

		private void AddItems(int firstItem, int count, int section)
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().AddItems(firstItem, count, section);
			}
			else
			{
				Refresh();
			}
		}

		private void RemoveItems(int firstItem, int count, int section)
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().RemoveItems(firstItem, count, section);
			}
			else
			{
				Refresh();
			}
		}

		private void AddGroup(int groupIndexInView)
		{
			Refresh();
		}

		private void RemoveGroup(int groupIndexInView)
		{
			Refresh();
		}

		private void ReplaceGroup(int groupIndexInView)
		{
			Refresh();
		}

		private ContentControl ContainerFromGroupIndex(int groupIndex) => throw new NotImplementedException();

		private void TryLoadMoreItems()
		{
			if (VirtualizingPanel.GetLayouter() is { } layouter)
			{
				TryLoadMoreItems(layouter.LastVisibleIndex);
			}
		}
	}
}
#endif
