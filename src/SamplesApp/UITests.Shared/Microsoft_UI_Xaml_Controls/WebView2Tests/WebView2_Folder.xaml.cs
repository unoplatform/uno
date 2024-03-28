using Microsoft.Web.WebView2.Core;
using System;
using Windows.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true)]
public sealed partial class WebView2_Folder : Page
{
	public WebView2_Folder()
	{
		this.InitializeComponent();
		this.Loaded += WebView2_Folder_Loaded;
	}

	private async void WebView2_Folder_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		await Inline.EnsureCoreWebView2Async();
		Inline.CoreWebView2?.SetVirtualHostNameToFolderMapping(
			"UnoNativeAssets",
			"WebContent",
			CoreWebView2HostResourceAccessKind.Allow);
		Inline.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");
	}
}
