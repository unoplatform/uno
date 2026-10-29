#if HAS_UNO_WINUI
using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.UI.Xaml.Media;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public async Task When_Showing_Cross_Origin_Content_Then_It_Can_Use_Browser_Tab_Features()
	{
		var frame = await ShowCrossOriginContent();

		Assert.AreEqual("autoplay *; encrypted-media *; clipboard-write *", frame.Allow);
		Assert.IsTrue(frame.HasAllowFullscreen, "The frame should allow fullscreen for every origin through the allowfullscreen attribute.");

		if (frame.AllowedFeatures is not null)
		{
			// Chrome allows picture-in-picture for every origin by default.
			Assert.AreEqual("autoplay fullscreen encrypted-media picture-in-picture clipboard-write", frame.AllowedFeatures);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public async Task When_Permissions_Policy_Changed_Then_New_Web_View_Uses_It()
	{
		var policy = FeatureConfiguration.WebView2.PermissionsPolicy;
		var defaultPolicy = policy.ToArray();

		try
		{
			policy.Remove("fullscreen");
			policy.Add("camera");
			policy.Add("microphone https://example.com");

			// The browser splits a directive on any ASCII whitespace, not only spaces.
			policy.Add("geolocation\nhttps://example.com");

			var frame = await ShowCrossOriginContent();

			Assert.AreEqual(
				"autoplay *; encrypted-media *; clipboard-write *; camera *; microphone https://example.com; geolocation https://example.com",
				frame.Allow);
			Assert.IsFalse(frame.HasAllowFullscreen, "The frame should not allow fullscreen through the allowfullscreen attribute.");

			if (frame.AllowedFeatures is not null)
			{
				// The microphone and the location are delegated to another origin only.
				Assert.AreEqual("autoplay encrypted-media picture-in-picture clipboard-write camera", frame.AllowedFeatures);
			}
		}
		finally
		{
			RestorePermissionsPolicy(defaultPolicy);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public async Task When_Default_Feature_Added_Again_Then_Added_Entry_Wins()
	{
		var policy = FeatureConfiguration.WebView2.PermissionsPolicy;
		var defaultPolicy = policy.ToArray();

		try
		{
			// The browser would keep the first directive for a feature, so the default entries must be replaced, not repeated.
			policy.Add("autoplay 'none'");
			policy.Add("fullscreen 'none'");

			var frame = await ShowCrossOriginContent();

			Assert.AreEqual("autoplay 'none'; fullscreen 'none'; encrypted-media *; clipboard-write *", frame.Allow);
			Assert.IsFalse(frame.HasAllowFullscreen, "The frame should not allow fullscreen through the allowfullscreen attribute.");

			if (frame.AllowedFeatures is not null)
			{
				Assert.AreEqual("encrypted-media picture-in-picture clipboard-write", frame.AllowedFeatures);
			}
		}
		finally
		{
			RestorePermissionsPolicy(defaultPolicy);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Collected_Then_Native_Iframe_Is_Removed()
	{
		var (webViewReference, elementId) = await LoadNavigateAndUnloadWebView2();

		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(webViewReference), "The WebView2 should be collectable once it left the tree.");

		await TestServices.WindowHelper.WaitFor(
			() => !IsInDom(elementId),
			timeoutMS: 5000,
			message: "The iframe should be removed from the DOM once its WebView2 is collected.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Unloaded_While_Referenced_Then_Native_Iframe_Is_Kept()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await NavigateToMarker(webView, "kept");
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
			Assert.AreEqual("kept", ReadMarker(elementId), "Unloading a WebView2 that is still referenced must keep its document.");

			await UITestHelper.Load(webView);

			Assert.AreEqual(elementId, GetIframeElementId(webView), "The reloaded WebView2 should present its original iframe.");
			Assert.AreEqual("kept", ReadMarker(elementId), "The reloaded WebView2 should still show the document it navigated to.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<(WeakReference, string)> LoadNavigateAndUnloadWebView2()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);

			// A navigated WebView2 registers load and message listeners, which must not keep it alive.
			await NavigateToMarker(webView, "collected");

			return (new WeakReference(webView), GetIframeElementId(webView));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	private static async Task NavigateToMarker(WebView2 webView, string marker)
	{
		var navigated = false;
		void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args) => navigated = true;

		webView.NavigationCompleted += OnNavigationCompleted;
		try
		{
			webView.NavigateToString($"<html><body data-marker='{marker}'></body></html>");
			await TestServices.WindowHelper.WaitFor(() => navigated, timeoutMS: 10000, message: "The WebView2 should complete its NavigateToString.");
		}
		finally
		{
			webView.NavigationCompleted -= OnNavigationCompleted;
		}
	}

	private static string GetIframeElementId(WebView2 webView)
	{
		var root = VisualTreeHelper.GetChild(webView, 0) as ContentPresenter;
		Assert.AreEqual("WebViewTemplateRoot", root?.Name, "The WebView2 template root should be its ContentPresenter.");

		var element = root.Content as BrowserHtmlElement;
		Assert.IsNotNull(element, "The WebView2 template root should present its iframe as a BrowserHtmlElement.");

		return element.ElementId;
	}

	private static bool IsInDom(string elementId)
		=> WasmSemanticDomHelper.InvokeBrowserJs($"(function(){{return document.getElementById('{elementId}') ? '1' : '0';}})()") == "1";

	private static string ReadMarker(string elementId)
		=> WasmSemanticDomHelper.InvokeBrowserJs(
			$"(function(){{var body = document.getElementById('{elementId}')?.contentDocument?.body; return body ? (body.dataset.marker || '') : '';}})()");

	// Shows a document with an opaque origin, so cross-origin to the app, and returns its frame's attributes and the features
	// the document reports it may use. Only Chromium tells the latter, so it's null in other browsers.
	private static async Task<(string Allow, bool HasAllowFullscreen, string AllowedFeatures)> ShowCrossOriginContent()
	{
		const string Probe = """
			<script>
			var policy = document.featurePolicy;
			var features = ['autoplay', 'fullscreen', 'encrypted-media', 'picture-in-picture', 'clipboard-write', 'clipboard-read',
				'camera', 'microphone', 'geolocation'];
			var allowed = policy ? features.filter(f => policy.allowsFeature(f)).join(' ') : '?';
			parent.postMessage({ unoAllowedFeatures: allowed }, '*');
			</script>
			""";

		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var elementId = GetIframeElementId(webView);

			WasmSemanticDomHelper.InvokeBrowserJs($$"""
				(function () {
					window.__unoAllowedFeatures = null;
					window.__unoAllowedFeaturesListener = function (e) {
						if (e.source === document.getElementById('{{elementId}}').contentWindow && e.data && e.data.unoAllowedFeatures !== undefined) {
							window.__unoAllowedFeatures = e.data.unoAllowedFeatures;
						}
					};
					window.addEventListener('message', window.__unoAllowedFeaturesListener);
					return '';
				})()
				""");

			webView.CoreWebView2.Navigate("data:text/html;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Probe)));

			static string ReadReport()
				=> WasmSemanticDomHelper.InvokeBrowserJs("(function(){var f=window.__unoAllowedFeatures;return f===null?'':'allowed:'+f;})()");

			await TestServices.WindowHelper.WaitFor(
				() => ReadReport().Length > 0,
				timeoutMS: 10000,
				message: "The cross-origin document should report the features it may use.");

			var allowedFeatures = ReadReport()["allowed:".Length..];
			var allow = WasmSemanticDomHelper.InvokeBrowserJs($"(function(){{return document.getElementById('{elementId}').getAttribute('allow') ?? '';}})()");
			var hasAllowFullscreen = WasmSemanticDomHelper.InvokeBrowserJs(
				$"(function(){{return document.getElementById('{elementId}').hasAttribute('allowfullscreen') ? '1' : '0';}})()") == "1";

			return (allow, hasAllowFullscreen, allowedFeatures == "?" ? null : allowedFeatures);
		}
		finally
		{
			WasmSemanticDomHelper.InvokeBrowserJs("""
				(function () {
					window.removeEventListener('message', window.__unoAllowedFeaturesListener);
					delete window.__unoAllowedFeaturesListener;
					delete window.__unoAllowedFeatures;
					return '';
				})()
				""");
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static void RestorePermissionsPolicy(string[] directives)
	{
		var policy = FeatureConfiguration.WebView2.PermissionsPolicy;
		policy.Clear();
		foreach (var directive in directives)
		{
			policy.Add(directive);
		}
	}
}
#endif
