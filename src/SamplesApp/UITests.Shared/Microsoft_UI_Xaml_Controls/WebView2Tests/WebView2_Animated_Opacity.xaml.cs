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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Description = "Opacity is animated on WebView2. It shouldn't exceed bounds while page is loading (Android bug #2006)")]
	public sealed partial class WebView2_Animated_Opacity : UserControl
	{
		public WebView2_Animated_Opacity()
		{
			this.InitializeComponent();

#if HAS_UNO
			VisualStateManager.GoToState(test, "Loading", true);
#endif
		}
	}
}
