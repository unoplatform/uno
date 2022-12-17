using Uno.UI.Samples.Controls;
using System;
using Windows.UI.Xaml;
using Windows.UI.Core;
using System.Threading;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[SampleControlInfo("WebView", "WebView2_JavascriptInvoke", typeof(WebView2ViewModel))]
	public sealed partial class WebView2_JavascriptInvoke : UserControl
	{
#if HAS_UNO
		public WebView2_JavascriptInvoke()
		{
			this.InitializeComponent();
			MyButton.Click += MyButton_OnClick;
		}

		private void MyButton_OnClick(object sender, RoutedEventArgs e)
		{
			var t = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
				async () => await InvokeScriptAsync(MyWebView2, CancellationToken.None, GetReloadJavascript(), new string[] { "" })
			);
		}
		public static async Task<string> InvokeScriptAsync(Microsoft.UI.Xaml.Controls.WebView2 webView, CancellationToken ct, string script, string[] arguments)
		{
			return await webView.InvokeScriptAsync(script, arguments).AsTask(ct);
		}

		private static string GetReloadJavascript()
		{
#if XAMARIN_IOS
			return "location.reload(true);";
#else
			return "window.location.reload()";
#endif
		}
#endif
	}
}
