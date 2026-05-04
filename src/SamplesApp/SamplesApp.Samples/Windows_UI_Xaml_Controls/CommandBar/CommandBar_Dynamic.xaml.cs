using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	[Sample("CommandBar", Name = "Dynamic", IgnoreInSnapshotTests = true)]
	public sealed partial class CommandBar_Dynamic : Page
	{
		public CommandBar_Dynamic()
		{
			this.InitializeComponent();

			cbVisibility.ItemsSource = Visibilities;
			cbVisibility.SelectedItem = Microsoft.UI.Xaml.Visibility.Visible;

			CommandVisibility.ItemsSource = Visibilities;
			CommandVisibility.SelectedItem = Microsoft.UI.Xaml.Visibility.Visible;

			BackgroundColor.ItemsSource = Colors;
			BackgroundColor.SelectedItem = Microsoft.UI.Colors.Red;

			ForegroundColor.ItemsSource = Colors;
			ForegroundColor.SelectedItem = Microsoft.UI.Colors.Black;

			CommandIsEnabled.ItemsSource = Booleans;
			CommandIsEnabled.SelectedItem = true;
		}

		public Windows.UI.Color[] Colors = new[]
		{
			Microsoft.UI.Colors.Black,
			Microsoft.UI.Colors.White,
			Microsoft.UI.Colors.Red,
			Microsoft.UI.Colors.Green,
			Microsoft.UI.Colors.Blue,
		};

		public bool[] Booleans = new[]
		{
			true,
			false,
		};

		public Microsoft.UI.Xaml.Visibility[] Visibilities = new[]
		{
			Microsoft.UI.Xaml.Visibility.Visible,
			Microsoft.UI.Xaml.Visibility.Collapsed,
		};
	}
}
