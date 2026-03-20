#nullable enable

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.ApplicationModel.Resources;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal partial class MacOSNativeWebView : MacOSNativeElement, INativeWebView
{
	private readonly MacOSWindowNative _window;
	private readonly CoreWebView2 _owner;
	private readonly nint _webview;
	private string _previousTitle;
	private bool _isHistoryChangeQueued;
	private bool _isCancelling;
	private string? _lastHtmlContent;

	private const string OkResourceKey = "WebView_Ok";
	private const string CancelResourceKey = "WebView_Cancel";

	private readonly string OkString;
	private readonly string CancelString;

	public MacOSNativeWebView(MacOSWindowNative window, CoreWebView2 owner)
	{
		_window = window;
		_owner = owner;

		// logic adapted from uno/src/Uno.UI/UI/Xaml/Controls/WebView/Native/iOSmacOS/UnoWKWebView.iOSmacOS.cs
		var resourceLoader = ResourceLoader.GetForCurrentView();
		var ok = resourceLoader.GetString("OkResourceKey");
		var cancel = resourceLoader.GetString("CancelResourceKey");

		if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en")
		{
			if (ok == $"[{OkResourceKey}]")
			{
				ok = "OK";
			}
			if (cancel == $"[{CancelResourceKey}]")
			{
				cancel = "Cancel";
			}
		}

		// Set strings with fallback to default English
		OkString = !string.IsNullOrEmpty(ok) ? ok : "OK";
		CancelString = !string.IsNullOrEmpty(cancel) ? cancel : "Cancel";

		_webview = NativeUno.uno_webview_create(_window.Handle, OkString, CancelString);
		NativeHandle = _webview;

		Unloaded += (s, e) =>
		{
			_webViews.Remove(NativeHandle);
		};

		_webViews.Add(NativeHandle, new WeakReference<MacOSNativeWebView>(this));

		_previousTitle = "";
	}

	public string DocumentTitle => NativeUno.uno_webview_get_title(_webview);

	public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		var executedScript = string.Format(CultureInfo.InvariantCulture, "javascript:{0}", script);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"ExecuteScriptAsync: {executedScript}");
		}

		var tcs = new TaskCompletionSource<string>();
		using (token.Register(() => tcs.TrySetCanceled()))
		{
			var handle = GCHandle.Alloc(tcs);
			NativeUno.uno_webview_execute_script(_webview, GCHandle.ToIntPtr(handle), script);
		}

		return await tcs.Task;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	static internal unsafe void ExecuteScriptCallback(IntPtr handle, sbyte* result, sbyte* error)
	{
		var gch = GCHandle.FromIntPtr(handle);
		var tcs = gch.Target as TaskCompletionSource<string?>;
		if (tcs is null)
		{
			typeof(MacOSNativeWebView).Log().Error("GCHandle returning a null TaskCompletionSource");
		}
		else if (error is not null)
		{
			tcs.TrySetException(new InvalidOperationException(new string(error)));
		}
		else
		{
			var s = result == null ? null : new string(result);
			tcs.TrySetResult(s);
		}
		gch.Free();
	}

	public void GoBack() => NativeUno.uno_webview_go_back(_webview);

	public void GoForward() => NativeUno.uno_webview_go_forward(_webview);

	public async Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		var javascript = string.Empty;
		if (arguments is null || arguments.Length == 0)
		{
			javascript = "javascript:" + script;
		}
		else
		{
			var argumentString = WebView.ConcatenateJavascriptArguments(arguments);
			javascript = string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\")", script, argumentString);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"InvokeScriptAsync: {javascript}");
		}

		var tcs = new TaskCompletionSource<string?>();
		using (token.Register(() => tcs.TrySetCanceled()))
		{
			var handle = GCHandle.Alloc(tcs);
			NativeUno.uno_webview_invoke_script(_webview, GCHandle.ToIntPtr(handle), javascript);
			return await tcs.Task;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	// note: cannot use `string` in the signature since it's not blittable
	static internal unsafe void InvokeScriptCallback(nint handle, sbyte* result, sbyte* error)
	{
		var gch = GCHandle.FromIntPtr(handle);
		var tcs = gch.Target as TaskCompletionSource<string?>;
		if (tcs is null)
		{
			typeof(MacOSNativeWebView).Log().Error("GCHandle returning a null TaskCompletionSource");
		}
		else if (error is not null)
		{
			tcs.TrySetException(new InvalidOperationException(new string(error)));
		}
		else
		{
			var s = result == null ? null : new string(result);
			tcs.TrySetResult(s);
		}
		gch.Free();
	}

	public void ProcessNavigation(Uri uri)
	{
		_lastHtmlContent = null;
		string? url = null;

		if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
		{
			var baseUrl = NativeUno.uno_application_is_bundled() ? "[BundlePath]" : AppDomain.CurrentDomain.BaseDirectory;
			url = $"file://{baseUrl}{uri.PathAndQuery}";
		}
		else if (_owner.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			var relativePath = uri.PathAndQuery;
			var sep = relativePath.StartsWith('/') ? "" : "/";
			var baseUrl = NativeUno.uno_application_is_bundled() ? "[ResourcePath]" : AppDomain.CurrentDomain.BaseDirectory;
			url = $"file://{baseUrl}{folderName}{sep}{relativePath}";
		}
		else
		{
			url = uri.AbsoluteUri;
		}

		NativeUno.uno_webview_navigate(_webview, url, null);
	}

	public void ProcessNavigation(string html)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"LoadHtmlString: {html}");
		}

		_lastHtmlContent = html;
		NativeUno.uno_webview_load_html(_webview, html);
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
		_lastHtmlContent = null;
		if (httpRequestMessage == null)
		{
			this.Log().Warn("HttpRequestMessage is null. Please make sure the http request is complete.");
			return;
		}

		var url = httpRequestMessage.RequestUri?.ToString();
		if (url is not null)
		{
			var headers = JsonSerializer.Serialize(httpRequestMessage.Headers);
			NativeUno.uno_webview_navigate(_webview, url, headers);
		}
	}

	public void Reload()
	{
		if (_lastHtmlContent != null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Reloading cached HTML content");
			}
			NativeUno.uno_webview_load_html(_webview, _lastHtmlContent);
		}
		else
		{
			NativeUno.uno_webview_reload(_webview);
		}
	}

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
		NativeUno.uno_webview_set_scrolling_enabled(_webview, isScrollingEnabled);
	}

	public void Stop() => NativeUno.uno_webview_stop(_webview);

	private static readonly Dictionary<nint, WeakReference<MacOSNativeWebView>> _webViews = [];

	private static MacOSNativeWebView? GetWebView(nint handle)
	{
		if (_webViews.TryGetValue(handle, out var weak))
		{
			weak.TryGetTarget(out var webview);
			return webview;
		}

		if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSNativeWebView).Log().Error($"Could not map handle 0x{handle:X} to a managed MacOSNativeWebView");
		}
		return null;
	}

	private void SetHistoryProperties()
	{
		_owner.SetHistoryProperties(
			NativeUno.uno_webview_can_go_back(_webview),
			NativeUno.uno_webview_can_go_forward(_webview));
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe int NavigationStartingCallback(nint handle, sbyte* url)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			var s = url == null ? null : new string(url);
			if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
			{
				webview._isCancelling = false;
				webview.SetHistoryProperties();
				webview._owner.RaiseNavigationStarting(uri, out var cancel);

				if (cancel)
				{
					webview._isCancelling = true;
					webview.Stop();
				}
				return cancel ? 0 : 1;
			}
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.NavigationStartingCallback could not map 0x{handle:X} with an WKWebView");
		}
		return 1;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe void NavigationFinishingCallback(nint handle, sbyte* url)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			webview.SetHistoryProperties();
			webview.QueueHistoryChange();

			webview.CheckForTitleChange();

			var s = url == null ? null : new string(url);
			if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
			{
				webview._owner.RaiseNavigationCompleted(uri, isSuccess: true, httpStatusCode: 200, errorStatus: CoreWebView2WebErrorStatus.Unknown, shouldSetSource: true);
			}
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.NavigationCompletedCallback could not map 0x{handle:X} with an WKWebView");
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe void NavigationFailingCallback(nint handle, sbyte* url, CoreWebView2WebErrorStatus status)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			if (status != CoreWebView2WebErrorStatus.OperationCanceled && !webview._isCancelling)
			{
				var s = url == null ? null : new string(url);
				Uri.TryCreate(s, UriKind.Absolute, out var uri);
				// url might be null
				webview._owner.RaiseNavigationCompleted(uri, isSuccess: false, httpStatusCode: 0, errorStatus: CoreWebView2WebErrorStatus.Unknown, shouldSetSource: true);
			}
			else
			{
				webview._isCancelling = false;
			}
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.NavigationFailingCallback could not map 0x{handle:X} with an WKWebView");
		}
	}

	private void QueueHistoryChange()
	{
		if (!_isHistoryChangeQueued)
		{
			_isHistoryChangeQueued = true;
			MacOSDispatcher.DispatchNativeSingle(RaiseQueuedHistoryChange, Dispatching.NativeDispatcherPriority.Normal);
		}
	}

	private void RaiseQueuedHistoryChange()
	{
		_owner.RaiseHistoryChanged();
		_isHistoryChangeQueued = false;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe void DidChangeValue(nint handle, long key)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			switch (key)
			{
				case 0: // Title
					webview.CheckForTitleChange();
					break;
				case 1: // URL
				case 2: // CanGoBack
				case 3: // CanGoForward
					webview.SetHistoryProperties();
					webview.QueueHistoryChange();
					break;
				default:
					typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.DidChangeValue could not map key {key} to a property of WKWebView");
					break;
			}
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.NavigationCompletedCallback could not map 0x{handle:X} with an WKWebView");
		}
	}

	private void CheckForTitleChange()
	{
		var currentTitle = DocumentTitle;
		if (_previousTitle != currentTitle)
		{
			_previousTitle = currentTitle;
			_owner.OnDocumentTitleChanged();
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe int NewWindowRequestedCallback(nint handle, sbyte* targetUrl, sbyte* refererUrl)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			var targetString = targetUrl == null ? "about:blank" : new string(targetUrl);
			var refererString = refererUrl == null ? null : new string(refererUrl);

			if (refererString == null || !Uri.TryCreate(refererString, UriKind.Absolute, out var refererUri))
			{
				if (refererString != null && typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.NewWindowRequestedCallback: Invalid referer URI '{refererString}', using about:blank");
				}
				refererUri = new Uri("about:blank");
			}

			if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(MacOSNativeWebView).Log().Debug($"MacOSNativeWebView.NewWindowRequestedCallback: Target='{targetString}', Referer='{refererUri}'");
			}

			webview._owner.RaiseNewWindowRequested(
				targetString,
				refererUri,
				out var handled);

			if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(MacOSNativeWebView).Log().Debug($"MacOSNativeWebView.NewWindowRequestedCallback: Handled={handled}");
			}

			// Return 1 if handled (which prevents the native code from opening a new window),
			// or 0 if not handled (allowing the native code to proceed, e.g., opening in an external browser).
			return handled ? 1 : 0;
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.NewWindowRequestedCallback could not map 0x{handle:X} with an WKWebView");
		}

		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe void DidReceiveScriptMessage(nint handle, sbyte* messageBody)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			var message = messageBody == null ? "" : new string(messageBody);
			webview._owner.RaiseWebMessageReceived(message);
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.DidReceiveScriptMessage could not map 0x{handle:X} with an WKWebView");
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe int OnUnsupportedUriSchemeIdentified(nint handle, sbyte* url)
	{
		var webview = GetWebView(handle);
		if (webview is not null)
		{
			var s = url == null ? null : new string(url);
			if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
			{
				webview._owner.RaiseUnsupportedUriSchemeIdentified(uri, out var handled);
				return handled ? 1 : 0;
			}
			else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.OnUnsupportedUriSchemeIdentified given a malformed URL '{s}'.");
			}
		}
		else if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSNativeWebView).Log().Warn($"MacOSNativeWebView.OnUnsupportedUriSchemeIdentified could not map 0x{handle:X} with an WKWebView");
		}
		return 0;
	}
}
