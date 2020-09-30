using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[SampleControlInfo("CommandBar", "Dynamic", ignoreInSnapshotTests: true)]
	public sealed partial class CommandBar_Dynamic : Page
	{
		public CommandBar_Dynamic()
		{
			this.InitializeComponent();

			cbVisibility.ItemsSource = Visibilities;
			cbVisibility.SelectedItem = Windows.UI.Xaml.Visibility.Visible;

			CommandVisibility.ItemsSource = Visibilities;
			CommandVisibility.SelectedItem = Windows.UI.Xaml.Visibility.Visible;

			BackgroundColor.ItemsSource = Colors;
			BackgroundColor.SelectedItem = Windows.UI.Colors.Red;

			ForegroundColor.ItemsSource = Colors;
			ForegroundColor.SelectedItem = Windows.UI.Colors.Black;

			CommandIsEnabled.ItemsSource = Booleans;
			CommandIsEnabled.SelectedItem = true;
		}

		public Windows.UI.Color[] Colors = new[]
		{
			Windows.UI.Colors.Black,
			Windows.UI.Colors.White,
			Windows.UI.Colors.Red,
			Windows.UI.Colors.Green,
			Windows.UI.Colors.Blue,
		};

		public bool[] Booleans = new[]
		{
			true,
			false,
		};

		public Windows.UI.Xaml.Visibility[] Visibilities = new[]
		{
			Windows.UI.Xaml.Visibility.Visible,
			Windows.UI.Xaml.Visibility.Collapsed,
		};
	}
}
