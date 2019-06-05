using System;
using System.Collections.Generic;
using System.Text;
using Windows.Data.Xml.Dom;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TreeView
{
	public class ItemTypeTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ParentTemplate { get; set; }

		public DataTemplate ChildTemplate { get; set; }

	}
}
