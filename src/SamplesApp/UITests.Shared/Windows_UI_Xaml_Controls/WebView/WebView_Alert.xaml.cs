using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.WebView;

namespace UITests.Shared.Windows_UI_Xaml
{
	[Sample("WebView", "WebView_Alert", typeof(WebViewViewModel))]
	public sealed partial class WebView_Alert : Page
	{
#if HAS_UNO
		public WebView_Alert()
		{
			this.InitializeComponent();
		}

		private void Load(object sender, RoutedEventArgs e)
		{
			var html = (DataContext as WebViewViewModel).AlertHtml;
			web.NavigateToString(html);
		}

		private async void StopScript(object sender, RoutedEventArgs e)
		{
			await web.CoreWebView2.ExecuteScriptAsync("count = -1;");
		}
#endif
	}
}
