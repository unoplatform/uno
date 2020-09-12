using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class GridView : ListViewBase
	{
		public GridView()
		{
			DefaultStyleKey = typeof(GridView);
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is GridViewItem;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new GridViewItem() { IsGeneratedContainer = true };
		}

		internal override ContentControl GetGroupHeaderContainer(object groupHeader)
		{
			return new GridViewHeaderItem() { IsGeneratedContainer = true };
		}
	}
}
