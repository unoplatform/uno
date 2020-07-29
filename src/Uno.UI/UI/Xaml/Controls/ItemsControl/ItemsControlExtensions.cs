using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal static class ItemsControlExtensions
	{
		public static void UpdateItemTemplateSelectorForDisplayMemberPath(this IItemsControl itemsControl, string displayMemberPath)
		{
			//If setting a non-empty displayMemberPath,
			if (!string.IsNullOrEmpty(displayMemberPath))
			{
				//and there is no existing ItemTemplateSelector, supply a DisplayMemberTemplateSelector
				if (itemsControl != null && (itemsControl.ItemTemplateSelector == null || itemsControl.ItemTemplateSelector is DisplayMemberTemplateSelector))
				{
					itemsControl.ItemTemplateSelector = new DisplayMemberTemplateSelector(displayMemberPath);
				}
			}
			//Otherwise we're setting the displayMemberPath to empty, so clear the existing DisplayMemberTemplateSelector
			else if (itemsControl?.ItemTemplateSelector is DisplayMemberTemplateSelector)
			{
				itemsControl.ItemTemplateSelector = null;
			}
		}

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
