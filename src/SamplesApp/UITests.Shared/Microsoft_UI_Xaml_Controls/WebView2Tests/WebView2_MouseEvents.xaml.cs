using Microsoft.Web.WebView2.Core;
using System;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[Uno.UI.Samples.Controls.Sample("WebView",
IsManualTest = true,
IgnoreInSnapshotTests = true,
Description = "Tests that mouse events are properly propagated to the WebView2 control and that events are also handled by the app itself.")]
public sealed partial class WebView2_MouseEvents : Page
{
	private int counter;
	public WebView2_MouseEvents()
	{
		this.InitializeComponent();
		this.Loaded += WebView2_MouseEvents_Loaded;
	}

	private void OnButton_Clicked(object o, object s)
	{
		counter++;
		LocalButton.Content = $"{counter}";
	}

	private async void WebView2_MouseEvents_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		await Inline.EnsureCoreWebView2Async();
		Inline.CoreWebView2?.SetVirtualHostNameToFolderMapping(
			"UnoNativeAssets",
			"WebContent",
			CoreWebView2HostResourceAccessKind.Allow);
		Inline.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");
	}
}
