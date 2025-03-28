using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	public class SelectorInheritanceTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TrueTemplate { get; set; }

		public DataTemplate FalseTemplate { get; set; }

		public DataTemplate NullTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return SelectTemplateCore(item);
		}

		protected override DataTemplate SelectTemplateCore(object item)
		{
			var value = item?.ToString();

			if (value != null)
			{
				return bool.Parse(value) ? TrueTemplate : FalseTemplate;
			}

			return NullTemplate;
		}
	}
}
