#nullable enable

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
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
	internal static void DispatchLoadEvent(ElementId elementId)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			nativeWebView.OnNavigationCompleted(nativeWebView._coreWebView, EventArgs.Empty);
		}
	}

	public string DocumentTitle => NativeMethods.GetDocumentTitle(_elementId) ?? "";

	private void OnNavigationCompleted(object sender, EventArgs e)
	{
		if (_coreWebView is null)
		{
			return;
		}

		var uriString = NativeMethods.GetAttribute(_elementId, "src");
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

		if (!string.IsNullOrEmpty(uri.Host) && _coreWebView.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			var packageBase = NativeMethods.GetPackageBase();
			var relativePath = uri.AbsolutePath.TrimStart('/');
			var mappedUrl = packageBase.TrimEnd('/') + "/" + folderName.TrimStart('/').TrimEnd('/');

			if (!string.IsNullOrEmpty(relativePath))
			{
				if (!relativePath.StartsWith(folderName.Trim('/'), StringComparison.OrdinalIgnoreCase))
				{
					mappedUrl += "/" + relativePath;
				}
				else
				{
					var afterFolder = relativePath[folderName.Trim('/').Length..].TrimStart('/');
					if (!string.IsNullOrEmpty(afterFolder))
					{
						mappedUrl += "/" + afterFolder;
					}
				}
			}

			if (!string.IsNullOrEmpty(uri.Query))
			{
				mappedUrl += uri.Query;
			}
			if (!string.IsNullOrEmpty(uri.Fragment))
			{
				mappedUrl += uri.Fragment;
			}

			uriString = mappedUrl;
		}

		ScheduleNavigationStarting(uriString, () => NativeMethods.SetAttribute(_elementId, "src", uriString));
		OnNavigationCompleted(this, EventArgs.Empty);
	}

	public void ProcessNavigation(string html)
	{
		ScheduleNavigationStarting(null, () => NativeMethods.SetAttribute(_elementId, "srcdoc", html));
		OnNavigationCompleted(this, EventArgs.Empty);
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
		DispatchLoadEvent(_elementId);
	}

	public void OnUnloaded()
	{
		NativeMethods.CleanupEvents(_elementId);
		_elementIdToNativeWebView.TryRemove(_elementId, out var _);
	}
}
