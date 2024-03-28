using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class Selector : ItemsControl
	{
		partial void OnSelectedItemChangedPartial(object oldSelectedItem, object selectedItem)
		{
			UpdateItemSelectedState(oldSelectedItem, false);
			UpdateItemSelectedState(selectedItem, true);
		}

		private void UpdateItemSelectedState(object item, bool updateTo)
		{
			var cell = GetCellForItem(item) as ListViewBaseInternalContainer;
			var selector = cell?.Content as SelectorItem;

			if (selector != null)
			{
				using (cell.InterceptSetNeedsLayout())
				{
					selector.IsSelected = updateTo;
				}
			}
		}

		protected virtual UICollectionViewCell GetCellForItem(object item)
		{
			return null;
		}
	}
}
