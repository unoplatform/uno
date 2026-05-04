using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;

namespace UITests.Shared.Windows_UI_Xaml
{
	[Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true, Description = "This sample shows 4 WebViews in different configurations. Top left is a simple WebView in a Border. Top right and Bottom right are a WebView in vertical and horizontal StackPanels respectively. Bottom left is a WebView in a Grid with Auto rows and columns. The top left webview should always show and take half the width and half the height of the sample. The other 3 should either show as blank or should show similarly to the first, based on the platform.")]
	public sealed partial class WebView2_InfiniteBounds : Page
	{
		public WebView2_InfiniteBounds()
		{
			this.InitializeComponent();
		}
	}
}
