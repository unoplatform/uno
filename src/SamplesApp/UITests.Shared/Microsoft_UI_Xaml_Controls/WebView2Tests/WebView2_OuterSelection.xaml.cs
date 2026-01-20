using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;

namespace UITests.Shared.Windows_UI_Xaml
{
	[Sample("WebView",
		Name = "WebView2_OuterSelection",
		IsManualTest = true,
		IgnoreInSnapshotTests = true,
		Description = "When trying to select the slider as if it were text or pressing Ctrl+A, the WebView should not become selected (blue overlay).")]
	public sealed partial class WebView2_OuterSelection : Page
	{
		public WebView2_OuterSelection()
		{
			this.InitializeComponent();
		}
	}
}
