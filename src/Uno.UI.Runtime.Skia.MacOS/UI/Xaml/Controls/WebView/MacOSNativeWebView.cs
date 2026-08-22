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

internal partial class MacOSNativeWebView : MacOSNativeElement, ICleanableNativeWebView
{
	private readonly MacOSWindowNative _window;
	private readonly CoreWebView2 _owner;
	private string _previousTitle;
	private bool _isHistoryChangeQueued;
	private bool _isCancelling;
	private string? _lastHtmlContent;
	private nint _registeredHandle;

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

		NativeHandle = NativeUno.uno_webview_create(_window.Handle, OkString, CancelString);

		NativeUno.uno_webview_set_inspectable(NativeHandle, global::Uno.UI.FeatureConfiguration.WebView2.EnableDevTools);

		_previousTitle = "";
	}

	/// <summary>
	/// Resolves the native <c>WKWebView</c> handle, refusing to hand a disposed peer to native code.
	/// </summary>
	/// <remarks>
	/// The peer is destroyed when the element leaves the visual tree, which zeroes
	/// <see cref="MacOSNativeElement.NativeHandle"/>. Messaging the freed <c>WKWebView</c> would make ARC
	/// retain deallocated memory and take down the whole process, so each operation has to fail on its own.
	/// </remarks>
	private bool TryGetHandle(string operation, out nint handle)
	{
		handle = NativeHandle;

		if (Disposed || handle == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Cannot {operation} a WebView2 whose native peer was already disposed.");
			}

			return false;
		}

		return true;
	}

	void ICleanableNativeWebView.OnLoaded()
	{
		// A zeroed handle would key the map at 0, which every disposed instance would then share.
		if (TryGetHandle("register the message handler of", out var handle))
		{
			_registeredHandle = handle;
			_webViews[handle] = new WeakReference<MacOSNativeWebView>(this);
			NativeUno.uno_webview_register_message_handler(handle);
		}
	}

	void ICleanableNativeWebView.OnUnloaded()
	{
		// Removed by the remembered key, not by NativeHandle: the unload order between the WebView2 and
		// its native element is undefined, so the peer may already be disposed and the handle zeroed.
		if (_registeredHandle != 0)
		{
			_webViews.Remove(_registeredHandle);
			_registeredHandle = 0;
		}
	}

	public string DocumentTitle => TryGetHandle("read the document title of", out var handle)
		? NativeUno.uno_webview_get_title(handle)
		: "";

	public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		var executedScript = string.Format(CultureInfo.InvariantCulture, "javascript:{0}", script);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"ExecuteScriptAsync: {executedScript}");
		}

		if (!TryGetHandle("execute a script on", out var webview))
		{
			return null;
		}

		var tcs = new TaskCompletionSource<string?>();
		using (token.Register(() => tcs.TrySetCanceled()))
		{
			var handle = GCHandle.Alloc(tcs);
			NativeUno.uno_webview_execute_script(webview, GCHandle.ToIntPtr(handle), script);
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

	public void GoBack()
	{
		if (TryGetHandle("navigate back", out var handle))
		{
			NativeUno.uno_webview_go_back(handle);
		}
	}

	public void GoForward()
	{
		if (TryGetHandle("navigate forward", out var handle))
		{
			NativeUno.uno_webview_go_forward(handle);
		}
	}

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

		if (!TryGetHandle("invoke a script on", out var webview))
		{
			return null;
		}

		var tcs = new TaskCompletionSource<string?>();
		using (token.Register(() => tcs.TrySetCanceled()))
		{
			var handle = GCHandle.Alloc(tcs);
			NativeUno.uno_webview_invoke_script(webview, GCHandle.ToIntPtr(handle), javascript);
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

		if (TryGetHandle("navigate", out var handle))
		{
			NativeUno.uno_webview_navigate(handle, url, null);
		}
	}

	public void ProcessNavigation(string html)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"LoadHtmlString: {html}");
		}

		_lastHtmlContent = html;
		if (TryGetHandle("load HTML into", out var handle))
		{
			NativeUno.uno_webview_load_html(handle, html);
		}
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
			if (TryGetHandle("navigate", out var handle))
			{
				NativeUno.uno_webview_navigate(handle, url, headers);
			}
		}
	}

	public void Reload()
	{
		if (!TryGetHandle("reload", out var handle))
		{
			return;
		}

		if (_lastHtmlContent != null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Reloading cached HTML content");
			}
			NativeUno.uno_webview_load_html(handle, _lastHtmlContent);
		}
		else
		{
			NativeUno.uno_webview_reload(handle);
		}
	}

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
		if (TryGetHandle("set the scrolling mode of", out var handle))
		{
			NativeUno.uno_webview_set_scrolling_enabled(handle, isScrollingEnabled);
		}
	}

	public void Stop()
	{
		if (TryGetHandle("stop", out var handle))
		{
			NativeUno.uno_webview_stop(handle);
		}
	}

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
		// A disposed peer reports no history rather than keeping the last known values: a stale `true`
		// would invite a GoBack/GoForward call that can no longer do anything.
		var canGoBack = false;
		var canGoForward = false;

		if (TryGetHandle("read the navigation history of", out var handle))
		{
			canGoBack = NativeUno.uno_webview_can_go_back(handle);
			canGoForward = NativeUno.uno_webview_can_go_forward(handle);
		}

		_owner.SetHistoryProperties(canGoBack, canGoForward);
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
