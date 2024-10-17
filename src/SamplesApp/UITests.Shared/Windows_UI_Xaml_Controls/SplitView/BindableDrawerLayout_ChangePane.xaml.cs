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
using Uno.Extensions;
using Windows.UI.Xaml.Automation;

namespace UITests.Windows_UI_Xaml_Controls.SplitView
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[SampleControlInfo("SplitView", nameof(BindableDrawerLayout_ChangePane))]
	public sealed partial class BindableDrawerLayout_ChangePane : Page
	{
		public BindableDrawerLayout_ChangePane()
		{
			this.InitializeComponent();
		}
		private void ManualOpenPane(object sender, RoutedEventArgs e) => RootSplitView.IsPaneOpen = true;

		private void ManualClosePane(object sender, RoutedEventArgs e) => RootSplitView.IsPaneOpen = false;

		private void SetPaneTo(object sender, RoutedEventArgs e)
		{
			var tag = (string)((Button)sender).Tag;

			RootSplitView.Pane = tag switch
			{
				"SkyBlue" => CreateGrid(Windows.UI.Colors.SkyBlue),
				"Pink" => CreateGrid(Windows.UI.Colors.Pink),

				_ => null,
			};

			UIElement CreateGrid(Windows.UI.Color color) => new Grid
			{
				Background = new SolidColorBrush(color),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			}.Apply(x => AutomationProperties.SetAutomationId(x, "DrawerContent"));
		}

		private void SetPaneToPinkGrid(object sender, RoutedEventArgs e)
		{
			RootSplitView.Pane = new Grid
			{
				Background = new SolidColorBrush(Windows.UI.Colors.Pink),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};
		}
	}
}
