using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;

namespace UITests.Shared.Windows_UI_Xaml
{
	[SampleControlInfo("WebView", "WebView2_Alert", typeof(WebView2ViewModel))]
	public sealed partial class WebView2_Alert : Page
	{
		public WebView2_Alert()
		{
			this.InitializeComponent();
		}

		private async void Load(object sender, RoutedEventArgs e)
		{
			var html = (DataContext as WebView2ViewModel).AlertHtml;
			await web.EnsureCoreWebView2Async();
			web.CoreWebView2.NavigateToString(html);
		}
	}
}
