using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

// These tests also run on WinUI, so they check Uno's Close against it. Close isn't implemented on the other Uno targets yet.
[TestClass]
[RunsOnUIThread]
public class Given_WebView2_Close
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_Then_CoreWebView2_Is_Null_And_Members_Fail()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();

			webView.Close();

			Assert.IsNull(webView.CoreWebView2, "CoreWebView2 should be null once the WebView2 is closed.");
			Assert.IsFalse(webView.CanGoBack);
			Assert.IsFalse(webView.CanGoForward);

			// A second Close, GoBack and GoForward do nothing.
			webView.Close();
			webView.GoBack();
			webView.GoForward();

			await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await webView.EnsureCoreWebView2Async());
			Assert.ThrowsExactly<ObjectDisposedException>(() => webView.Source = new Uri("https://platform.uno/"));
			Assert.ThrowsExactly<InvalidOperationException>(() => webView.NavigateToString("<p>closed</p>"));
			Assert.ThrowsExactly<InvalidOperationException>(() => webView.Reload());

			// ExecuteScriptAsync fails the operation it returns rather than the call.
			var script = webView.ExecuteScriptAsync("1");
			await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await script);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_From_CoreWebView2Initialized_Before_Template_Then_EnsureCoreWebView2Async_Completes()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };
		webView.CoreWebView2Initialized += (sender, args) => sender.Close();

		var ensureTask = webView.EnsureCoreWebView2Async().AsTask();
		await TestServices.WindowHelper.WaitFor(
			() => ensureTask.IsCompleted,
			timeoutMS: 30000,
			message: "EnsureCoreWebView2Async should complete when a CoreWebView2Initialized handler closes the WebView2.");

		// Closing the WebView2 from CoreWebView2Initialized doesn't fail the call.
		await ensureTask;
		Assert.IsNull(webView.CoreWebView2, "The CoreWebView2Initialized handler should have closed the WebView2.");
	}
}
