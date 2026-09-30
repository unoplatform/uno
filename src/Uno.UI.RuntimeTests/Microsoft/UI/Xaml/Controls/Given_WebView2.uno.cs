#if HAS_UNO_WINUI
using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaFrameBuffer)]
public class Given_WebView2
{
	[TestMethod]
#if __SKIA__
	[Ignore("WebView2 is not yet supported on skia targets")]
#endif
	public async Task When_InvokeScriptAsync()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString("<html><body><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>");
		await TestServices.WindowHelper.WaitFor(() => navigated, timeoutMS: 10000);

		var sw = Stopwatch.StartNew();
		string color = null;

		do
		{
			// We need to wait for the element to be available, navigated
			// may be set to true too early on wasm.
			color = await webView.ExecuteScriptAsync(
				"""
				(function () {
					let testElement = document.getElementById('test');
					if(testElement){
						return testElement.style.backgroundColor.toString();
					}
					return "";
				})()
				""");

		} while (sw.Elapsed < TimeSpan.FromSeconds(10) && string.IsNullOrEmpty(color.Replace("\"", "")));

		Assert.AreEqual("\"blue\"", color);

		// Change color to red
		await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor = 'red'");
		color = await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor.toString()");

		Assert.AreEqual("\"red\"", color);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Collected_Then_Native_Iframe_Is_Removed()
	{
		var (webViewReference, elementId) = await LoadAndUnloadWebView2();

		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(10) && (webViewReference.IsAlive || IsInDom(elementId)))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			await Task.Yield();
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.IsFalse(webViewReference.IsAlive, "The WebView2 should be collectable once it left the tree.");
		Assert.IsFalse(IsInDom(elementId), "The iframe should be removed from the DOM once its WebView2 is collected.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Unloaded_While_Referenced_Then_Native_Iframe_Is_Kept()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			var elementId = GetIframeElementId(webView);

			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			for (var i = 0; i < 3; i++)
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				await TestServices.WindowHelper.WaitForIdle();
			}

			// WinUI keeps the browser across unloads and only closes it when the WebView2 is closed or destroyed.
			Assert.IsTrue(IsInDom(elementId), "Unloading a WebView2 that is still referenced must not remove its iframe.");

			await UITestHelper.Load(webView);

			Assert.AreEqual(elementId, GetIframeElementId(webView));
			Assert.IsTrue(IsInDom(elementId), "The reloaded WebView2 should present its original iframe.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<(WeakReference, string)> LoadAndUnloadWebView2()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);

			return (new WeakReference(webView), GetIframeElementId(webView));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	private static string GetIframeElementId(WebView2 webView)
		=> ((BrowserHtmlElement)((ContentPresenter)VisualTreeHelper.GetChild(webView, 0)).Content).ElementId;

	private static bool IsInDom(string elementId)
	{
		using var probe = BrowserHtmlElement.CreateHtmlElement("div");

		// The JS bridge turns a false result into an empty string, hence the explicit markers.
		return probe.ExecuteJavascript($"return document.getElementById('{elementId}') ? 'yes' : 'no';") == "yes";
	}
}
#endif
