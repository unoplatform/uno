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
		set
		{
			_requestedIsZoomControlEnabled = value;
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("CoreWebView2Settings.IsZoomControlEnabled is not honored on the Skia macOS target.");
			}
		}
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
		var tcs = gch.Target as TaskCompletionSource<byte[]>;
		try
		{
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
			gch.Free();
		}
	}

	async Task<Stream> ISupportsPrint.PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct)
	{
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

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void CookiesCallback(nint handle, sbyte* json, sbyte* error)
	{
		var gch = GCHandle.FromIntPtr(handle);
		var tcs = gch.Target as TaskCompletionSource<string?>;
		try
		{
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
			gch.Free();
		}
	}

	async Task<IReadOnlyList<CoreWebView2Cookie>> ISupportsCookieManager.GetCookiesAsync(string uri, CancellationToken ct)
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
			expires = cookie.Expires,
		});
		NativeUno.uno_webview_set_cookie(_webview, json);
	}

	void ISupportsCookieManager.DeleteCookie(CoreWebView2Cookie cookie)
		=> NativeUno.uno_webview_delete_cookies(_webview, cookie.Name, cookie.Domain, cookie.Path);

	void ISupportsCookieManager.DeleteCookies(string name, string? uri)
	{
		string? host = null;
		if (!string.IsNullOrEmpty(uri))
		{
			try { host = new System.Uri(uri).Host; } catch { }
		}
		NativeUno.uno_webview_delete_cookies(_webview, name, host, null);
	}

	void ISupportsCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path)
		=> NativeUno.uno_webview_delete_cookies(_webview, name, domain, path);

	void ISupportsCookieManager.DeleteAllCookies() => NativeUno.uno_webview_delete_all_cookies(_webview);
}
