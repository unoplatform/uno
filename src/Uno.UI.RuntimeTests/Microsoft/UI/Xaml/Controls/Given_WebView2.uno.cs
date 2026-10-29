#if HAS_UNO_WINUI
using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Markup;
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_Then_Native_Iframe_Is_Removed()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await NavigateToMarker(webView, "closed");
			var elementId = GetIframeElementId(webView);

			webView.Close();

			Assert.IsFalse(IsInDom(elementId), "Close should remove the iframe from the DOM right away.");

			// Unloading and loading the closed WebView2 again must neither touch the removed iframe nor create a new one.
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
			await UITestHelper.Load(webView);

			var root = VisualTreeHelper.GetChild(webView, 0) as ContentPresenter;
			Assert.IsNull(root?.Content, "A closed WebView2 should not present an iframe.");
			Assert.IsNull(webView.CoreWebView2, "Loading a closed WebView2 again should not create a CoreWebView2.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_Then_Collectable()
	{
		var (webViewReference, elementId) = await LoadNavigateAndUnloadWebView2(close: true);

		Assert.IsFalse(IsInDom(elementId), "Close should remove the iframe from the DOM.");
		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(webViewReference), "A closed WebView2 should be collectable once it left the tree.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_Before_Loaded_Then_Pending_EnsureCoreWebView2Async_Fails()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		// The WebView2 isn't loaded, so the operation waits for its native WebView.
		var ensureTask = webView.EnsureCoreWebView2Async().AsTask();
		Assert.IsFalse(ensureTask.IsCompleted, "EnsureCoreWebView2Async should wait for the WebView2 to load.");

		webView.Close();

		// Unlike WinUI, which doesn't wait for a template, the pending call fails as a call made after Close does.
		await TestServices.WindowHelper.WaitFor(() => ensureTask.IsCompleted, timeoutMS: 5000, message: "Close should end a pending EnsureCoreWebView2Async.");
		var exception = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await ensureTask);
		Assert.AreEqual("Cannot create CoreWebView2 for Closed WebView2 element.", exception.Message, "The message should be WinUI's.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_With_Script_Pending_Then_ExecuteScriptAsync_Returns_Empty_String()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await NavigateToMarker(webView, "script");

			// The script runs asynchronously, so it's still pending when the WebView2 closes.
			var script = webView.ExecuteScriptAsync("document.body.dataset.marker").AsTask();
			webView.Close();

			// As in WinUI, which swallows the error of the closed browser and returns its default value.
			Assert.AreEqual("", await script, "A script still pending when the WebView2 closes should complete with an empty string.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_With_Navigation_Pending_Then_NavigationStarting_Is_Not_Raised()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };
		var navigationStartingCount = 0;

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var elementId = GetIframeElementId(webView);

			var coreWebView2 = webView.CoreWebView2;
			coreWebView2.NavigationStarting += (sender, args) => navigationStartingCount++;
			webView.NavigationStarting += (sender, args) => navigationStartingCount++;

			// NavigationStarting is raised from the dispatcher, so the navigation is still pending when the WebView2 closes.
			coreWebView2.Navigate("data:text/html,pending");
			webView.Close();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0, navigationStartingCount, "A navigation requested before Close should be dropped.");
			Assert.IsFalse(IsInDom(elementId), "Close should remove the iframe from the DOM.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_From_NavigationStarting_Then_Navigation_Is_Dropped()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };
		var navigationStartingCount = 0;
		void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{
			navigationStartingCount++;
			sender.Close();
		}

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var elementId = GetIframeElementId(webView);
			webView.NavigationStarting += OnNavigationStarting;

			// The dropped load itself can't be observed: without the iframe it would only fail inside the dispatcher.
			webView.NavigateToString("<html><body data-marker='dropped'></body></html>");
			await TestServices.WindowHelper.WaitFor(
				() => navigationStartingCount > 0,
				timeoutMS: 5000,
				message: "NavigationStarting should be raised for the navigation.");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, navigationStartingCount, "NavigationStarting should be raised once.");
			Assert.IsNull(webView.CoreWebView2, "The NavigationStarting handler should have closed the WebView2.");
			Assert.IsFalse(IsInDom(elementId), "Close should remove the iframe from the DOM.");
		}
		finally
		{
			webView.NavigationStarting -= OnNavigationStarting;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_Closed_During_Navigation_Completion_Then_CoreWebView2_Raises_No_More_Events(bool fromSourceChanged)
	{
		var webView = new WebView2 { Width = 200, Height = 200 };
		var closed = false;
		var eventsAfterClose = "";
		void Record(string name)
		{
			if (closed)
			{
				eventsAfterClose += name + " ";
			}
		}

		void CloseOnce(string name)
		{
			if (closed)
			{
				Record(name);
				return;
			}

			webView.Close();
			closed = true;
		}

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var coreWebView2 = webView.CoreWebView2;

			// A same-origin page without scripts, whose load changes Source.
			coreWebView2.SetVirtualHostNameToFolderMapping("UnoNativeAssets", "WebContent", CoreWebView2HostResourceAccessKind.Allow);

			coreWebView2.NavigationStarting += (sender, args) => Record(nameof(coreWebView2.NavigationStarting));
			coreWebView2.NavigationCompleted += (sender, args) => Record(nameof(coreWebView2.NavigationCompleted));
			coreWebView2.HistoryChanged += (sender, args) => Record(nameof(coreWebView2.HistoryChanged));
			coreWebView2.WebMessageReceived += (sender, args) => Record(nameof(coreWebView2.WebMessageReceived));
			coreWebView2.SourceChanged += (sender, args) =>
			{
				if (fromSourceChanged)
				{
					CloseOnce(nameof(coreWebView2.SourceChanged));
				}
				else
				{
					Record(nameof(coreWebView2.SourceChanged));
				}
			};
			coreWebView2.DocumentTitleChanged += (sender, args) =>
			{
				if (fromSourceChanged)
				{
					Record(nameof(coreWebView2.DocumentTitleChanged));
				}
				else
				{
					CloseOnce(nameof(coreWebView2.DocumentTitleChanged));
				}
			};

			coreWebView2.Navigate("http://UnoNativeAssets/css/site.css");
			await TestServices.WindowHelper.WaitFor(
				() => closed,
				timeoutMS: 10000,
				message: $"The {(fromSourceChanged ? "SourceChanged" : "DocumentTitleChanged")} handler should have closed the WebView2.");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(webView.CoreWebView2, "The handler should have closed the WebView2.");
			Assert.AreEqual("", eventsAfterClose.Trim(), "As in WinUI, a closed CoreWebView2 should raise no more events.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Releasing_Browser_Fails_Then_WebView2_Is_Still_Closed()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var elementId = GetIframeElementId(webView);

			MakeBrowserReleaseFail(elementId);

			// As in WinUI, Close doesn't throw; the failure is logged.
			webView.Close();

			Assert.IsNull(webView.CoreWebView2, "The WebView2 should be closed even though releasing its browser failed.");
			Assert.IsFalse(IsInDom(elementId), "The iframe should be removed even though releasing its browser failed.");
			await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await webView.EnsureCoreWebView2Async());
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Releasing_Browser_Fails_On_Retemplate_Then_New_Template_Gets_A_Browser()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var replacedElementId = GetIframeElementId(webView);
			MakeBrowserReleaseFail(replacedElementId);

			await Retemplate(webView);

			// A failure while the template is applied doesn't reach Application.UnhandledException, so the iframe checks catch it.
			Assert.IsFalse(IsInDom(replacedElementId), "The iframe of the replaced template should still be removed.");
			Assert.AreNotEqual(replacedElementId, GetIframeElementId(webView), "The new template should present a new iframe.");
			await NavigateToMarker(webView, "retemplated");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_From_CoreWebView2Initialized_On_Load_Then_No_Iframe_Is_Presented()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };
		webView.CoreWebView2Initialized += (sender, args) => sender.Close();

		// Loaded handlers' exceptions are caught and reported here.
		Exception unhandledException = null;
		void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
		{
			unhandledException ??= args.Exception;
			args.Handled = true;
		}

		Microsoft.UI.Xaml.Application.Current.UnhandledException += OnUnhandledException;

		try
		{
			await UITestHelper.Load(webView);

			Assert.IsNull(unhandledException, $"Closing the WebView2 from CoreWebView2Initialized should not throw: {unhandledException}");
			Assert.IsNull(webView.CoreWebView2, "The CoreWebView2Initialized handler should have closed the WebView2.");

			var root = VisualTreeHelper.GetChild(webView, 0) as ContentPresenter;
			Assert.IsNull(root?.Content, "A closed WebView2 should not present an iframe.");
		}
		finally
		{
			Microsoft.UI.Xaml.Application.Current.UnhandledException -= OnUnhandledException;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Closed_From_CoreWebView2Initialized_After_Template_Then_EnsureCoreWebView2Async_Completes()
	{
		var border = new Border { Width = 200, Height = 200 };
		var webView = new WebView2 { Width = 200, Height = 200 };
		webView.CoreWebView2Initialized += (sender, args) => sender.Close();

		try
		{
			await UITestHelper.Load(border);

			// Lay the WebView2 out so it creates its iframe, then call before its Loaded raises CoreWebView2Initialized.
			// This relies on Uno raising Loaded after UpdateLayout returns rather than during it.
			border.Child = webView;
			webView.UpdateLayout();
			_ = GetIframeElementId(webView); // The template is applied now.

			var ensureTask = webView.EnsureCoreWebView2Async().AsTask();
			await TestServices.WindowHelper.WaitFor(
				() => ensureTask.IsCompleted,
				timeoutMS: 5000,
				message: "EnsureCoreWebView2Async should complete when a CoreWebView2Initialized handler closes the WebView2.");

			// As in WinUI, closing the WebView2 from CoreWebView2Initialized doesn't fail the call.
			await ensureTask;
			Assert.IsNull(webView.CoreWebView2, "The CoreWebView2Initialized handler should have closed the WebView2.");

			var root = VisualTreeHelper.GetChild(webView, 0) as ContentPresenter;
			Assert.IsNull(root?.Content, "A closed WebView2 should not present an iframe.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25736")]
	public async Task When_Retemplated_Then_Replaced_Iframe_Is_Removed()
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);
			await NavigateToMarker(webView, "replaced");
			var replacedElementId = GetIframeElementId(webView);

			await Retemplate(webView);

			Assert.AreNotEqual(replacedElementId, GetIframeElementId(webView), "The new template should present a new iframe.");
			Assert.IsFalse(IsInDom(replacedElementId), "The iframe of the replaced template should be removed.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
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

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25735")]
	public async Task When_Navigated_To_Cross_Origin_Content_Then_Navigation_Completes()
	{
		// A data: URL gets an opaque origin, so the browser hides the iframe's location and document from the app.
		const string DataUrl = "data:text/html;base64,PHRpdGxlPkNyb3NzLW9yaWdpbjwvdGl0bGU+PHA+Q3Jvc3Mtb3JpZ2luPC9wPg==";

		var webView = new WebView2 { Width = 200, Height = 200 };
		CoreWebView2NavigationCompletedEventArgs completedArgs = null;
		void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			if (sender.CoreWebView2.Source == DataUrl)
			{
				completedArgs = args;
			}
		}

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			webView.NavigationCompleted += OnNavigationCompleted;

			StartCapturingSecurityErrors();
			webView.CoreWebView2.Navigate(DataUrl);
			await TestServices.WindowHelper.WaitFor(
				() => completedArgs is not null,
				timeoutMS: 10000,
				message: "NavigationCompleted should be raised for the cross-origin document, with Source set to its URL.");

			Assert.IsTrue(completedArgs.IsSuccess);
			Assert.AreEqual(new Uri(DataUrl), webView.Source);
			Assert.AreEqual("", webView.CoreWebView2.DocumentTitle, "The title of a cross-origin document can't be read, so it should be empty.");
			Assert.AreEqual("", StopCapturingSecurityErrors(), "The browser should not report a SecurityError for the cross-origin load.");
		}
		finally
		{
			StopCapturingSecurityErrors();
			webView.NavigationCompleted -= OnNavigationCompleted;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25735")]
	public async Task When_Navigated_To_Sandboxed_Mapped_Content_Then_Navigation_Completes()
	{
		const string MappedUrl = "http://UnoNativeAssets/index.html";
		const string MappedPath = "/WebContent/index.html";

		var webView = new WebView2 { Width = 200, Height = 200 };
		string completedSource = null;
		void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			if (sender.CoreWebView2.Source is { } source && source.EndsWith(MappedPath, StringComparison.Ordinal))
			{
				completedSource = source;
			}
		}

		async Task<string> NavigateToMappedContent(string elementId, string sandbox)
		{
			// The frame's sandbox applies to the documents it loads from then on.
			WasmSemanticDomHelper.InvokeBrowserJs(
				$"(function(){{document.getElementById('{elementId}').setAttribute('sandbox', '{sandbox}');return '';}})()");

			completedSource = null;
			webView.CoreWebView2.Navigate(MappedUrl);
			await TestServices.WindowHelper.WaitFor(
				() => completedSource is not null,
				timeoutMS: 10000,
				message: $"NavigationCompleted should be raised for the mapped content (sandbox='{sandbox}'), with Source set to its URL.");

			return completedSource;
		}

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			webView.CoreWebView2.SetVirtualHostNameToFolderMapping("UnoNativeAssets", "WebContent", CoreWebView2HostResourceAccessKind.Allow);
			webView.NavigationCompleted += OnNavigationCompleted;
			var elementId = GetIframeElementId(webView);
			StartCapturingSecurityErrors();

			// Keeping the origin leaves the document readable (its scripts are off, which this test doesn't need).
			var readableSource = await NavigateToMappedContent(elementId, "allow-same-origin");

			// Without allow-same-origin the document gets an opaque origin, as with a Content-Security-Policy: sandbox header,
			// so the browser hides its location and the URL known from the navigation has to be reported.
			var hiddenSource = await NavigateToMappedContent(elementId, "");

			Assert.AreEqual(readableSource, hiddenSource, "A hidden document should be reported with the same URL as a readable one.");
			Assert.AreEqual(new Uri(readableSource), webView.Source);
			Assert.AreEqual("", StopCapturingSecurityErrors(), "The browser should not report a SecurityError for the sandboxed load.");
		}
		finally
		{
			StopCapturingSecurityErrors();
			webView.NavigationCompleted -= OnNavigationCompleted;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25735")]
	public async Task When_Showing_String_Then_Source_Is_Blank()
	{
		const string EarlierUrl = "data:text/html,earlier";

		var webView = new WebView2 { Width = 200, Height = 200 };
		string lastSource = null;
		void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args) => lastSource = sender.CoreWebView2.Source;

		// Waits for the NavigationCompleted of the navigation that NavigateToString starts, skipping the early one (#25806).
		async Task<string> ShowString(string html)
		{
			ulong? startedId = null;
			string loadedSource = null;
			void OnStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args) => startedId = args.NavigationId;
			void OnCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
			{
				if (args.NavigationId == startedId)
				{
					loadedSource = sender.CoreWebView2.Source;
				}
			}

			webView.NavigationStarting += OnStarting;
			webView.NavigationCompleted += OnCompleted;
			try
			{
				webView.NavigateToString(html);
				await TestServices.WindowHelper.WaitFor(
					() => loadedSource is not null,
					timeoutMS: 10000,
					message: "NavigationCompleted should be raised when the frame loads the string.");

				return loadedSource;
			}
			finally
			{
				webView.NavigationStarting -= OnStarting;
				webView.NavigationCompleted -= OnCompleted;
			}
		}

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var elementId = GetIframeElementId(webView);

			// As in WinUI, a string is reported as about:blank, not as the iframe's about:srcdoc.
			Assert.AreEqual("about:blank", await ShowString("<p>Readable</p>"), "A string should be reported as about:blank.");

			// Without allow-same-origin, the frame's documents get an opaque origin, so the browser hides their location.
			WasmSemanticDomHelper.InvokeBrowserJs(
				$"(function(){{document.getElementById('{elementId}').setAttribute('sandbox', '');return '';}})()");

			webView.NavigationCompleted += OnNavigationCompleted;
			webView.CoreWebView2.Navigate(EarlierUrl);
			await TestServices.WindowHelper.WaitFor(
				() => lastSource == EarlierUrl,
				timeoutMS: 10000,
				message: "NavigationCompleted should be raised for the earlier document, with Source set to its URL.");

			Assert.AreEqual(
				"about:blank",
				await ShowString("<p>Hidden</p>"),
				"A string shown in a sandboxed frame should be reported as about:blank, not with the earlier URL.");
		}
		finally
		{
			webView.NavigationCompleted -= OnNavigationCompleted;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25735")]
	public async Task When_Url_Read_Before_Cross_Origin_Content_Arrives_Then_Its_Url_Is_Reported()
	{
		const string DataUrl = "data:text/html,arriving";

		var webView = new WebView2 { Width = 200, Height = 200 };
		string loadedSource = null;
		void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args) => loadedSource ??= sender.CoreWebView2.Source;

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			var elementId = GetIframeElementId(webView);
			webView.NavigationCompleted += OnNavigationCompleted;

			// Start a navigation to a cross-origin document and, in the same task, read the frame's URL as Loaded does when the
			// WebView2 is added to the tree again before that document arrives.
			WasmSemanticDomHelper.InvokeBrowserJs(
				$"(function(){{var w=Microsoft.UI.Xaml.Controls.WebView;w.navigate('{elementId}','{DataUrl}');w.getCurrentUrl('{elementId}');return '';}})()");

			await TestServices.WindowHelper.WaitFor(
				() => loadedSource is not null,
				timeoutMS: 10000,
				message: "NavigationCompleted should be raised when the cross-origin document loads.");

			Assert.AreEqual(DataUrl, loadedSource, "The cross-origin document should be reported with its own URL, not the previous page's.");
		}
		finally
		{
			webView.NavigationCompleted -= OnNavigationCompleted;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25735")]
	public async Task When_Loaded_Again_Then_Source_Is_Kept()
	{
		// A cross-origin document, whose location the browser hides.
		const string DataUrl = "data:text/html,kept";

		var webView = new WebView2 { Width = 200, Height = 200 };
		var documentLoaded = false;
		void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
			=> documentLoaded |= sender.CoreWebView2.Source == DataUrl;

		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			webView.NavigationCompleted += OnNavigationCompleted;

			webView.CoreWebView2.Navigate(DataUrl);
			await TestServices.WindowHelper.WaitFor(
				() => documentLoaded,
				timeoutMS: 10000,
				message: "NavigationCompleted should be raised for the document, with Source set to its URL.");

			// Unloading and loading the WebView2 again keeps its frame and the document it shows.
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
			await UITestHelper.Load(webView);

			Assert.AreEqual(new Uri(DataUrl), webView.Source, "Loading the WebView2 again should keep the Source of the document it shows.");
		}
		finally
		{
			webView.NavigationCompleted -= OnNavigationCompleted;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<(WeakReference, string)> LoadNavigateAndUnloadWebView2(bool close = false)
	{
		var webView = new WebView2 { Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(webView);

			// A navigated WebView2 registers load and message listeners, which must not keep it alive.
			await NavigateToMarker(webView, "collected");
			var elementId = GetIframeElementId(webView);

			if (close)
			{
				webView.Close();
			}

			return (new WeakReference(webView), elementId);
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

		// On WebAssembly, NavigationCompleted is raised before the queued navigation loads the document.
		var elementId = GetIframeElementId(webView);
		await TestServices.WindowHelper.WaitFor(() => ReadMarker(elementId) == marker, timeoutMS: 10000, message: "The iframe should load the marker.");
	}

	private static async Task Retemplate(WebView2 webView)
	{
		webView.Template = (ControlTemplate)XamlReader.Load(
			"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<ContentPresenter x:Name="WebViewTemplateRoot" />
			</ControlTemplate>
			""");
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Makes the first step of releasing the browser, removing the iframe's listeners, fail.
	private static void MakeBrowserReleaseFail(string elementId)
		=> WasmSemanticDomHelper.InvokeBrowserJs(
			$"(function(){{document.getElementById('{elementId}').removeEventListener=function(){{throw new Error('Simulated failure');}};return '';}})()");

	private static string GetIframeElementId(WebView2 webView)
	{
		var root = VisualTreeHelper.GetChild(webView, 0) as ContentPresenter;
		Assert.AreEqual("WebViewTemplateRoot", root?.Name, "The WebView2 template root should be its ContentPresenter.");

		var element = root.Content as BrowserHtmlElement;
		Assert.IsNotNull(element, "The WebView2 template root should present its iframe as a BrowserHtmlElement.");

		return element.ElementId;
	}

	private static void StartCapturingSecurityErrors()
		=> WasmSemanticDomHelper.InvokeBrowserJs(
			"(function(){var errors=window.__unoTestErrors=[];window.__unoTestOnError=function(e){" +
			"if((e.error&&e.error.name==='SecurityError')||String(e.message).indexOf('SecurityError')>=0){errors.push(String(e.message));}};" +
			"window.addEventListener('error',window.__unoTestOnError);return '';})()");

	private static string StopCapturingSecurityErrors()
		=> WasmSemanticDomHelper.InvokeBrowserJs(
			"(function(){var handler=window.__unoTestOnError;if(!handler){return '';}window.removeEventListener('error',handler);" +
			"var errors=window.__unoTestErrors.join('\\n');delete window.__unoTestOnError;delete window.__unoTestErrors;return errors;})()");

	private static bool IsInDom(string elementId)
		=> WasmSemanticDomHelper.InvokeBrowserJs($"(function(){{return document.getElementById('{elementId}') ? '1' : '0';}})()") == "1";

	private static string ReadMarker(string elementId)
		=> WasmSemanticDomHelper.InvokeBrowserJs(
			$"(function(){{var body = document.getElementById('{elementId}')?.contentDocument?.body; return body ? (body.dataset.marker || '') : '';}})()");
}
#endif
