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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.WebView
{
	[Sample(Description: "Opacity is animated on WebView. It shouldn't exceed bounds while page is loading (Android bug #2006)")]
	public sealed partial class WebView_Animated_Opacity : UserControl
	{
		public WebView_Animated_Opacity()
		{
			this.InitializeComponent();

#if HAS_UNO
			VisualStateManager.GoToState(test, "Loading", true);
#endif
		}
	}
}
