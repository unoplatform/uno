using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests
{
	public class ItemTemplateSelectorTestTemplateSelector : DataTemplateSelector
	{
		public DataTemplate DefaultTemplate { get; set; }
		public DataTemplate RedTemplate { get; set; }
		public DataTemplate BlueTemplate { get; set; }
		public DataTemplate GreenTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			var colourString = (item as ItemTemplateSelectorTestPageViewModel.ListItem)?.ColourString;

			bool? nullableBool = null;
			var toBool = nullableBool ?? false;


			if ((colourString?.ToUpperInvariant()?.StartsWith("RED")).GetValueOrDefault())
			{
				return RedTemplate;
			}
			if ((colourString?.ToUpperInvariant()?.StartsWith("BLUE")).GetValueOrDefault())
			{
				return BlueTemplate;
			}
			if ((colourString?.ToUpperInvariant()?.StartsWith("GREEN")).GetValueOrDefault())
			{
				return GreenTemplate;
			}

			return DefaultTemplate;
		}
	}
}
