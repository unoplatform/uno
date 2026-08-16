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
		private const string SampleDescription = "[iOS-only] Keyboard theme should be determined based on the following precedences: KeyboardAppearance > RequestedTheme > Device Theme.";

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

		private void UpdateKeyboardAppearance(object sender, RoutedEventArgs e)
		{
#if __APPLE_UIKIT__
			var appearance = (sender as RadioButton).Content switch
			{
				"Light" => UIKit.UIKeyboardAppearance.Light,
				"Dark" => UIKit.UIKeyboardAppearance.Dark,
				"Default" => UIKit.UIKeyboardAppearance.Default,

				_ => throw new ArgumentOutOfRangeException()
			};
			foreach (var item in TestPanel.Children.OfType<FrameworkElement>())
			{
				if (item is TextBox tbox && tbox.PlaceholderText?.StartsWith("custom") == true)
				{
					tbox.PlaceholderText = $"custom: {appearance}";
					tbox.KeyboardAppearance = appearance;
				}
				else if (item is PasswordBox pbox && pbox.PlaceholderText?.StartsWith("custom") == true)
				{
					pbox.PlaceholderText = $"custom: {appearance}";
					pbox.KeyboardAppearance = appearance;
				}
			}
#endif
		}
	}
}
