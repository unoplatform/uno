using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
#if WINAPPSDK
using Uno.UI.Extensions;
using _NativeType = Windows.UI.Xaml.Controls.Grid;
#elif __ANDROID__
using Uno.UI;
using _NativeType = Windows.UI.Xaml.Controls.BindableButtonEx;
#elif __IOS__
using UIKit;
using Uno.UI;
using _NativeType = Uno.UI.Views.Controls.BindableUIButton;
#else
using _NativeType = Windows.UI.Xaml.Controls.Grid; // We use a 'fake' native style on WASM
#endif
#if UNO_REFERENCE_API
using Uno.UI;
#elif __MACOS__
using AppKit;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[SampleControlInfo("Buttons", "Button_UseUWPDefaultStyles", description: "Demonstrates correct operation of UseUWPDefaultStyles flag.")]
	public sealed partial class Button_UseUWPDefaultStyles : UserControl
	{
		public Button_UseUWPDefaultStyles()
		{
			this.InitializeComponent();
			Loaded += Button_UseUWPDefaultStyles_Loaded;
		}

		private async void Button_UseUWPDefaultStyles_Loaded(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			var originalFlagValue = Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles;
			Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles = false;
#endif

			var button = new Windows.UI.Xaml.Controls.Button() { Content = "Native button" };
			RootPanel.Children.Add(button);
			await Task.Yield();
			var nativeView = button.FindFirstChild<_NativeType>();
			if (nativeView != null)
			{
				ResultsTextBlock.Text = "Native view found";
			}
			else
			{
				ResultsTextBlock.Text = "Native view not found";
			}
#if !WINAPPSDK
			Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles = originalFlagValue;
#endif
		}
	}
}
