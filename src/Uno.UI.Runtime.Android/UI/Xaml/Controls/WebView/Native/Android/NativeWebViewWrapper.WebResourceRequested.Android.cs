#if __ANDROID__ || ANDROID_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.Web.WebView2.Core;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Partial class extension for NativeWebViewWrapper to implement ISupportsWebResourceRequested on Android.
/// </summary>
/// <remarks>
/// <para>
/// <c>shouldInterceptRequest</c> sees every request, so virtual hosts such as <c>https://0.0.0.1/</c> are served
/// directly, without rewriting URLs. It runs on a WebView IO thread: the event is raised on the UI thread and the IO
/// thread blocks until the handlers and their deferrals complete, at most <see cref="WebResourceRequestedTimeout"/>.
/// </para>
/// <para>
/// A request the app does not answer (no handler sets a response) returns null, and the WebView loads it itself.
/// </para>
/// </remarks>
internal partial class NativeWebViewWrapper : ISupportsWebResourceRequested
{
	private const string DefaultCharset = "utf-8";

	private readonly object _filtersGate = new();
	private readonly List<WebResourceFilter> _webResourceFilters = new();
	private WebResourceInterceptingClient? _interceptingClient;
	private static readonly Lazy<HttpClient> _refetchClient = new(CreateRefetchClient);

	/// <summary>
	/// How long the IO thread waits for <c>WebResourceRequested</c> handlers and deferrals before letting the
	/// WebView load the request itself.
	/// </summary>
	internal static TimeSpan WebResourceRequestedTimeout { get; set; } = TimeSpan.FromSeconds(30);

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	public void AddWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		lock (_filtersGate)
		{
			_webResourceFilters.Add(new WebResourceFilter(uri, resourceContext, requestSourceKinds));
		}

