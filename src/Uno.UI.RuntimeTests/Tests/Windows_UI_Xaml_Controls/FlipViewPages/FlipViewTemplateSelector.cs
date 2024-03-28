using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FlipViewPages
{
	public class FlipViewTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TemplateEven { get; set; }

		public DataTemplate TemplateOdd { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return Convert.ToInt32(item) % 2 == 0
					? TemplateEven
					: TemplateOdd;
		}
	}
}
