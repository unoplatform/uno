using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	public class RandomDataTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Template1 { get; set; }
		public DataTemplate Template2 { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return InnerSelectTemplate(item);
		}

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return InnerSelectTemplate(item);
		}

		private DataTemplate InnerSelectTemplate(object item)
		{
			if (item != null)
			{
				var random = new Random();
				return (random.Next(2) % 2 == 0) ? Template1 : Template2;
			}

			return null;
		}
	}
}
