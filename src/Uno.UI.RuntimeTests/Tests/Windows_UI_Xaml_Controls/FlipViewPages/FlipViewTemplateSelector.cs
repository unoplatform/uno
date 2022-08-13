using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FlipViewPages
{
	public class FlipViewTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TemplatePair { get; set; }

		public DataTemplate TemplateOdd { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return Convert.ToInt32(item) % 2 == 0
					? TemplatePair
					: TemplateOdd;			
		}
	}
}
