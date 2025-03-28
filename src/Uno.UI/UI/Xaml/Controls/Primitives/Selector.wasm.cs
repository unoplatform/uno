using System;
using System.Collections.Generic;
using System.Text;

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
			if (item == null)
			{
				return;
			}

			var container = ContainerFromItem(item);
			if (container is SelectorItem selectorItem)
			{
				selectorItem.IsSelected = updateTo;
			}
		}
	}
}