		EnsureInterceptingClient();
	}

	public void RemoveWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		lock (_filtersGate)
		{
			_webResourceFilters.RemoveAll(f => f.Equals(uri, resourceContext, requestSourceKinds));
		}
	}

	private void EnsureInterceptingClient()
	{
		if (_webView == null)
		{
			return;
		}

		if (_interceptingClient == null)
		{
			_interceptingClient = new WebResourceInterceptingClient(_coreWebView, this);
		}

		_webView.SetWebViewClient(_interceptingClient);
	}

	/// <summary>
	/// Called by WebResourceInterceptingClient, on a WebView IO thread, for every request.
	/// </summary>
	internal WebResourceResponse? OnWebResourceRequested(IWebResourceRequest? request)
	{
		if (request?.Url == null)
		{
			return null;
		}

		var url = request.Url.ToString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(url))
		{
			return null;
		}

		var resourceContext = WebResourceContextHelper.DetermineResourceContext(url, request.IsForMainFrame, ReadHeaders(request));

		if (!MatchesAnyFilter(url, resourceContext, CoreWebView2WebResourceRequestSourceKinds.Document))
		{
			return null;
		}

		var nativeArgs = new NativeCoreWebView2WebResourceRequestedEventArgs(request, resourceContext);
		var args = new CoreWebView2WebResourceRequestedEventArgs(nativeArgs);

		if (!RaiseWebResourceRequestedAndWait(nativeArgs, args, url))
		{
			return null;
		}

		if (nativeArgs.Response is { } response)
		{
			return CreateNativeResponse(response, url, request.IsForMainFrame);
		}

		if (!nativeArgs.RequiresRefetch)
		{
			return null;
		}

		var effectiveHeaders = nativeArgs.GetEffectiveHeaders();
		if (effectiveHeaders == null)
		{
			return null;
		}

		var method = args.Request.Method;
		if (string.IsNullOrWhiteSpace(method))
		{
			method = request.Method ?? "GET";
		}

		var refetchUrl = args.Request.Uri;
		if (string.IsNullOrWhiteSpace(refetchUrl))
		{
			refetchUrl = url;
		}

		return FetchWithModifiedHeaders(refetchUrl, method, effectiveHeaders);
	}

	/// <returns>false when the request must be left to the WebView (no handler, timeout).</returns>
	private bool RaiseWebResourceRequestedAndWait(NativeCoreWebView2WebResourceRequestedEventArgs nativeArgs, CoreWebView2WebResourceRequestedEventArgs args, string url)
	{
		if (WebResourceRequested is null)
		{
			// The CoreWebView2 detached from this view (template re-applied): nobody can answer.
			return false;
		}

		void Raise()
		{
			try
			{
				WebResourceRequested?.Invoke(this, args);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("WebResourceRequested handler threw an exception.", ex);
				}
			}
			finally
			{
				nativeArgs.OnHandlersReturned();
			}
		}

		var dispatcher = _coreWebView.Owner.Dispatcher;
		if (dispatcher.HasThreadAccess)
		{
			// Android documents shouldInterceptRequest as never called on the UI thread; waiting here would deadlock a
			// handler that awaits UI work, so a pending deferral cannot be honored.
			Raise();
			return nativeArgs.WaitForCompletion(TimeSpan.Zero, CancellationToken.None) || LogTimeout(url, deferred: true);
		}

		_ = dispatcher.RunAsync(CoreDispatcherPriority.High, Raise);

		return nativeArgs.WaitForCompletion(WebResourceRequestedTimeout, CancellationToken.None) || LogTimeout(url, deferred: false);
	}

	private bool LogTimeout(string url, bool deferred)
	{
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn(deferred
				? $"WebResourceRequested for {url} took a deferral on the UI thread, which Android cannot wait for; the WebView loads the request itself."
				: $"WebResourceRequested for {url} did not complete within {WebResourceRequestedTimeout}; the WebView loads the request itself.");
		}

		return false;
	}

	/// <summary>
	/// Converts any response (managed, from <c>CreateWebResourceResponse</c>, or native) to an Android response.
	/// </summary>
	private WebResourceResponse? CreateNativeResponse(INativeWebResourceResponse response, string url, bool isForMainFrame)
	{
		WebResourceResponseSnapshot snapshot;
		try
		{
			snapshot = WebResourceResponseSnapshot.Create(response);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"WebResourceRequested: the response for {url} could not be read.", ex);
			}

			return null;
		}

		// WebResourceResponse rejects redirects (3xx) and codes outside 100-599.
		if (snapshot.StatusCode is < 100 or > 599 or (>= 300 and <= 399))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"WebResourceRequested: status {snapshot.StatusCode} for {url} cannot be served by the Android WebView (redirects are not supported); the WebView loads the request itself.");
			}

			return null;
		}

		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var header in snapshot.Headers)
		{
			headers[header.Key] = headers.TryGetValue(header.Key, out var existing)
				? existing + ", " + header.Value
				: header.Value;
		}

		ParseContentType(headers, url, out var mimeType, out var charset);
		var body = snapshot.Body;

		if (isForMainFrame
			&& string.Equals(mimeType, "text/html", StringComparison.OrdinalIgnoreCase)
			&& !HasContentEncoding(headers)
			&& TryInjectDocumentScripts(body, charset, url, out var injected))
		{
			body = injected;
			charset = HtmlDocumentScriptInjector.OutputCharset;
			headers["Content-Type"] = mimeType + "; charset=" + charset;
			headers.Remove("Content-Length");
		}

		return new WebResourceResponse(
			mimeType,
			charset ?? DefaultCharset,
			snapshot.StatusCode,
			GetReasonPhrase(snapshot),
			headers,
			new MemoryStream(body, writable: false));
	}

	private static void ParseContentType(Dictionary<string, string> headers, string url, out string mimeType, out string? charset)
	{
		charset = null;
		if (headers.TryGetValue("Content-Type", out var contentType)
			&& MediaTypeHeaderValue.TryParse(contentType, out var parsed)
			&& !string.IsNullOrWhiteSpace(parsed.MediaType))
		{
			mimeType = parsed.MediaType;
			charset = string.IsNullOrWhiteSpace(parsed.CharSet) ? null : parsed.CharSet.Trim('"');
			return;
		}

		// Without a Content-Type, guess from the extension like a file server would; an extension-less URL is a page.
		var extension = MimeTypeMap.GetFileExtensionFromUrl(url);
		mimeType = (string.IsNullOrEmpty(extension) ? null : MimeTypeMap.Singleton?.GetMimeTypeFromExtension(extension.ToLowerInvariant()))
			?? (string.IsNullOrEmpty(extension) ? "text/html" : "application/octet-stream");
	}

	private static bool HasContentEncoding(Dictionary<string, string> headers)
		=> headers.TryGetValue("Content-Encoding", out var encoding)
			&& !string.IsNullOrWhiteSpace(encoding)
			&& !string.Equals(encoding.Trim(), "identity", StringComparison.OrdinalIgnoreCase);

	// Android requires a non-empty ASCII reason phrase.
	private static string GetReasonPhrase(WebResourceResponseSnapshot snapshot)
	{
		var reason = snapshot.ReasonPhrase?.Trim();
		if (!string.IsNullOrEmpty(reason) && System.Text.Ascii.IsValid(reason))
		{
			return reason;
		}

		using var standard = new HttpResponseMessage((HttpStatusCode)snapshot.StatusCode);
		return string.IsNullOrEmpty(standard.ReasonPhrase) ? "Unknown" : standard.ReasonPhrase;
	}

	private static Dictionary<string, string> ReadHeaders(IWebResourceRequest request)
	{
		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (request.RequestHeaders is { } source)
		{
			foreach (var header in source)
			{
				if (header.Key is not null && header.Value is not null)
				{
					headers[header.Key] = header.Value;
				}
			}
		}

		return headers;
	}

	private WebResourceResponse? FetchWithModifiedHeaders(
		string url,
		string method,
		IDictionary<string, string> headers)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
		{
			return null;
		}

		if (RequiresRequestBody(method))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"WebResourceRequested: Unable to refetch {method} {url} because the request body is not accessible on Android.");
			}
			return null;
		}

		try
		{
			using var requestMessage = new HttpRequestMessage(new HttpMethod(method), targetUri);

			foreach (var header in headers)
			{
				if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value))
				{
					requestMessage.Content ??= new ByteArrayContent(Array.Empty<byte>());
					requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
				}
			}

			// Sync cookies from WebView's CookieManager to the request
			SyncCookiesFromWebView(url, requestMessage);

			using var response = _refetchClient.Value.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

			// Sync Set-Cookie headers from the response back to WebView's CookieManager
			SyncCookiesToWebView(url, response);

			var payload = new MemoryStream();
			response.Content.CopyToAsync(payload).GetAwaiter().GetResult();
			payload.Position = 0;

			var mimeType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
			var encoding = response.Content.Headers.ContentType?.CharSet ?? "UTF-8";
			var reasonPhrase = string.IsNullOrWhiteSpace(response.ReasonPhrase) ? "OK" : response.ReasonPhrase;
			var responseHeaders = BuildResponseHeaders(response);

			return new WebResourceResponse(
				mimeType,
				encoding,
				(int)response.StatusCode,
				reasonPhrase,
				responseHeaders,
				payload);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"WebResourceRequested: Failed to refetch {method} {url}.", ex);
			}
			return null;
		}
	}

	/// <summary>
	/// Syncs cookies from Android WebView's CookieManager to the HttpRequestMessage.
	/// This ensures session cookies are included in re-fetched requests.
	/// </summary>
	private static void SyncCookiesFromWebView(string url, HttpRequestMessage request)
	{
		try
		{
			var cookieManager = Android.Webkit.CookieManager.Instance;
			if (cookieManager == null)
			{
				return;
			}

			var cookies = cookieManager.GetCookie(url);
			if (!string.IsNullOrEmpty(cookies))
			{
				// Don't override if Cookie header was already set by the caller
				if (!request.Headers.Contains("Cookie"))
				{
					request.Headers.TryAddWithoutValidation("Cookie", cookies);
				}
			}
		}
		catch (Exception)
		{
			// Cookie sync is best-effort, don't fail the request
		}
	}

	/// <summary>
	/// Syncs Set-Cookie headers from the HttpResponseMessage back to Android WebView's CookieManager.
	/// This ensures session state is maintained after re-fetched requests.
	/// </summary>
	private static void SyncCookiesToWebView(string url, HttpResponseMessage response)
	{
		try
		{
			var cookieManager = Android.Webkit.CookieManager.Instance;
			if (cookieManager == null)
			{
				return;
			}

			if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
			{
				foreach (var cookie in cookies)
				{
					cookieManager.SetCookie(url, cookie);
				}
			}
		}
		catch (Exception)
		{
			// Cookie sync is best-effort, don't fail the request
		}
	}

	private static Dictionary<string, string> BuildResponseHeaders(HttpResponseMessage response)
	{
		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var header in response.Headers)
		{
			headers[header.Key] = string.Join(", ", header.Value);
		}
		foreach (var header in response.Content.Headers)
		{
			headers[header.Key] = string.Join(", ", header.Value);
		}
		return headers;
	}

	private static HttpClient CreateRefetchClient()
	{
		var handler = new HttpClientHandler
		{
			AutomaticDecompression = DecompressionMethods.All,
		};

		return new HttpClient(handler);
	}

	private static bool RequiresRequestBody(string method)
		=> !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);

	private bool MatchesAnyFilter(string url, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds sourceKind)
	{
		lock (_filtersGate)
		{
			foreach (var filter in _webResourceFilters)
			{
				if (filter.Matches(url, resourceContext, sourceKind))
				{
					return true;
				}
			}
		}

		return false;
	}
}
#endif
