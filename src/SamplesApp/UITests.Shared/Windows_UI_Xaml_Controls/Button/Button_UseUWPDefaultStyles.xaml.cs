using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
#if WINAPPSDK
using Uno.UI.Extensions;
using _NativeType = Microsoft.UI.Xaml.Controls.Grid;
#elif __ANDROID__
using Uno.UI;
using _NativeType = Microsoft.UI.Xaml.Controls.BindableButtonEx;
#elif __APPLE_UIKIT__
using UIKit;
using Uno.UI;
using _NativeType = Uno.UI.Views.Controls.BindableUIButton;
#else
using _NativeType = Microsoft.UI.Xaml.Controls.Grid; // We use a 'fake' native style on WASM
#endif
#if UNO_REFERENCE_API
using Uno.UI;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[Sample("Buttons", "Button_UseUWPDefaultStyles", Description: "Demonstrates correct operation of UseUWPDefaultStyles flag.")]
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

			var button = new Microsoft.UI.Xaml.Controls.Button() { Content = "Native button" };
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
