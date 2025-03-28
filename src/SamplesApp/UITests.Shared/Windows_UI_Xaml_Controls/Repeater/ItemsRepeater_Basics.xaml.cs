using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater
{
	[Sample("ItemsRepeater")]
	public sealed partial class ItemsRepeater_Basics : Page
	{
		public ItemsRepeater_Basics()
		{
			this.InitializeComponent();
		}

		private void SetSource(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			var dt = new DataTemplate(() => new Border
			{
				Child = new MyItem()
			});
			var SUT = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeater
			{
				ItemTemplate = dt
			};
			TheSV.Content = SUT;
			((FrameworkElement)sender).Visibility = Visibility.Collapsed;
			SUT.ItemsSource = Enumerable.Range(0, 5000);
#endif
		}
	}
}
