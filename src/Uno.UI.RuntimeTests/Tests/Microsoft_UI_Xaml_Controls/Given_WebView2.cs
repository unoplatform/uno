using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Uno.UI.RuntimeTests.Helpers;
using FluentAssertions;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

#if !HAS_UNO || __ANDROID__ || __IOS__
[TestClass]
[RunsOnUIThread]
public class Given_WebView2
{
	[TestMethod]
#if __IOS__
	[Ignore("iOS is disabled https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task When_Navigate()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		var uri = new Uri("https://example.com/");
		await webView.EnsureCoreWebView2Async();
		bool navigationStarting = false;
		bool navigationDone = false;
		webView.NavigationStarting += (s, e) => navigationStarting = true;
		webView.NavigationCompleted += (s, e) => navigationDone = true;
		webView.CoreWebView2.Navigate(uri.ToString());
		Assert.IsNull(webView.Source);
		await TestServices.WindowHelper.WaitFor(() => navigationStarting, 1000);
		await TestServices.WindowHelper.WaitFor(() => navigationDone, 3000);
		Assert.IsNotNull(webView.Source);
		Assert.IsTrue(webView.Source.OriginalString.StartsWith("https://example.com/", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public async Task When_NavigateToString()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		var uri = new Uri("https://example.com/");
		await webView.EnsureCoreWebView2Async();
		bool navigationStarting = false;
		bool navigationDone = false;
		webView.NavigationStarting += (s, e) => navigationStarting = true;
		webView.NavigationCompleted += (s, e) => navigationDone = true;
		webView.Source = uri;
		Assert.IsNotNull(webView.Source);
		await TestServices.WindowHelper.WaitFor(() => navigationStarting, 3000);
		await TestServices.WindowHelper.WaitFor(() => navigationDone, 3000);
		Assert.IsNotNull(webView.Source);
		navigationStarting = false;
		navigationDone = false;
		webView.NavigateToString("<html></html>");
		await TestServices.WindowHelper.WaitFor(() => navigationStarting, 3000);
		await TestServices.WindowHelper.WaitFor(() => navigationDone, 3000);
#if HAS_UNO
		Assert.AreEqual(CoreWebView2.BlankUri, webView.Source);
#endif
	}


	[TestMethod]
	public async Task When_GoBack()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await webView.EnsureCoreWebView2Async();

		Assert.IsFalse(webView.CoreWebView2.CanGoBack);
		Assert.IsFalse(webView.CanGoBack);
		Assert.IsFalse(webView.CoreWebView2.CanGoForward);
		Assert.IsFalse(webView.CanGoForward);

		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.CoreWebView2.Navigate("https://example.com/1");
		await TestServices.WindowHelper.WaitFor(() => navigated, 3000);

		Assert.IsFalse(webView.CoreWebView2.CanGoBack);
		Assert.IsFalse(webView.CanGoBack);
		Assert.IsFalse(webView.CoreWebView2.CanGoForward);
		Assert.IsFalse(webView.CanGoForward);

		navigated = false;
		webView.CoreWebView2.Navigate("https://example.com/2");
		await TestServices.WindowHelper.WaitFor(() => navigated, 3000);

		Assert.IsTrue(webView.CoreWebView2.CanGoBack);
		Assert.IsTrue(webView.CanGoBack);

		navigated = false;
		webView.GoBack();
		await TestServices.WindowHelper.WaitFor(() => navigated, 3000);

		Assert.IsFalse(webView.CoreWebView2.CanGoBack);
		Assert.IsFalse(webView.CanGoBack);
		Assert.IsTrue(webView.CoreWebView2.CanGoForward);
		Assert.IsTrue(webView.CanGoForward);
	}

#if __ANDROID__ || __IOS__
	[TestMethod]
	public async Task When_IsScrollable()
	{
		var border = new Border();
		var webView = new WebView();
		webView.Source = new Uri("https://bing.com");
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);

		Assert.IsTrue(webView.IsScrollEnabled);

#if __IOS__
		var nativeWebView = ((_View)webView)
			.FindSubviewsOfType<INativeWebView>()
			.FirstOrDefault();
		var scrollView = ((_View)nativeWebView)?.FindSubviewsOfType<UIScrollView>().FirstOrDefault();
		Assert.IsTrue(scrollView.ScrollEnabled);
		Assert.IsTrue(scrollView.Bounces);
#endif

#if __ANDROID__
		var nativeWebView = (webView as Android.Views.ViewGroup)?
			.GetChildren(v => v is Android.Webkit.WebView)
			.FirstOrDefault() as Android.Webkit.WebView;
		Assert.IsTrue(nativeWebView.HorizontalScrollBarEnabled);
		Assert.IsTrue(nativeWebView.VerticalScrollBarEnabled);
#endif
		webView.IsScrollEnabled = false;

#if __IOS__
		Assert.IsFalse(scrollView.ScrollEnabled);
		Assert.IsFalse(scrollView.Bounces);
#endif

#if __ANDROID__
		Assert.IsFalse(nativeWebView.HorizontalScrollBarEnabled);
		Assert.IsFalse(nativeWebView.VerticalScrollBarEnabled);
#endif

	}
#endif

#if !__IOS__ // Temporarily disabled due to #11997
	[TestMethod]
	public async Task When_ExecuteScriptAsync_Has_No_Result()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			bool navigated = false;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await webView.EnsureCoreWebView2Async();
			webView.NavigationCompleted += (sender, e) => navigated = true;
			webView.NavigateToString("<html><body><script>function testMe(){ }</script><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>");
			await TestServices.WindowHelper.WaitFor(() => navigated);

			Func<Task> act = async () => await webView.ExecuteScriptAsync("testMe()");
			await act.Should().NotThrowAsync();
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	public async Task When_ExecuteScriptAsync()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			bool navigated = false;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await webView.EnsureCoreWebView2Async();
			webView.NavigationCompleted += (sender, e) => navigated = true;
			webView.NavigateToString("<html><body><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>");
			await TestServices.WindowHelper.WaitFor(() => navigated);

			var color = await webView.ExecuteScriptAsync("eval({ 'color' : document.getElementById('test').style.backgroundColor })");
			Assert.AreEqual("{\"color\":\"blue\"}", color);

			// Change color to red
			await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor = 'red';");
			color = await webView.ExecuteScriptAsync("eval({ 'color' : document.getElementById('test').style.backgroundColor })");

			Assert.AreEqual("{\"color\":\"red\"}", color);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	public async Task When_ExecuteScriptAsync_String_Double_Quote()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			bool navigated = false;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await webView.EnsureCoreWebView2Async();
			webView.NavigationCompleted += (sender, e) => navigated = true;
			webView.NavigateToString("<html></html>");
			await TestServices.WindowHelper.WaitFor(() => navigated);

			var script = $"'hello \"world\"'.toString()";
			var result = await webView.ExecuteScriptAsync(script);
			Assert.AreEqual("\"hello \\\"world\\\"\"", result);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	public async Task When_ExecuteScriptAsync_String()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			bool navigated = false;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await webView.EnsureCoreWebView2Async();

			webView.NavigationCompleted += (sender, e) => navigated = true;
			webView.NavigateToString("<html></html>");
			await TestServices.WindowHelper.WaitFor(() => navigated);
			var script = "(1 + 1).toString()";

			var result = await webView.ExecuteScriptAsync($"eval(\"{script}\")");
			Assert.AreEqual("\"2\"", result);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	public async Task When_LocalFolder_File()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			bool navigated = false;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await webView.EnsureCoreWebView2Async();
			webView.CoreWebView2?.SetVirtualHostNameToFolderMapping(
				"UnoNativeAssets",
				"WebContent",
				CoreWebView2HostResourceAccessKind.Allow);
			webView.NavigationCompleted += (sender, e) => navigated = true;
			string message = "";
			webView.WebMessageReceived += (s, e) =>
			{
				Assert.IsTrue(webView.Dispatcher.HasThreadAccess);
				message = e.WebMessageAsJson;
			};
			webView.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");
			await TestServices.WindowHelper.WaitFor(() => navigated);
			await TestServices.WindowHelper.WaitFor(() => message is not null, 2000);

			Assert.AreEqual(@"""rgb(255, 0, 0)""", message);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	public async Task When_ExecuteScriptAsync_Non_String()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			bool navigated = false;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await webView.EnsureCoreWebView2Async();
			webView.NavigationCompleted += (sender, e) => navigated = true;
			webView.NavigateToString("<html></html>");
			await TestServices.WindowHelper.WaitFor(() => navigated);
			var script = "(1 + 1)";

			var result = await webView.ExecuteScriptAsync($"eval(\"{script}\")");
			Assert.AreEqual("2", result);
		}

