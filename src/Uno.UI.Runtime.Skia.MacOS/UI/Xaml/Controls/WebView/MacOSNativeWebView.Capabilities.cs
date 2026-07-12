#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal partial class MacOSNativeWebView :
	ISupportsUserAgent,
	ISupportsScriptEnabled,
	ISupportsZoomControl,
	ISupportsPostWebMessage,
	ISupportsDocumentCreatedScripts,
	ISupportsCookieManager,
	ISupportsPrint
{
	private bool _requestedIsZoomControlEnabled = true;

	internal static unsafe void RegisterCapabilityCallbacks()
	{
		NativeUno.uno_set_webview_pdf_callback(&PdfCallback);
		NativeUno.uno_set_webview_cookies_callback(&CookiesCallback);
		NativeUno.uno_set_webview_cookie_operation_callback(&CookieOperationCallback);
	}

	// --- ISupportsUserAgent ---

	string? ISupportsUserAgent.UserAgent
	{
		get
		{
			var ptr = NativeUno.uno_webview_get_user_agent(_webview);
			if (ptr == nint.Zero)
			{
				return null;
			}
			var s = Marshal.PtrToStringUTF8(ptr);
			NativeUno.free(ptr);
			return s;
		}
		set => NativeUno.uno_webview_set_user_agent(_webview, value);
	}

	// --- ISupportsScriptEnabled ---

	bool ISupportsScriptEnabled.IsScriptEnabled
	{
		get => NativeUno.uno_webview_get_javascript_enabled(_webview);
		set => NativeUno.uno_webview_set_javascript_enabled(_webview, value);
	}

	// --- ISupportsZoomControl ---
	// WKWebView on macOS has no first-class pinch toggle; we accept the setting but no-op.

	bool ISupportsZoomControl.IsZoomControlEnabled
	{
		get => _requestedIsZoomControlEnabled;
		set => _requestedIsZoomControlEnabled = value;
	}

	// --- ISupportsPostWebMessage ---

	void ISupportsPostWebMessage.PostWebMessageAsJson(string json)
		=> NativeUno.uno_webview_post_web_message(_webview, json, true);

	void ISupportsPostWebMessage.PostWebMessageAsString(string message)
		=> NativeUno.uno_webview_post_web_message(_webview, message, false);

	// --- ISupportsDocumentCreatedScripts ---

	Task<string> ISupportsDocumentCreatedScripts.AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
	{
		var ptr = NativeUno.uno_webview_add_user_script(_webview, javaScript);
		var id = Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
		NativeUno.free(ptr);
		return Task.FromResult(id);
	}

	void ISupportsDocumentCreatedScripts.RemoveScriptToExecuteOnDocumentCreated(string id)
		=> NativeUno.uno_webview_remove_user_script(_webview, id);

	// --- ISupportsPrint ---

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void PdfCallback(nint handle, byte* bytes, nint length, sbyte* error)
	{
		var gch = GCHandle.FromIntPtr(handle);
		try
		{
			var tcs = gch.Target as TaskCompletionSource<byte[]>;
			if (tcs is null)
			{
				return;
			}
			if (error is not null)
			{
				tcs.TrySetException(new System.InvalidOperationException(new string(error)));
				return;
			}
			if (bytes is null || length <= 0)
			{
				tcs.TrySetResult(System.Array.Empty<byte>());
				return;
			}
			var managed = new byte[length];
			Marshal.Copy((nint)bytes, managed, 0, (int)length);
			tcs.TrySetResult(managed);
		}
		finally
		{
			if (gch.IsAllocated)
			{
				gch.Free();
			}
		}
	}

	async Task<Stream> ISupportsPrint.PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct)
	{
		if (settings?.HasNonDefaultPdfSettings == true)
		{
			throw new System.NotSupportedException("WKWebView on macOS does not support custom CoreWebView2 PDF print settings.");
		}

		var tcs = new TaskCompletionSource<byte[]>();
		var gch = GCHandle.Alloc(tcs);
		using var reg = ct.Register(() => tcs.TrySetCanceled());
		NativeUno.uno_webview_print_to_pdf(_webview, GCHandle.ToIntPtr(gch));
		var bytes = await tcs.Task;
		return new MemoryStream(bytes, writable: false);
	}

	Task<CoreWebView2PrintStatus> ISupportsPrint.ShowPrintUIAsync(CoreWebView2PrintDialogKind dialogKind, CancellationToken ct)
	{
		var status = NativeUno.uno_webview_show_print_ui(_webview);
		// CoreWebView2PrintStatus has only Succeeded / PrinterUnavailable / OtherError;
		// user-cancelled is reported as OtherError to match the cross-platform enum surface.
		return Task.FromResult(status switch
		{
			0 => CoreWebView2PrintStatus.Succeeded,
			_ => CoreWebView2PrintStatus.OtherError,
		});
	}

	// --- ISupportsCookieManager ---
	private readonly object _cookieMutationGate = new();
	private Task _pendingCookieMutations = Task.CompletedTask;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void CookieOperationCallback(nint handle, sbyte* error)
	{
		var gch = GCHandle.FromIntPtr(handle);
		try
		{
			var tcs = gch.Target as TaskCompletionSource<object?>;
			if (tcs is null)
			{
				return;
			}
			if (error is not null)
			{
				tcs.TrySetException(new System.InvalidOperationException(new string(error)));
			}
			else
			{
				tcs.TrySetResult(null);
			}
		}
		finally
		{
			if (gch.IsAllocated)
			{
				gch.Free();
			}
		}
	}

	private void QueueCookieMutation(Func<Task> mutation)
	{
		lock (_cookieMutationGate)
		{
			_pendingCookieMutations = RunQueuedCookieMutationAsync(_pendingCookieMutations, mutation);
		}
	}

	private async Task RunQueuedCookieMutationAsync(Task previous, Func<Task> mutation)
	{
		try
		{
			await previous;
		}
		catch (System.Exception error)
		{
			this.Log().Error("A previous CoreWebView2 cookie mutation failed.", error);
		}

		await mutation();
	}

	private Task GetPendingCookieMutations()
	{
		lock (_cookieMutationGate)
		{
			return _pendingCookieMutations;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void CookiesCallback(nint handle, sbyte* json, sbyte* error)
	{
		var gch = GCHandle.FromIntPtr(handle);
		try
		{
			var tcs = gch.Target as TaskCompletionSource<string?>;
			if (tcs is null)
			{
				return;
			}
			if (error is not null)
			{
				tcs.TrySetException(new System.InvalidOperationException(new string(error)));
				return;
			}
			tcs.TrySetResult(json is null ? null : new string(json));
		}
		finally
		{
			if (gch.IsAllocated)
			{
				gch.Free();
			}
		}
	}

	async Task<IReadOnlyList<CoreWebView2Cookie>> ISupportsCookieManager.GetCookiesAsync(string uri, CancellationToken ct)
	{
		await GetPendingCookieMutations();
		return await GetCookiesCoreAsync(uri, ct);
	}

	private async Task<IReadOnlyList<CoreWebView2Cookie>> GetCookiesCoreAsync(string uri, CancellationToken ct)
	{
		var tcs = new TaskCompletionSource<string?>();
		var gch = GCHandle.Alloc(tcs);
		using var reg = ct.Register(() => tcs.TrySetCanceled());
		NativeUno.uno_webview_get_cookies(_webview, GCHandle.ToIntPtr(gch), uri);
		var json = await tcs.Task;
		if (string.IsNullOrEmpty(json))
		{
			return System.Array.Empty<CoreWebView2Cookie>();
		}

		var doc = JsonDocument.Parse(json);
		var list = new List<CoreWebView2Cookie>(doc.RootElement.GetArrayLength());
		foreach (var e in doc.RootElement.EnumerateArray())
		{
			var name = e.GetProperty("name").GetString() ?? string.Empty;
			var value = e.GetProperty("value").GetString() ?? string.Empty;
			var domain = e.GetProperty("domain").GetString() ?? string.Empty;
			var path = e.GetProperty("path").GetString() ?? "/";
			var cookie = new CoreWebView2Cookie(name, value, domain, path)
			{
				IsSecure = e.GetProperty("isSecure").GetBoolean(),
				IsHttpOnly = e.GetProperty("isHttpOnly").GetBoolean(),
				Expires = e.GetProperty("expires").GetDouble(),
				SameSite = (CoreWebView2CookieSameSiteKind)e.GetProperty("sameSite").GetInt32(),
			};
			list.Add(cookie);
		}
		return list;
	}

	void ISupportsCookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie)
	{
		var json = JsonSerializer.Serialize(new
		{
			name = cookie.Name,
			value = cookie.Value ?? string.Empty,
			domain = cookie.Domain ?? "localhost",
			path = string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path,
			isSecure = cookie.IsSecure,
			isHttpOnly = cookie.IsHttpOnly,
			sameSite = (int)cookie.SameSite,
			expires = cookie.Expires,
		});
		QueueCookieMutation(() => RunCookieMutation(handle => NativeUno.uno_webview_set_cookie(_webview, handle, json)));
	}

	void ISupportsCookieManager.DeleteCookie(CoreWebView2Cookie cookie)
		=> QueueCookieMutation(() => RunCookieMutation(handle => NativeUno.uno_webview_delete_cookies(_webview, handle, cookie.Name, cookie.Domain, cookie.Path)));

	void ISupportsCookieManager.DeleteCookies(string name, string? uri)
	{
		QueueCookieMutation(async () =>
		{
			if (string.IsNullOrEmpty(uri))
			{
				await RunCookieMutation(handle => NativeUno.uno_webview_delete_cookies(_webview, handle, name, null, null));
				return;
			}

			var cookies = await GetCookiesCoreAsync(uri, CancellationToken.None);
			foreach (var cookie in cookies)
			{
				if (string.Equals(cookie.Name, name, System.StringComparison.Ordinal))
				{
					await RunCookieMutation(handle => NativeUno.uno_webview_delete_cookies(_webview, handle, cookie.Name, cookie.Domain, cookie.Path));
				}
			}
		});
	}

	void ISupportsCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path)
		=> QueueCookieMutation(() => RunCookieMutation(handle => NativeUno.uno_webview_delete_cookies(_webview, handle, name, domain, path)));

	void ISupportsCookieManager.DeleteAllCookies() =>
		QueueCookieMutation(() => RunCookieMutation(handle => NativeUno.uno_webview_delete_all_cookies(_webview, handle)));

	private static Task RunCookieMutation(Action<nint> start)
	{
		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var gch = GCHandle.Alloc(tcs);
		start(GCHandle.ToIntPtr(gch));
		return tcs.Task;
	}
}
