using Uno.UI.Samples.Controls;
using System;
using Windows.UI.Xaml;
using Windows.UI.Core;
using System.Threading;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Private.Infrastructure;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[SampleControlInfo("WebView", "WebView_JavascriptInvoke", typeof(WebViewViewModel))]
	public sealed partial class WebView_JavascriptInvoke : UserControl
	{
#if HAS_UNO
		public WebView_JavascriptInvoke()
		{
			this.InitializeComponent();
			MyButton.Click += MyButton_OnClick;
		}

		private void MyButton_OnClick(object sender, RoutedEventArgs e)
		{
			var t = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal,
				async () => await InvokeScriptAsync(MyWebView2, CancellationToken.None, GetReloadJavascript(), new string[] { "" })
			);
		}
		public static async Task<string> InvokeScriptAsync(Windows.UI.Xaml.Controls.WebView webView, CancellationToken ct, string script, string[] arguments)
		{
			return await webView.InvokeScriptAsync(script, arguments).AsTask(ct);
		}

		private static string GetReloadJavascript()
		{
#if __IOS__
			return "location.reload(true);";
#else
			return "window.location.reload()";
#endif
		}
#endif
	}
}
