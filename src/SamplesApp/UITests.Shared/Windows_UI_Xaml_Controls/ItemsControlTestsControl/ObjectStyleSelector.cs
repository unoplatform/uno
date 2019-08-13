using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ItemsControlTestsControl
{
	public class ObjectStyleSelector : StyleSelector
	{
		public object ObjectForStyle1 { get; set; }
		public Style Style1 { get; set; }
		public Style Style2 { get; set; }

		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			return item?.Equals(ObjectForStyle1) ? Style1 : Style2;
		}
	}
}
