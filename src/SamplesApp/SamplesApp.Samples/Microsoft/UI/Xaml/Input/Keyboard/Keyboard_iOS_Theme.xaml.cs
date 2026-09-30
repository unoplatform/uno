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

namespace UITests.Windows_UI_Xaml_Input.Keyboard
{
	[Sample("Keyboard", Name = nameof(Keyboard_iOS_Theme),
		Description = SampleDescription,
		IgnoreInSnapshotTests = true,
		IsManualTest = true)]
	public sealed partial class Keyboard_iOS_Theme : Page
	{
		private const string SampleDescription = "[iOS-only] Keyboard theme should be determined based on the following precedences: RequestedTheme > Device Theme.";

		public Keyboard_iOS_Theme()
		{
			this.InitializeComponent();
		}

		private void UpdateTheme(object sender, RoutedEventArgs e)
		{
			var root = XamlRoot?.Content as FrameworkElement;
			var theme = (sender as RadioButton).Content switch
			{
				"Light" => ElementTheme.Light,
				"Dark" => ElementTheme.Dark,
				"Default" => ElementTheme.Default,

				_ => throw new ArgumentOutOfRangeException()
			};

			root.RequestedTheme = theme;
		}
	}
}
