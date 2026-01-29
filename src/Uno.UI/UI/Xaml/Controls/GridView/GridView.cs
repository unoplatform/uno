using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
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

#if __ANDROID__ || __IOS__
		internal override ContentControl GetGroupHeaderContainer(object groupHeader)
		{
			return new GridViewHeaderItem() { IsGeneratedContainer = true };
		}
#endif
	}
}
