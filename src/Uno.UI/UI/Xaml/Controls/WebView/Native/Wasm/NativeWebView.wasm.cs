#nullable enable

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Helpers;
using static __Microsoft.UI.Xaml.Controls.NativeWebView;

#if WASM_SKIA
using ElementId = System.String;
#else
using ElementId = System.IntPtr;
#endif

namespace Microsoft.UI.Xaml.Controls;

internal partial class NativeWebView : ICleanableNativeWebView
{
	private readonly CoreWebView2 _coreWebView;
	private readonly ElementId _elementId;
	private static readonly ConcurrentDictionary<ElementId, NativeWebView> _elementIdToNativeWebView = new();

	public NativeWebView(CoreWebView2 coreWebView, ElementId elementId)
	{
		NativeMethods.BuildImports(
#if WASM_SKIA
			"Uno.UI.Runtime.Skia.WebAssembly.Browser"
#else
			"Uno.UI"
#endif
			);
		_coreWebView = coreWebView;
		_elementId = elementId;

		NativeMethods.InitializeStyling(elementId);
		NativeMethods.SetupEvents(elementId);
	}

	[JSExport]
	internal static bool DispatchNewWindowRequested(ElementId elementId, string targetUrl, string refererUrl)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			Uri refererUri;
			if (string.IsNullOrEmpty(refererUrl) || !Uri.TryCreate(refererUrl, UriKind.Absolute, out refererUri!))
			{
				refererUri = CoreWebView2.BlankUri;
			}

			nativeWebView._coreWebView.RaiseNewWindowRequested(
				targetUrl,
				refererUri,
				out bool handled);

			return handled;
		}

		return false;
	}

	[JSExport]
	internal static void DispatchLoadEvent(ElementId elementId, string? absoluteUrl)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			nativeWebView.OnNavigationCompleted(nativeWebView._coreWebView, absoluteUrl);
		}
	}

	[JSExport]
	internal static void DispatchWebMessage(ElementId elementId, string message)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			nativeWebView._coreWebView.RaiseWebMessageReceived(message);
		}
	}

	public string DocumentTitle => NativeMethods.GetDocumentTitle(_elementId) ?? "";

	private void OnNavigationCompleted(object sender, string? absoluteUrl)
	{
		if (_coreWebView is null)
		{
			return;
		}

		var uriString = string.IsNullOrEmpty(absoluteUrl) ? NativeMethods.GetAttribute(_elementId, "src") : absoluteUrl;
		Uri uri = CoreWebView2.BlankUri;
		if (!string.IsNullOrEmpty(uriString))
		{
			uri = new Uri(uriString);
		}

		_coreWebView.OnDocumentTitleChanged();
		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		await Task.Yield();
		var result = NativeMethods.ExecuteScript(_elementId, script);

		// String needs to be wrapped in quotes to match Windows behavior
		return $"\"{result?.Replace("\"", "\\\"")}\"";
	}

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) =>
		throw new NotSupportedException("InvokeScriptAsync with arguments is not yet supported on this platform.");

	private void ScheduleNavigationStarting(string? url, Action loadAction)
	{
		_ = _coreWebView.Owner.Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.High, () =>
		{
			_coreWebView.RaiseNavigationStarting(url, out var cancel);

			if (!cancel)
			{
				loadAction?.Invoke();
			}
		});
	}

	public void ProcessNavigation(Uri uri)
	{
		var uriString = uri.OriginalString;

		// Handle virtual host mapping for local assets
		if (!string.IsNullOrEmpty(uri.Host) &&
			_coreWebView.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			var relativePath = uri.AbsolutePath.TrimStart('/');
			var mappedPath = $"{folderName.TrimEnd('/')}/{relativePath}";

			if (!string.IsNullOrEmpty(relativePath))
			{
				var packageBase = NativeMethods.GetPackageBase();
				uriString = $"{packageBase.TrimEnd('/')}/{mappedPath.TrimStart('/')}";

				if (!string.IsNullOrEmpty(uri.Query))
				{
					uriString += uri.Query;
				}
				if (!string.IsNullOrEmpty(uri.Fragment))
				{
					uriString += uri.Fragment;
				}
			}
		}

		ScheduleNavigationStarting(uriString, () => NativeMethods.Navigate(_elementId, uriString));
		OnNavigationCompleted(this, null);
	}

	public void ProcessNavigation(string html)
	{
		ScheduleNavigationStarting(null, () => NativeMethods.SetAttribute(_elementId, "srcdoc", html));
		OnNavigationCompleted(this, null);
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
	}

	public void Reload() => NativeMethods.Reload(_elementId);

	public void Stop() => NativeMethods.Stop(_elementId);

	public void GoBack() => NativeMethods.GoBack(_elementId);

	public void GoForward() => NativeMethods.GoForward(_elementId);

	public void SetScrollingEnabled(bool isScrollingEnabled) { }

	public void Dispose()
	{
		// Todo call this and reattach if needed
		_elementIdToNativeWebView.TryRemove(_elementId, out _);
	}

	public void OnLoaded()
	{
		_elementIdToNativeWebView.TryAdd(_elementId, this);
		NativeMethods.SetupEvents(_elementId);
		DispatchLoadEvent(_elementId, null);
	}

	public void OnUnloaded()
	{
		NativeMethods.CleanupEvents(_elementId);
		_elementIdToNativeWebView.TryRemove(_elementId, out var _);
	}
}
