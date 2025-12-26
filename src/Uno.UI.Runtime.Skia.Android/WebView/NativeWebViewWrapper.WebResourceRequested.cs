#nullable enable

#if ANDROID_SKIA
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Android.WebView.Adapters;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Partial class extension for NativeWebViewWrapper to implement ISupportsWebResourceRequested on Skia Android.
/// </summary>
internal partial class NativeWebViewWrapper : ISupportsWebResourceRequested
{
	private readonly List<WebResourceFilter> _webResourceFilters = new();
	private WebResourceInterceptingClientSkia? _interceptingClient;
	private static readonly Lazy<HttpClient> _refetchClient = new(CreateRefetchClient);

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	public void AddWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.Add(new WebResourceFilter(uri, resourceContext, requestSourceKinds));
		EnsureInterceptingClient();
	}

	public void RemoveWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.RemoveAll(f => f.Equals(uri, resourceContext, requestSourceKinds));
	}

	private void EnsureInterceptingClient()
	{
		if (_webView == null)
		{
			return;
		}

		if (_interceptingClient == null)
		{
			_interceptingClient = new WebResourceInterceptingClientSkia(_coreWebView, this);
		}

		_webView.SetWebViewClient(_interceptingClient);
	}

	/// <summary>
	/// Called by WebResourceInterceptingClientSkia when a request is intercepted.
	/// </summary>
	internal WebResourceResponse? OnWebResourceRequested(IWebResourceRequest? request)
	{
		if (request?.Url == null || _webResourceFilters.Count == 0)
		{
			return null;
		}

		var url = request.Url.ToString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(url))
		{
			return null;
		}

		var resourceContext = WebResourceContextHelper.DetermineResourceContext(url);
		var sourceKind = DetermineSourceKind(request);

		if (!MatchesAnyFilter(url, resourceContext, sourceKind))
		{
			return null;
		}

		// Create an adapter that mimics the native WebView2 event args API
		var adapter = new WebResourceRequestedEventArgsAdapter(request, resourceContext, sourceKind);

		// Create the Skia CoreWebView2WebResourceRequestedEventArgs using the adapter
		// The Skia implementation uses dynamic, so it will access properties on our adapter
		var args = new CoreWebView2WebResourceRequestedEventArgs(adapter);

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
			return null;
		}

		// Check if custom response was set via the adapter
		var nativeResponse = adapter.GetNativeResponse();
		if (nativeResponse != null)
		{
			return nativeResponse;
		}

		if (!adapter.RequiresRefetch)
		{
			return null;
		}

		var effectiveHeaders = adapter.GetEffectiveHeaders();
		if (effectiveHeaders == null)
		{
			return null;
		}

		var method = adapter.Request.Method;
		if (string.IsNullOrWhiteSpace(method))
		{
			method = request.Method ?? "GET";
		}

		var refetchUrl = adapter.Request.Uri;
		if (string.IsNullOrWhiteSpace(refetchUrl))
		{
			refetchUrl = url;
		}

		return FetchWithModifiedHeaders(refetchUrl, method, effectiveHeaders);
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
		foreach (var filter in _webResourceFilters)
		{
			if (filter.Matches(url, resourceContext, sourceKind))
			{
				return true;
			}
		}
		return false;
	}

	private static CoreWebView2WebResourceRequestSourceKinds DetermineSourceKind(IWebResourceRequest request)
		=> request.IsForMainFrame
			? CoreWebView2WebResourceRequestSourceKinds.Document
			: CoreWebView2WebResourceRequestSourceKinds.All;
}
#endif
