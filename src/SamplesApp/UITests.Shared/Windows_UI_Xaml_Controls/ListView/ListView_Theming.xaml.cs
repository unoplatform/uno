using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", IsManualTest = true, Description = "Change theme then scroll, expectation is that foreground color is correct.")]
	public sealed partial class ListView_Theming : Page
	{
		public List<string> Names { get; } = Enumerable.Range(1, 50).Select(x => $"Item {x}").ToList();

		public ListView_Theming()
		{
			this.InitializeComponent();

			DataContext = this;
		}

		private void Button_Click1(object sender, RoutedEventArgs e)
		{
			if (XamlRoot?.Content is FrameworkElement root)
			{
				root.RequestedTheme = ElementTheme.Light;
			}
		}
		private void Button_Click2(object sender, RoutedEventArgs e)
		{
			if (XamlRoot?.Content is FrameworkElement root)
			{
				root.RequestedTheme = ElementTheme.Dark;
			}
		}
	}
}
