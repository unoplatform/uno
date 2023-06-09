using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", IgnoreInSnapshotTests = true)]
	public sealed partial class WebView2_Video_Bug546 : Page
	{
		public WebView2_Video_Bug546()
		{
			this.InitializeComponent();
		}

		private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			await webview.EnsureCoreWebView2Async();
			webview.CoreWebView2.Navigate(addr.Text);
		}
	}
}
