using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	public class IndexAwareItemContainerStyleSelector : StyleSelector
	{
		public Style FirstItemStyle { get; set; }
		public Style OtherItemsStyle { get; set; }
		public Style LastItemStyle { get; set; }

		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			var itemsControl = ItemsControl.ItemsControlFromItemContainer(container);
			var index = itemsControl.IndexFromContainer(container);

			if (index == 0)
			{
				return FirstItemStyle;
			}

			var itemsCount = itemsControl.Items?.Count ?? (itemsControl.ItemsSource as IEnumerable)?.Cast<object>().Count() ?? 0;
			if (index == itemsCount - 1)
			{
				return LastItemStyle;
			}

			return OtherItemsStyle;
		}
	}
}
