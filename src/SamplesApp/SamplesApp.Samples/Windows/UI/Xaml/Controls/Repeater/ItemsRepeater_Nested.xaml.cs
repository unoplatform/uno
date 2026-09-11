using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater
{
	[Sample("ItemsRepeater")]
	public sealed partial class ItemsRepeater_Nested : Page
	{
		public ItemsRepeater_Nested()
		{
			this.InitializeComponent();
		}

		private void SetSource(object sender, RoutedEventArgs e)
		{
			((FrameworkElement)sender).Visibility = Visibility.Collapsed;
			SUT.ItemsSource = Enumerable.Range(0, 5000).GroupBy(i => Math.Floor(i / 500.0));
		}
	}
}
