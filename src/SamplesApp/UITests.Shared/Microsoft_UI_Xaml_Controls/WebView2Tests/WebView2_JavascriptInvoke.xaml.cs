using System;
using System.Threading;
using System.Threading.Tasks;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Uno.UI.Samples.Controls;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Name = "WebView2_JavascriptInvoke", ViewModelType = typeof(WebView2ViewModel))]
	public sealed partial class WebView2_JavascriptInvoke : UserControl
	{
#if HAS_UNO
		public WebView2_JavascriptInvoke()
		{
			this.InitializeComponent();
			//TODO:MZ:
			//	MyButton.Click += MyButton_OnClick;
			//}

			//private void MyButton_OnClick(object sender, RoutedEventArgs e)
			//{
			//	var t = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal,
			//		async () => await InvokeScriptAsync(MyWebView2, CancellationToken.None, GetReloadJavascript(), new string[] { "" })
			//	);
			//}
			//public static async Task<string> InvokeScriptAsync(Microsoft.UI.Xaml.Controls.WebView2 webView, CancellationToken ct, string script, string[] arguments)
			//{
			//	
			//	//return await webView.CoreWebView2.ExecuteScriptAsync(script, arguments).AsTask(ct);
		}

		//		private static string GetReloadJavascript()
		//		{
		//#if __IOS__
		//			return "location.reload(true);";
		//#else
		//			return "window.location.reload()";
		//#endif
		//		}
#endif
	}
}
