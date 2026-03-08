using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Uno.UI.RuntimeTests.Helpers;
using Combinatorial.MSTest;


#if !HAS_UNO_WINUI && !WINAPPSDK
using Microsoft.UI.Xaml.Controls;
#endif

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

#if !HAS_UNO || __ANDROID__ || __IOS__ || __SKIA__
[RunsOnUIThread]
[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWpf | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaIslands | RuntimeTestPlatforms.SkiaFrameBuffer)]
public class Given_WebView2
{
	[TestMethod]
	[Ignore("This test is flaky on CI, see #9080")]
	public async Task When_Navigate()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForLoaded(border);
			var uri = new Uri("https://platform.uno/");
			await webView.EnsureCoreWebView2Async();
			bool navigationStarting = false;
			bool navigationDone = false;
			webView.NavigationStarting += (s, e) => navigationStarting = true;
			webView.NavigationCompleted += (s, e) => navigationDone = true;
			webView.CoreWebView2.Navigate(uri.ToString());
			Assert.IsNull(webView.Source);
			await TestServices.WindowHelper.WaitFor(() => navigationStarting, 1000);
			await TestServices.WindowHelper.WaitFor(() => navigationDone, 30000);
			Assert.IsNotNull(webView.Source);
			Assert.IsTrue(webView.Source.OriginalString.StartsWith("https://platform.uno/", StringComparison.OrdinalIgnoreCase));
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	[Ignore("This test is flaky on CI, see #9080")]
	public async Task When_NavigateToString()
	{
		async Task Do()
		{
			var border = new Border();
			var webView = new WebView2();
			webView.Width = 200;
			webView.Height = 200;
			border.Child = webView;
			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForLoaded(border);
			var uri = new Uri("https://platform.uno/");
			await webView.EnsureCoreWebView2Async();
			bool navigationStarting = false;
			bool navigationDone = false;
			webView.NavigationStarting += (s, e) => navigationStarting = true;
			webView.NavigationCompleted += (s, e) => navigationDone = true;
			webView.Source = uri;
			Assert.IsNotNull(webView.Source);
			await TestServices.WindowHelper.WaitFor(() => navigationStarting, 10000);
			await TestServices.WindowHelper.WaitFor(() => navigationDone, 10000);
			Assert.IsNotNull(webView.Source);
			navigationStarting = false;
			navigationDone = false;
			webView.NavigateToString("<html></html>");
			await TestServices.WindowHelper.WaitFor(() => navigationStarting, 10000);
			await TestServices.WindowHelper.WaitFor(() => navigationDone, 10000);
			Assert.AreEqual(new Uri("about:blank"), webView.Source);
		}

		await TestHelper.RetryAssert(Do, 3);
	}


	[TestMethod]
	[Ignore("Currently very flaky https://github.com/unoplatform/uno/issues/9080")]
	public async Task When_GoBack()
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

			Assert.IsFalse(webView.CoreWebView2.CanGoBack);
			Assert.IsFalse(webView.CanGoBack);
			Assert.IsFalse(webView.CoreWebView2.CanGoForward);
			Assert.IsFalse(webView.CanGoForward);

			webView.NavigationCompleted += (sender, e) => navigated = true;
			webView.CoreWebView2.Navigate("https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html");
			await TestServices.WindowHelper.WaitFor(() => navigated, 10000);

			Assert.IsFalse(webView.CoreWebView2.CanGoBack);
			Assert.IsFalse(webView.CanGoBack);
			Assert.IsFalse(webView.CoreWebView2.CanGoForward);
			Assert.IsFalse(webView.CanGoForward);

			navigated = false;
			webView.CoreWebView2.Navigate("https://platform.uno");
			await TestServices.WindowHelper.WaitFor(() => navigated, 10000);

			Assert.IsTrue(webView.CoreWebView2.CanGoBack);
			Assert.IsTrue(webView.CanGoBack);

			navigated = false;
			webView.GoBack();
			await TestServices.WindowHelper.WaitFor(() => navigated, 10000);

			Assert.IsFalse(webView.CoreWebView2.CanGoBack);
			Assert.IsFalse(webView.CanGoBack);
			Assert.IsTrue(webView.CoreWebView2.CanGoForward);
			Assert.IsTrue(webView.CanGoForward);
		}

		await TestHelper.RetryAssert(Do, 3);
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

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit | RuntimeTestPlatforms.SkiaWin32)] // Temporarily disabled due to #11997
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit | RuntimeTestPlatforms.SkiaWin32)] // Temporarily disabled due to #11997
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
			await TestServices.WindowHelper.WaitFor(() => navigated, 3000);

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit | RuntimeTestPlatforms.SkiaWin32)] // Temporarily disabled due to #11997
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
			await TestServices.WindowHelper.WaitFor(() => navigated, 3000);

			var script = $"'hello \"world\"'.toString()";
			var result = await webView.ExecuteScriptAsync(script);
			Assert.AreEqual("\"hello \\\"world\\\"\"", result);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit | RuntimeTestPlatforms.SkiaWin32)] // Temporarily disabled due to #11997
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
			await TestServices.WindowHelper.WaitFor(() => navigated, 3000);
			var script = "(1 + 1).toString()";

			var result = await webView.ExecuteScriptAsync($"eval(\"{script}\")");
			Assert.AreEqual("\"2\"", result);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

