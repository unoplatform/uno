using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal static class ItemsControlExtensions
	{
		internal static DataTemplate ResolveItemTemplate(this IItemsControl itemsControl, object item)
		{
			return DataTemplateHelper.ResolveTemplate(
				itemsControl.ItemTemplate,
				itemsControl.ItemTemplateSelector,
				item,
				itemsControl
			);
		}
	}
}
