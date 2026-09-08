#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_WebView2_SourcePort
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Source_Is_Cleared_The_Current_Document_Is_Preserved(bool clearValue)
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
			await NavigateAsync(webView, "preserved-after-clear");
			var core = webView.CoreWebView2;
			var coreSource = core.Source;
			var navigations = 0;
			webView.NavigationStarting += (_, _) => navigations++;

			if (clearValue)
			{
				webView.ClearValue(WebView2.SourceProperty);
			}
			else
			{
				webView.Source = null;
			}
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(webView.Source);
			Assert.AreSame(core, webView.CoreWebView2);
			Assert.AreEqual(coreSource, core.Source);
			Assert.AreEqual(0, navigations);
			Assert.AreEqual("\"preserved-after-clear\"", await webView.ExecuteScriptAsync("document.body.textContent"));
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Focus_Leaves_And_Reenters_The_Browser_Native_Focus_Follows()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		var after = new Button { Content = "After browser" };
		var panel = new StackPanel { Children = { webView, after } };
		try
		{
			await UITestHelper.Load(panel);
			var xamlRoot = webView.XamlRoot;
			Assert.IsNotNull(xamlRoot);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
			await NavigateAsync(webView, "<input value='native focus' />");
			Assert.IsTrue(webView.Focus(FocusState.Keyboard));
			await TestServices.WindowHelper.WaitForIdle();
			var browserFocus = GetNativeFocus();
			var root = GetNativeAncestor(browserFocus, 2);
			Assert.AreNotEqual(nint.Zero, browserFocus);
			Assert.AreNotEqual(nint.Zero, root);
			Assert.AreNotEqual(root, browserFocus);

			var options = new FindNextElementOptions { SearchRoot = xamlRoot.Content };
			var next = await FocusManager.TryMoveFocusAsync(FocusNavigationDirection.Next, options);
			Assert.IsTrue(next.Succeeded);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreSame(after, FocusManager.GetFocusedElement(xamlRoot));
			Assert.AreEqual(root, GetNativeFocus(), "Keyboard input must return to the managed window, not remain in Chromium.");

			var previous = await FocusManager.TryMoveFocusAsync(FocusNavigationDirection.Previous, options);
			Assert.IsTrue(previous.Succeeded);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreSame(webView, FocusManager.GetFocusedElement(xamlRoot));
			Assert.AreNotEqual(root, GetNativeFocus());
			Assert.AreEqual(root, GetNativeAncestor(GetNativeFocus(), 2));
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Initialized_After_Layout_The_Viewport_Matches_The_Control()
	{
		var webView = new WebView2 { Width = 321, Height = 123 };
		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
			await NavigateAsync(webView, "viewport");
			var width = ReadJsonNumber(await webView.ExecuteScriptAsync("innerWidth"));
			var height = ReadJsonNumber(await webView.ExecuteScriptAsync("innerHeight"));
			Assert.AreEqual(webView.ActualWidth, width, 1d);
			Assert.AreEqual(webView.ActualHeight, height, 1d);

			webView.Width = 411;
			webView.Height = 177;
			await TestServices.WindowHelper.WaitForIdle();
			for (var attempt = 0; attempt < 100; attempt++)
			{
				width = ReadJsonNumber(await webView.ExecuteScriptAsync("innerWidth"));
				height = ReadJsonNumber(await webView.ExecuteScriptAsync("innerHeight"));
				if (Math.Abs(webView.ActualWidth - width) <= 1 && Math.Abs(webView.ActualHeight - height) <= 1)
				{
					break;
				}
				await Task.Delay(50);
			}
			Assert.AreEqual(webView.ActualWidth, width, 1d);
			Assert.AreEqual(webView.ActualHeight, height, 1d);
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Initialized_The_Control_Does_Not_Request_Global_Animation_Frames()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
#if HAS_UNO
			var field = typeof(CompositionTarget).GetField("_rendering", BindingFlags.Static | BindingFlags.NonPublic);
			Assert.IsNotNull(field);
			var subscriptions = (Delegate?)field.GetValue(null);
			Assert.IsFalse(subscriptions?.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, webView)) == true);
