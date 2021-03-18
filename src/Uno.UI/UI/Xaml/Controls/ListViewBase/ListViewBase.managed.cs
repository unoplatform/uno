#if UNO_REFERENCE_API || __MACOS__
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		IVirtualizingPanel VirtualizingPanel => ItemsPanelRoot as IVirtualizingPanel;

		private int PageSize => throw new NotImplementedException();

		private protected override bool ShouldItemsControlManageChildren => !(ItemsPanelRoot is IVirtualizingPanel);

		private protected override void Refresh()
		{
			base.Refresh();

			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().Refresh();

				InvalidateMeasure();
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
			//TODO: ISupportIncrementalLoading
		}

		partial void UpdateReordering(Point location, FrameworkElement draggedContainer, object draggedItem)
		{
			VirtualizingPanel?.GetLayouter().UpdateReorderingItem(location, draggedContainer, draggedItem);
		}

		partial void CompleteReordering(FrameworkElement draggedContainer, object draggedItem, ref Uno.UI.IndexPath? updatedIndex)
		{
			updatedIndex = VirtualizingPanel?.GetLayouter().CompleteReorderingItem(draggedContainer, draggedItem);
		}
	}
}
#endif
