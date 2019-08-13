using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ItemsControlTestsControl
{
	public class ObjectTemplateSelector : DataTemplateSelector
	{
		public object ObjectForTemplate1 { get; set; }
		public DataTemplate Template1 { get; set; }
		public DataTemplate Template2 { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return SelectTemplateCore(item);
		}

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return item?.Equals(ObjectForTemplate1) ? Template1 : Template2;
		}
	}
}