#endif
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Initialized_Before_Attachment_The_Core_Is_Usable()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			Assert.IsNull(webView.CoreWebView2);
			var environment = await CreateEnvironmentAsync();
			Assert.IsFalse(string.IsNullOrEmpty(environment.BrowserVersionString));
			Assert.IsTrue(Path.IsPathFullyQualified(environment.UserDataFolder));
			Console.WriteLine($"WebView2 native environment: {environment.BrowserVersionString}; {environment.UserDataFolder}.");

			var initialized = 0;
			webView.CoreWebView2Initialized += (_, args) =>
			{
				Assert.IsNull(args.Exception);
				Assert.IsNotNull(webView.CoreWebView2);
				initialized++;
			};
			await webView.EnsureCoreWebView2Async(environment);
			Assert.IsFalse(webView.IsLoaded);
			Assert.IsNotNull(webView.CoreWebView2);
			Assert.AreEqual(1, initialized);
			Assert.AreEqual("2", await webView.ExecuteScriptAsync("1 + 1"));

			var core = webView.CoreWebView2;
			await UITestHelper.Load(webView);
			Assert.AreSame(core, webView.CoreWebView2);
			await NavigateAsync(webView, "attached");
			Assert.AreEqual("\"attached\"", await webView.ExecuteScriptAsync("document.body.textContent"));
			Assert.AreEqual(1, initialized);
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Ensure_Is_Not_Awaited_Source_Resumes_On_The_UI_Thread()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			var environment = await CreateEnvironmentAsync();
			var initialization = webView.EnsureCoreWebView2Async(environment);
			var navigation = NavigateAsync(webView, "concurrent-source");
			await initialization;
			await navigation;
			Assert.IsTrue(webView.DispatcherQueue.HasThreadAccess);
			Assert.AreEqual("\"concurrent-source\"", await webView.ExecuteScriptAsync("document.body.textContent"));
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_An_Environment_Is_Shared_Closing_One_Control_Does_Not_Close_The_Other()
	{
		var first = new WebView2 { Width = 320, Height = 180 };
		var second = new WebView2 { Width = 320, Height = 180 };
		try
		{
			var environment = await CreateEnvironmentAsync();
			await UITestHelper.Load(new StackPanel { Children = { first, second } });
			var firstInitialization = first.EnsureCoreWebView2Async(environment);
			var secondInitialization = second.EnsureCoreWebView2Async(environment);
			await firstInitialization;
			await secondInitialization;
			Assert.AreSame(environment, first.CoreWebView2.Environment);
			Assert.AreSame(environment, second.CoreWebView2.Environment);
			Assert.AreEqual(first.CoreWebView2.BrowserProcessId, second.CoreWebView2.BrowserProcessId);

			var version = environment.BrowserVersionString;
			first.Close();
			Assert.AreEqual(version, environment.BrowserVersionString);
			await NavigateAsync(second, "still-alive");
			Assert.AreEqual("\"still-alive\"", await second.ExecuteScriptAsync("document.body.textContent"));
			await second.EnsureCoreWebView2Async(environment);
		}
		finally
		{
			first.Close();
			second.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Controller_Creation_Fails_A_Later_Ensure_Can_Retry()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			var environment = await CreateEnvironmentAsync();
			var invalidOptions = environment.CreateCoreWebView2ControllerOptions();
			invalidOptions.ProfileName = "/";
			var count = 0;
			Exception? error = null;
			webView.CoreWebView2Initialized += (_, args) =>
			{
				count++;
				error = args.Exception;
			};

			await webView.EnsureCoreWebView2Async(environment, invalidOptions);
			Assert.AreEqual(1, count);
			Assert.IsNotNull(error);
			Assert.IsNull(webView.CoreWebView2);

			await webView.EnsureCoreWebView2Async(environment);
			Assert.AreEqual(2, count);
			Assert.IsNull(error);
			Assert.IsNotNull(webView.CoreWebView2);
			await webView.EnsureCoreWebView2Async(environment);
			await NavigateAsync(webView, "retried");
			Assert.AreEqual("\"retried\"", await webView.ExecuteScriptAsync("document.body.textContent"));
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_UserAgent_Is_Restored_The_Browser_Default_Is_Used()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
			var settings = webView.CoreWebView2.Settings;
			var defaultUserAgent = settings.UserAgent;
			Assert.IsFalse(string.IsNullOrEmpty(defaultUserAgent));
			Assert.AreEqual(defaultUserAgent, ReadJsonString(await webView.ExecuteScriptAsync("navigator.userAgent")));

			settings.UserAgent = "Uno-WebView2-SourcePort";
			await NavigateAsync(webView, "custom-ua");
			Assert.AreEqual("\"Uno-WebView2-SourcePort\"", await webView.ExecuteScriptAsync("navigator.userAgent"));
			settings.UserAgent = string.Empty;
			Assert.AreEqual("Uno-WebView2-SourcePort", settings.UserAgent);

			settings.UserAgent = defaultUserAgent;
			await NavigateAsync(webView, "restored-ua");
			Assert.AreEqual(defaultUserAgent, settings.UserAgent);
			Assert.AreEqual(defaultUserAgent, ReadJsonString(await webView.ExecuteScriptAsync("navigator.userAgent")));
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Reattached_And_Retemplated_The_Core_And_Document_Are_Preserved()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		var holder = new Border { Child = webView };
		try
		{
			var initialized = 0;
			webView.CoreWebView2Initialized += (_, _) => initialized++;
			await UITestHelper.Load(holder);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
			await NavigateAsync(webView, "preserved");
			var core = webView.CoreWebView2;
			var process = core.BrowserProcessId;

			for (var cycle = 0; cycle < 3; cycle++)
			{
				holder.Child = null;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsFalse(webView.IsLoaded);
				holder.Child = webView;
				await TestServices.WindowHelper.WaitForLoaded(webView);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreSame(core, webView.CoreWebView2);
				Assert.AreEqual(process, core.BrowserProcessId);
				Assert.AreEqual("\"preserved\"", await webView.ExecuteScriptAsync("document.body.textContent"));
			}
#if HAS_UNO
			webView.Template = new ControlTemplate(null, static (_, _) =>
			{
				var presenter = new ContentPresenter { Name = "WebViewTemplateRoot" };
				var names = new NameScope();
				names.RegisterName(presenter.Name, presenter);
				NameScope.SetNameScope(presenter, names);
				return presenter;
			});
			webView.ApplyTemplate();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(webView.GetNativePresenter()?.IsLoaded == true);
#endif
			Assert.AreSame(core, webView.CoreWebView2);
			Assert.AreEqual("\"preserved\"", await webView.ExecuteScriptAsync("document.body.textContent"));
			Assert.AreEqual(1, initialized);

			var messages = 0;
			webView.WebMessageReceived += (_, _) => messages++;
			await webView.ExecuteScriptAsync("chrome.webview.postMessage('once')");
			await TestServices.WindowHelper.WaitFor(() => messages != 0, 10_000);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, messages);

			var source = webView.Source;
			webView.Close();
			webView.Close();
			Assert.IsNull(webView.CoreWebView2);
			Assert.AreEqual(source, webView.Source);
			Assert.IsFalse(webView.CanGoBack);
			Assert.IsFalse(webView.CanGoForward);
			webView.GoBack();
			webView.GoForward();
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_The_Browser_Process_Exits_The_Core_Can_Be_Recreated()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			var environment = await CreateEnvironmentAsync();
			var initialized = 0;
			webView.CoreWebView2Initialized += (_, _) => initialized++;
			await webView.EnsureCoreWebView2Async(environment);
			await NavigateAsync(webView, "before-browser-failure");
			var previousCore = webView.CoreWebView2;
			var previousProcess = previousCore.BrowserProcessId;
			CoreWebView2ProcessFailedEventArgs? failure = null;
			webView.CoreProcessFailed += (_, args) =>
			{
				if (args.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
				{
					failure = args;
					Assert.IsNull(webView.CoreWebView2);
				}
			};

			// The pinned MUX CoreObjects test uses this URI. A unique user-data folder isolates this browser.
			previousCore.Navigate("edge://inducebrowsercrashforrealz/");
			await TestServices.WindowHelper.WaitFor(() => failure is not null, 30_000);
			Assert.IsFalse(webView.CanGoBack);
			Assert.IsFalse(webView.CanGoForward);

			await webView.EnsureCoreWebView2Async();
			Assert.IsNotNull(webView.CoreWebView2);
			Assert.AreNotSame(previousCore, webView.CoreWebView2);
			Assert.AreSame(environment, webView.CoreWebView2.Environment);
			Assert.AreNotEqual(previousProcess, webView.CoreWebView2.BrowserProcessId);
			Assert.AreEqual(2, initialized);
			await NavigateAsync(webView, "recreated");
			Assert.AreEqual("\"recreated\"", await webView.ExecuteScriptAsync("document.body.textContent"));
			Console.WriteLine($"WebView2 browser recreation: {previousProcess} -> {webView.CoreWebView2.BrowserProcessId}; {failure!.Reason}, exit {failure.ExitCode}.");
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_The_Renderer_Process_Exits_Reload_Recovers_Without_Replacing_The_Core()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async(await CreateEnvironmentAsync());
			await NavigateAsync(webView, "renderer-recovered");
			var core = webView.CoreWebView2;
			var failed = false;
			webView.CoreProcessFailed += (_, args) =>
				failed |= args.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited;
			core.Navigate("edge://crash");
			await TestServices.WindowHelper.WaitFor(() => failed, 30_000);
			Assert.AreSame(core, webView.CoreWebView2);

			var reloaded = false;
			webView.NavigationCompleted += (_, args) => reloaded |= args.IsSuccess;
			webView.Reload();
			await TestServices.WindowHelper.WaitFor(() => reloaded, 20_000);
			Assert.AreSame(core, webView.CoreWebView2);
			Assert.AreEqual("\"renderer-recovered\"", await webView.ExecuteScriptAsync("document.body.textContent"));
		}
		finally
		{
			webView.Close();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
	{
		var root = Environment.GetEnvironmentVariable("UNO_WEBVIEW2_TEST_USER_DATA_ROOT")
			?? Path.Combine(Environment.CurrentDirectory, "webview2-test-profiles");
		return await CoreWebView2Environment.CreateWithOptionsAsync(
			null, Path.Combine(root, "webview2-" + Guid.NewGuid().ToString("N")), new CoreWebView2EnvironmentOptions());
	}

	[DllImport("user32.dll", EntryPoint = "GetFocus")]
	private static extern nint GetNativeFocus();

	[DllImport("user32.dll", EntryPoint = "GetAncestor")]
	private static extern nint GetNativeAncestor(nint window, uint flags);

	private static double ReadJsonNumber(string json)
	{
		using var document = JsonDocument.Parse(json);
		return document.RootElement.GetDouble();
	}

	private static string? ReadJsonString(string json)
	{
		using var document = JsonDocument.Parse(json);
		return document.RootElement.GetString();
	}

	private static async Task NavigateAsync(WebView2 webView, string content)
	{
		var uri = new Uri("data:text/html;base64," + Convert.ToBase64String(
			Encoding.UTF8.GetBytes($"<html><body>{content}</body></html>")));
		var completion = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>();
		TypedEventHandler<WebView2, CoreWebView2NavigationCompletedEventArgs> handler = (_, args) =>
		{
			if (Equals(webView.Source, uri))
			{
				completion.TrySetResult(args);
			}
		};
		webView.NavigationCompleted += handler;
		try
		{
			webView.Source = uri;
			var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
			Assert.IsTrue(result.IsSuccess, result.WebErrorStatus.ToString());
		}
		finally
		{
			webView.NavigationCompleted -= handler;
		}
	}
}
