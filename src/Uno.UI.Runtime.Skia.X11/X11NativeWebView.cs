using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using SharpWebview;
using SharpWebview.Content;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeWebViewProvider(CoreWebView2 coreWebView2) : INativeWebViewProvider
{
	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter) => new X11NativeWebView(coreWebView2, contentPresenter);
}

internal class X11NativeWebView : INativeWebView
{
	// Webview doesn't naturally support multiple windows and constructing any Webview after the first will block.
	// On X11, Webview wraps GTK: https://github.com/webview/webview/blob/6e847a6efe88a15edf58c26b8c9ea933ba2569ec/webview.h#L1951-L1957
	// Due to the way GTK works, we need to create the second Webview from GTK's main thread. Run() calls g_main_context_iteration in a loop,
	// so we should only need to have one Run() function running for all Webviews combined.
	// To create multiple Webviews, we need a "master" webview that is in charge of the Run() loop. The first webview created is this
	// master Webview
	private static Webview? _masterWebview;

	private int _jsInvokeCounter;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string?>> _jsInvokeTasks = new();

	private Webview _nativeWebview;

	private readonly CoreWebView2 _coreWebView;
	private readonly ContentPresenter _presenter;
	private readonly string _title = $"Uno WebView {Random.Shared.Next()}";

