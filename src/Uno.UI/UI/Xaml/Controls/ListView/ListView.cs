﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListView : ListViewBase, IListView
	{
		public ListView()
		{
			DefaultStyleKey = typeof(ListView);
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is ListViewItem;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ListViewItem() { IsGeneratedContainer = true };
		}

#if __ANDROID__ || __IOS__
		internal override ContentControl GetGroupHeaderContainer(object groupHeader)
		{
			return new ListViewHeaderItem() { IsGeneratedContainer = true };
		}
#endif
	}
}