		await TestHelper.RetryAssert(Do, 3);
	}
#endif

#if __IOS__
	[Ignore("Currently fails on iOS https://github.com/unoplatform/uno/issues/9080")]
#endif
	[TestMethod]
	public async Task When_WebMessageReceived()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await webView.EnsureCoreWebView2Async();

		string message = null;
		webView.WebMessageReceived += (s, e) =>
		{
			Assert.IsTrue(webView.Dispatcher.HasThreadAccess);
			message = e.WebMessageAsJson;
		};

		webView.NavigateToString(
"""
<html>
	<head>
		<title>Test message</title>
	</head>
	<body>
		<div id='test' style='width: 100px; height: 100px; background-color: blue;'></div>
		<script type="text/javascript">
			function sendWebMessage(){
				try{
					let message = {"some": ['values',"in","json",1]};

					if (window.hasOwnProperty("chrome") && typeof chrome.webview !== undefined) {
						// Windows
						chrome.webview.postMessage(message);
					} else if (window.hasOwnProperty("unoWebView")) {
						// Android
						unoWebView.postMessage(JSON.stringify(message));
					} else if (window.hasOwnProperty("webkit") && typeof webkit.messageHandlers !== undefined) {
						// iOS and macOS
						webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(message));
					}
				}
				catch (ex){
					alert("Error occurred: " + ex);
				}
			}
			sendWebMessage()
		</script>
	</body>
</html>
"""
		);

		await TestServices.WindowHelper.WaitFor(() => message is not null, 2000);

		Assert.AreEqual(@"{""some"":[""values"",""in"",""json"",1]}", message);
	}
}
#endif
