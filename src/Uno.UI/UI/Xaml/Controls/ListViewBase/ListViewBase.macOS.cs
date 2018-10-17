#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase : Selector
	{
		// TODO: This is copied from ListViewBase.cs (shared).

		public event ItemClickEventHandler ItemClick;

		public IList<object> SelectedItems { get; } = new List<object>();

		internal override void OnItemClicked(int clickedIndex)
		{
			var item = ItemFromIndex(clickedIndex);
			if (IsItemClickEnabled)
			{
				ItemClick?.Invoke(this, new ItemClickEventArgs { ClickedItem = item });
			}

			//Handle selection
			switch (SelectionMode)
			{
				case ListViewSelectionMode.None:
					break;
				case ListViewSelectionMode.Single:
					if (ItemsSource is ICollectionView collectionView)
					{
						//NOTE: Windows seems to call MoveCurrentTo(item); we set position instead to have expected behavior when you have duplicate items in the list.
						collectionView.MoveCurrentToPosition(clickedIndex);

						// The CollectionView may have intercepted the change
						clickedIndex = collectionView.CurrentPosition;

					}

					SelectedIndex = clickedIndex;
					SelectedItem = item;
					break;
				case ListViewSelectionMode.Multiple:
				case ListViewSelectionMode.Extended:
					HandleMultipleSelection(clickedIndex, item);
					break;
			}
		}

		private void HandleMultipleSelection(int clickedIndex, object item)
		{
			if (!SelectedItems.Contains(item))
			{
				SelectedItems.Add(item);
				SetSelectedState(clickedIndex, true);
			}
			else
			{
				SelectedItems.Remove(item);
				SetSelectedState(clickedIndex, false);
			}
		}

		private void SetSelectedState(int clickedIndex, bool selected)
		{
			var selectorItem = ContainerFromIndex(clickedIndex) as SelectorItem;
			if (selectorItem != null)
			{
				selectorItem.IsSelected = selected;
			}
		}
	}
}
