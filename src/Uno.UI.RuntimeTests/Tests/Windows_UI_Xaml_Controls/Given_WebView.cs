using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.Web.WebView2.Core;



#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif
#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif
#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if (!HAS_UNO || __ANDROID__ || __IOS__ || __SKIA__) && !WINAPPSDK
[RunsOnUIThread]
// only SkiaMacOS right now
[ConditionalTestClass(IgnoredPlatforms = RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWpf | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaIslands)]
public class Given_WebView
{
	[TestMethod]
#if __IOS__
	[Ignore("iOS is disabled https://github.com/unoplatform/uno/issues/9080")]
#endif
	public void When_Navigate()
	{
		var webView = new WebView();
		var uri = new Uri("https://bing.com");
		webView.Navigate(uri);
		Assert.IsNotNull(webView.Source);
		Assert.AreEqual("https://bing.com/", webView.Source.OriginalString);
		Assert.AreEqual("https://bing.com", uri.OriginalString);
	}

#if __ANDROID__ || __IOS__ || __WASM__
	[TestMethod]
	public void When_NavigateWithHttpRequestMessage()
	{
		var webView = new WebView();
		var uri = new Uri("https://bing.com");
		webView.NavigateWithHttpRequestMessage(new global::Windows.Web.Http.HttpRequestMessage(global::Windows.Web.Http.HttpMethod.Get, uri));
		Assert.IsNotNull(webView.Source);
		Assert.AreEqual("https://bing.com/", webView.Source.OriginalString);
		Assert.AreEqual("https://bing.com", uri.OriginalString);
	}
#endif

	[TestMethod]
	public async Task When_NavigateToString()
	{
		var border = new Border();
		var webView = new WebView();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		var uri = new Uri("https://platform.uno/");
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
	public async Task When_GoBack()
	{
		var border = new Border();
		var webView = new WebView();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);

		Assert.IsFalse(webView.CanGoBack);
		Assert.IsFalse(webView.CanGoForward);

		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.Navigate(new Uri("https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html"));
		await TestServices.WindowHelper.WaitFor(() => navigated, 10000);

		Assert.IsFalse(webView.CanGoBack);
		Assert.IsFalse(webView.CanGoForward);

		navigated = false;
		webView.Navigate(new Uri("https://platform.uno"));
		await TestServices.WindowHelper.WaitFor(() => navigated, 10000);

		Assert.IsTrue(webView.CanGoBack);

		navigated = false;
		webView.GoBack();
		await TestServices.WindowHelper.WaitFor(() => navigated, 10000);

		Assert.IsFalse(webView.CanGoBack);
		Assert.IsTrue(webView.CanGoForward);
	}

#if !HAS_UNO
	[TestMethod]
	public async Task When_InvokeScriptAsync()
	{
		var border = new Border();
		var webView = new WebView();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString("<html><body><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>");
		await TestServices.WindowHelper.WaitFor(() => navigated);

		var color = await webView.InvokeScriptAsync("eval", new[] { "document.getElementById('test').style.backgroundColor.toString()" });
		Assert.AreEqual("blue", color);

		// Change color to red
		await webView.InvokeScriptAsync("eval", new[] { "document.getElementById('test').style.backgroundColor = 'red'" });
		color = await webView.InvokeScriptAsync("eval", new[] { "document.getElementById('test').style.backgroundColor.toString()" });

		Assert.AreEqual("red", color);
	}

	[TestMethod]
	public async Task When_InvokeScriptAsync_String()
	{
		var border = new Border();
		var webView = new WebView();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString("<html></html>");
		await TestServices.WindowHelper.WaitFor(() => navigated);
		var script = "(1 + 1).toString()";

		var result = await webView.InvokeScriptAsync("eval", new[] { script });
		Assert.AreEqual("2", result);
	}

	[TestMethod]
	public async Task When_InvokeScriptAsync_Non_String()
	{
		var border = new Border();
		var webView = new WebView();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString("<html></html>");
		await TestServices.WindowHelper.WaitFor(() => navigated);
		var script = "(1 + 1)";

		var result = await webView.InvokeScriptAsync("eval", new[] { script });
		Assert.AreEqual("", result);
	}
#endif
}
#endif
