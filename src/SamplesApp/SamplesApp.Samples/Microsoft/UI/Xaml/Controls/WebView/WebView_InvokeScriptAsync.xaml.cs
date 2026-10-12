using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Microsoft_UI_Xaml_Controls.WebViewTests
{
	[Uno.UI.Samples.Controls.Sample("WebView")]
	public sealed partial class WebView_InvokeScriptAsync : Page
	{
		public WebView_InvokeScriptAsync()
		{
			this.InitializeComponent();
			TestWebView.Loaded += TestWebView_Loaded;
		}

		private void TestWebView_Loaded(object sender, RoutedEventArgs e)
		{
			var testHtml = "<html><body><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>";
			TestWebView.NavigateToString(testHtml);
		}

		private async void ChangeColor()
		{
			await TestWebView.InvokeScriptAsync("eval", new[] { "document.getElementById('test').style.backgroundColor = 'red'" });
		}

		private async void GetColor()
		{
			var color = await TestWebView.InvokeScriptAsync("eval", new[] { "document.getElementById('test').style.backgroundColor.toString()" });
			CurrentColorTextBlock.Text = color;
		}
	}
}