#if WINAPPSDK
	[Ignore("Crashes")]
#endif
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
				Assert.IsTrue(webView.DispatcherQueue.HasThreadAccess);
#if !WINAPPSDK
				Assert.IsTrue(webView.Dispatcher.HasThreadAccess);
#endif
				message = e.WebMessageAsJson;
			};
			webView.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");
			await TestServices.WindowHelper.WaitFor(() => navigated, 3000);
			await TestServices.WindowHelper.WaitFor(() => !string.IsNullOrEmpty(message), 2000);

			if (RuntimeTestsPlatformHelper.CurrentPlatform is RuntimeTestPlatforms.SkiaX11) // On X11 we double escape. This makes sense because in site.js, we stringify a string. Other webkit-based implementations get this wrong
			{
				Assert.AreEqual("\"\\\"rgb(255, 0, 0)\\\"\"", message);
			}
			else
			{
				Assert.AreEqual(@"""rgb(255, 0, 0)""", message);
			}
		}

		await TestHelper.RetryAssert(Do, 3);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit | RuntimeTestPlatforms.SkiaWin32)] // Temporarily disabled due to #11997
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
			await TestServices.WindowHelper.WaitFor(() => navigated, 3000);
			var script = "(1 + 1)";

			var result = await webView.ExecuteScriptAsync($"eval(\"{script}\")");
			Assert.AreEqual("2", result);
		}

		await TestHelper.RetryAssert(Do, 3);
	}

#if __IOS__
	[Ignore("Currently fails on iOS https://github.com/unoplatform/uno/issues/9080")]
#endif
	[TestMethod]
	// Fails on iOS https://github.com/unoplatform/uno/issues/9080
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIOS)]
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
			Assert.IsTrue(webView.DispatcherQueue.HasThreadAccess);
#if !WINAPPSDK
			Assert.IsTrue(webView.Dispatcher.HasThreadAccess);
#endif
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

		if (RuntimeTestsPlatformHelper.CurrentPlatform is RuntimeTestPlatforms.SkiaX11) // On X11 we double escape. If we fix this, other cases break.
		{
			Assert.AreEqual("\"{\\\"some\\\":[\\\"values\\\",\\\"in\\\",\\\"json\\\",1]}\"", message);
		}
		else
		{
			Assert.AreEqual(@"{""some"":[""values"",""in"",""json"",1]}", message);
		}
	}

	[TestMethod]
	[Ignore("WebResourceResponseReceived is not yet implemented")]
	public async Task When_Navigate_Error()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		var uri = new Uri("https://httpbin.org/status/444");
		await webView.EnsureCoreWebView2Async();
		bool navigationStarting = false;
		int statusCode = -1;
		CoreWebView2WebErrorStatus navigationStatus = CoreWebView2WebErrorStatus.Unknown;
		// TODO: WebResourceResponseReceived is not implemented
		webView.CoreWebView2.WebResourceResponseReceived += (s, e) =>
			statusCode = e.Response.StatusCode;
		webView.NavigationStarting += (s, e) => navigationStarting = true;
		webView.NavigationCompleted += (s, e) =>
			navigationStatus = e.WebErrorStatus;
		webView.CoreWebView2.Navigate(uri.ToString());
		Assert.IsNull(webView.Source);
		await TestServices.WindowHelper.WaitFor(() => navigationStarting, 3000);
		await TestServices.WindowHelper.WaitFor(() => navigationStatus != CoreWebView2WebErrorStatus.Unknown, 3000);
		Assert.IsNotNull(webView.Source);
		Assert.IsTrue(webView.Source.OriginalString.StartsWith("https://httpbin.org/status/444", StringComparison.OrdinalIgnoreCase));
	}

#if !WINAPPSDK && !__ANDROID__
	[TestMethod]
	[CombinatorialData]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Navigate_Unsupported_Scheme(bool handled)
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		var uri = new Uri("notsupported://httpbin.org/");
		await webView.EnsureCoreWebView2Async();
		bool navigationStarting = false;
		bool navigationDone = false;
		string scheme = null;
		webView.NavigationStarting += (s, e) => navigationStarting = true;
		webView.NavigationCompleted += (s, e) => navigationDone = true;
		webView.CoreWebView2.UnsupportedUriSchemeIdentified += (s, e) =>
		{
			scheme = e.Uri.Scheme;
			e.Handled = handled;
		};
		webView.CoreWebView2.Navigate(uri.ToString());
		Assert.IsNotNull(webView.Source);
		await TestServices.WindowHelper.WaitFor(() => scheme == "notsupported", 3000);
		if (handled)
		{
			Assert.IsFalse(navigationStarting);
			Assert.IsFalse(navigationDone);
		}
		else
		{
			await TestServices.WindowHelper.WaitFor(() => navigationStarting, 3000);
			await TestServices.WindowHelper.WaitFor(() => navigationDone, 3000);
		}
	}
#endif // !WINAPPSDK && !__ANDROID__
}

#endif