	public X11NativeWebView(CoreWebView2 coreWebView2, ContentPresenter presenter)
	{
		_coreWebView = coreWebView2;
		_presenter = presenter;

		var host = (X11XamlRootHost)X11Manager.XamlRootMap.GetHostForRoot(presenter.XamlRoot!)!;
		_nativeWebview = null!; // to work around nullability;
		if (_masterWebview is { })
		{
			_masterWebview.Dispatch(() => Init(false));
		}
		else
		{
			new Thread(() => Init(true)).Start();
			SpinWait.SpinUntil(() => _nativeWebview is not null);
			_masterWebview = _nativeWebview;
		}

		// Unfortunately, there's a split second where the new window spawns and is visible before being attached to the
		// Uno window. The API doesn't allow us to open a hidden window. We would have to talk to gtk directly.
		_ = Task.Run(() =>
		{
			var window = X11NativeElementHostingExtension.FindWindowByTitle(host, _title, TimeSpan.MaxValue);
			_ = _coreWebView.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				_presenter.Content = new X11NativeWindow(window);
			});
		});

		SpinWait.SpinUntil(() => _nativeWebview is not null);

		_presenter.SizeChanged += (_, args) =>
		{
			_nativeWebview.Dispatch(() => _nativeWebview.SetSize((int)args.NewSize.Width, (int)args.NewSize.Height, WebviewHint.None));
		};
	}

	private void Init(bool runLoop)
	{
		// WARNING: The threading for Webview is fragile. If you move the Webview construction outside
		// of the new thread, it won't work. Be careful about moving anything around.
#if DEBUG
		_nativeWebview = new Webview(true);
#else
		_nativeWebview = new Webview(false);
#endif
		_nativeWebview.SetSize(1, 1, WebviewHint.None);
		_nativeWebview.SetTitle(_title);

		_nativeWebview.Bind("onSourceChanged", OnSourceChanged);
		_nativeWebview.Bind("onScriptInvocationDone", OnScriptInvocationDone);
		_nativeWebview.Bind("onDocumentTitleChanged", OnDocumentTitleChanged);
		_nativeWebview.Bind("onDocumentLoaded", OnDocumentLoaded);
		// from https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.navigationcompleted?view=webview2-dotnet-1.0.2478.35#microsoft-web-webview2-core-corewebview2-navigationcompleted
		// "NavigationCompleted is raised when the WebView has completely loaded (body.onload has been raised) or loading stopped with error."
		_nativeWebview.InitScript(
			"""
			onSourceChanged({ 'url': window.location.href });

			document.addEventListener("DOMContentLoaded", (event) => {
				onDocumentTitleChanged(document.title);

				new MutationObserver(function(mutations) {
					onDocumentTitleChanged(document.title);
				}).observe(document, { attributes: true });
			});

			document.onload = (event) => { onDocumentLoaded(); };
			""");

		if (runLoop)
		{
			_nativeWebview.Run();
		}
	}

	~X11NativeWebView()
	{
		// don't dispose the master Webview because it's keeping the Run loop alive
		if (_nativeWebview != _masterWebview)
		{
			_nativeWebview.Dispatch(_nativeWebview.Dispose);
		}
	}

	public string DocumentTitle { get; private set; } = "";

	private void OnSourceChanged(string id, string req)
	{
		var rootNode = JsonSerializer.Deserialize<JsonNode>(req);
		var urlNode = rootNode?[0]?["url"];
		if (urlNode?.GetValue<string>() is { } url)
		{
			_ = _coreWebView.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				_coreWebView.Source = url;
			});
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(OnSourceChanged)} called with invalid json.");
			}
		}
	}

	private void OnScriptInvocationDone(string _, string req)
	{
		var rootNode = JsonSerializer.Deserialize<JsonNode>(req);
		var objNode = rootNode?[0];
		var idNode = objNode?["id"];
		var resultNode = objNode?["result"];
		if (idNode?.ToString() is { } id && resultNode?.ToString() is { } result)
		{
			_jsInvokeTasks.TryRemove(id, out var tcs);
			tcs?.TrySetResult(result);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(OnSourceChanged)} called with invalid json.");
			}
		}
	}

	private void OnDocumentTitleChanged(string id, string req)
	{
		var rootNode = JsonSerializer.Deserialize<JsonNode>(req);
		var titleNode = rootNode?[0];

		if (titleNode?.ToString() is { } title)
		{
			_ = _coreWebView.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				DocumentTitle = title;
				_coreWebView.OnDocumentTitleChanged();
			});
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(OnDocumentTitleChanged)} called with invalid json.");
			}
		}
	}

	// Note: this only works on successful page loads. The Webview API doesn't support intercepting errors
	// or status codes in general.
	private void OnDocumentLoaded(string id, string req)
	{
		_ = _coreWebView.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
		{
			_coreWebView.RaiseNavigationCompleted(new Uri(_coreWebView.Source), true, 200, CoreWebView2WebErrorStatus.Unknown);
		});
	}

	public void GoBack() => _nativeWebview.Evaluate("history.back();");
	public void GoForward() => _nativeWebview.Evaluate("history.forward();");
	public void Stop() => _nativeWebview.Evaluate("window.stop();");
	public void Reload() => _nativeWebview.Evaluate("window.location.reload();");

	private void ScheduleNavigationStarting(Uri uri, Action loadAction)
	{
		_coreWebView.RaiseNavigationStarting(uri, out var cancel);

		if (!cancel)
		{
			loadAction.Invoke();
		}
	}

	private void ScheduleNavigationStarting(string html, Action loadAction)
	{
		_coreWebView.RaiseNavigationStarting(html, out var cancel);

		if (!cancel)
		{
			loadAction.Invoke();
		}
	}

	// Note: Webview doesn't support web Navigation APIs, so we cannot set CoreWebView2.CanGo<Back|Forward>
	public void ProcessNavigation(Uri uri) => ScheduleNavigationStarting(uri, () =>
	{
		_nativeWebview.Dispatch(() => _nativeWebview.Navigate(new UrlContent(uri.AbsoluteUri)));
	});
	public void ProcessNavigation(string html) => ScheduleNavigationStarting(html, () =>
	{
		_nativeWebview.Dispatch(() => _nativeWebview.Navigate(new HtmlContent(html)));
	});
	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(ProcessNavigation)} is not supported on the X11 target.");
		}
	}

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		// we add an eval call so that we can get a return value even if `script` is not an expression.
		return InvokeScriptAsync("eval", new[] { script }, token);
	}

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		var tcs = new TaskCompletionSource<string?>();
		var jsInvokeId = Interlocked.Increment(ref _jsInvokeCounter).ToString(CultureInfo.InvariantCulture);

		// JsonSerializer.Serialize safely escapes quotes and concatenates the arguments (with a comma) to be passed to eval
		// the [1..^1] part is to remove [ and ].
		var argumentString = arguments is not null ? JsonSerializer.Serialize(arguments)[1..^1] : "";

		_nativeWebview.Dispatch(() => _nativeWebview.Evaluate($"onScriptInvocationDone({{ id: {jsInvokeId}, result: {script}({argumentString}) }} );"));
		_jsInvokeTasks.TryAdd(jsInvokeId, tcs);

		token.Register(() =>
		{
			_jsInvokeTasks.TryRemove(jsInvokeId, out var tcs_);
			tcs_?.TrySetCanceled();
		});

		return tcs.Task;
	}

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(SetScrollingEnabled)} is not supported on the X11 target.");
		}
	}
}
