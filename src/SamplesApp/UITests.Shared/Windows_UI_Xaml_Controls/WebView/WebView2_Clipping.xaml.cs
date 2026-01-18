using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UITests.Windows_UI_Xaml_Controls.WebView;

[Sample("WebView",
	IsManualTest = true,
	Description = "Shows a Webview between two rectangles and when scrolling it should have the right clipping")]
public sealed partial class WebView2_Clipping : Page
{
	public WebView2_Clipping()
	{
		this.InitializeComponent();
	}
}
