#nullable enable

#if ANDROID_SKIA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Uno.UI.Runtime.Skia.Android.WebView.Adapters;

/// <summary>
/// Adapts Android IWebResourceRequest to the native WebView2 event args API surface.
/// This allows the Skia CoreWebView2WebResourceRequestedEventArgs (which uses dynamic) 
/// to work with Android data.
/// </summary>
public class WebResourceRequestedEventArgsAdapter
{
	private readonly WebResourceRequestAdapter _request;
	private WebResourceResponseAdapter? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private readonly CoreWebView2WebResourceRequestSourceKinds _requestedSourceKind;

	internal WebResourceRequestedEventArgsAdapter(
		IWebResourceRequest? nativeRequest,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestedSourceKind)
	{
		_request = new WebResourceRequestAdapter(nativeRequest);
		_resourceContext = resourceContext;
		_requestedSourceKind = requestedSourceKind;
	}

	public WebResourceRequestAdapter Request => _request;

	public WebResourceResponseAdapter? Response
	{
		get => _response;
		set => _response = value;
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => _requestedSourceKind;

	public Deferral GetDeferral() => new Deferral(() => { });

	/// <summary>
	/// Checks if headers have been modified and require re-fetching.
	/// </summary>
	internal bool RequiresRefetch => _request.Headers?.HasModifications ?? false;

	/// <summary>
	/// Gets the native Android WebResourceResponse if a custom response was set.
	/// </summary>
	internal global::Android.Webkit.WebResourceResponse? GetNativeResponse() 
		=> _response?.ToNativeResponse();

	/// <summary>
	/// Gets the effective headers for re-fetching if headers were modified.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _request.Headers?.GetEffectiveHeaders();
}

/// <summary>
/// Adapts Android IWebResourceRequest to the native WebView2 request API surface.
/// </summary>
public class WebResourceRequestAdapter
{
	private HttpRequestHeadersAdapter? _headers;
	private string _uri;
	private string _method;
	private readonly IWebResourceRequest? _nativeRequest;

	internal WebResourceRequestAdapter(IWebResourceRequest? nativeRequest)
	{
		_nativeRequest = nativeRequest;
		_uri = nativeRequest?.Url?.ToString() ?? string.Empty;
		_method = nativeRequest?.Method ?? "GET";

		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (nativeRequest?.RequestHeaders != null)
		{
			foreach (var key in nativeRequest.RequestHeaders.Keys)
			{
				if (key != null && nativeRequest.RequestHeaders.TryGetValue(key, out var value) && value != null)
				{
					headers[key] = value;
				}
			}
		}

		_headers = new HttpRequestHeadersAdapter(headers);
	}

	public string Uri
	{
		get => _uri;
		set => _uri = value ?? string.Empty;
	}

	public string Method
	{
		get => _method;
		set => _method = string.IsNullOrWhiteSpace(value) ? _method : value;
	}

	// Note: Content is not available on Android WebResourceRequest
	public object? Content
	{
		get => null;
		set { /* Not supported on Android */ }
	}

	public HttpRequestHeadersAdapter Headers => _headers ??= new HttpRequestHeadersAdapter(null);
}

/// <summary>
/// Adapts Android headers dictionary to the native WebView2 headers API surface.
/// </summary>
public class HttpRequestHeadersAdapter : IEnumerable<KeyValuePair<string, string>>
{
	private readonly Dictionary<string, string> _originalHeaders;
	private readonly Dictionary<string, string> _addedHeaders = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _removedHeaders = new(StringComparer.OrdinalIgnoreCase);

	internal HttpRequestHeadersAdapter(IDictionary<string, string>? headers)
	{
		_originalHeaders = headers != null
			? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
			: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	internal bool HasModifications => _addedHeaders.Count > 0 || _removedHeaders.Count > 0;

	internal Dictionary<string, string> GetEffectiveHeaders()
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var kvp in _originalHeaders)
		{
			if (!_removedHeaders.Contains(kvp.Key))
			{
				result[kvp.Key] = kvp.Value;
			}
		}
		foreach (var kvp in _addedHeaders)
		{
			result[kvp.Key] = kvp.Value;
		}
		return result;
	}

	public string GetHeader(string name)
	{
		if (_removedHeaders.Contains(name))
		{
			return string.Empty;
		}
		if (_addedHeaders.TryGetValue(name, out var addedValue))
		{
			return addedValue;
		}
		return _originalHeaders.TryGetValue(name, out var value) ? value : string.Empty;
	}

	public bool Contains(string name)
	{
		if (_removedHeaders.Contains(name))
		{
			return false;
		}
		return _addedHeaders.ContainsKey(name) || _originalHeaders.ContainsKey(name);
	}

	public void SetHeader(string name, string value)
	{
		_removedHeaders.Remove(name);
		_addedHeaders[name] = value;
	}

	public void RemoveHeader(string name)
	{
		_addedHeaders.Remove(name);
		_removedHeaders.Add(name);
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		foreach (var kvp in _originalHeaders)
		{
			if (!_removedHeaders.Contains(kvp.Key) && !_addedHeaders.ContainsKey(kvp.Key))
			{
				yield return kvp;
			}
		}
		foreach (var kvp in _addedHeaders)
		{
			yield return kvp;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Adapts Android WebResourceResponse to the native WebView2 response API surface.
/// </summary>
public class WebResourceResponseAdapter
{
	private HttpResponseHeadersAdapter? _headers;
	private int _statusCode = 200;
	private string _reasonPhrase = "OK";
	private global::Windows.Storage.Streams.IRandomAccessStream? _content;
	private const string DefaultMimeType = "text/html";
	private const string DefaultEncoding = "UTF-8";

	public global::Windows.Storage.Streams.IRandomAccessStream Content
	{
		get => _content ?? throw new InvalidOperationException("Content has not been set.");
		set => _content = value;
	}

	public HttpResponseHeadersAdapter Headers
		=> _headers ??= new HttpResponseHeadersAdapter();

	public int StatusCode
	{
		get => _statusCode;
		set => _statusCode = value;
	}

	public string ReasonPhrase
	{
		get => _reasonPhrase;
		set => _reasonPhrase = value ?? "OK";
	}

	internal global::Android.Webkit.WebResourceResponse? ToNativeResponse()
	{
		if (_content == null)
		{
			return null;
		}

		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (_headers != null)
		{
			foreach (var header in _headers)
			{
				if (headers.TryGetValue(header.Key, out var existingValue))
				{
					headers[header.Key] = string.Concat(existingValue, ", ", header.Value);
				}
				else
				{
					headers[header.Key] = header.Value;
				}
			}
		}

		var (mimeType, encoding) = GetContentMetadata();
		var reasonPhrase = string.IsNullOrWhiteSpace(_reasonPhrase) ? "OK" : _reasonPhrase;
		var stream = _content.AsStreamForRead();
		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		return new global::Android.Webkit.WebResourceResponse(
			mimeType,
			encoding,
			_statusCode,
			reasonPhrase,
			headers,
			stream);
	}

	private (string MimeType, string Encoding) GetContentMetadata()
	{
		var contentTypeHeader = _headers?.GetHeader("Content-Type")?.Trim();
		if (string.IsNullOrWhiteSpace(contentTypeHeader))
		{
			return (DefaultMimeType, DefaultEncoding);
		}

		var mimeType = contentTypeHeader;
		var encoding = DefaultEncoding;
		var separatorIndex = contentTypeHeader.IndexOf(';');
		if (separatorIndex >= 0)
		{
			mimeType = contentTypeHeader.Substring(0, separatorIndex).Trim();
			var charsetPart = contentTypeHeader.Substring(separatorIndex + 1).Trim();
			if (charsetPart.StartsWith("charset=", StringComparison.OrdinalIgnoreCase))
			{
				var charset = charsetPart.Substring("charset=".Length).Trim().Trim('"');
				if (!string.IsNullOrWhiteSpace(charset))
				{
					encoding = charset;
				}
			}
		}

		if (string.IsNullOrWhiteSpace(mimeType))
		{
			mimeType = DefaultMimeType;
		}

		if (string.IsNullOrWhiteSpace(encoding))
		{
			encoding = DefaultEncoding;
		}

		return (mimeType, encoding);
	}
}

/// <summary>
/// Adapts response headers to the native WebView2 headers API surface.
/// </summary>
public class HttpResponseHeadersAdapter : IEnumerable<KeyValuePair<string, string>>
{
	private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);

	public void AppendHeader(string name, string value)
	{
		if (!_headers.TryGetValue(name, out var values))
		{
			values = new List<string>();
			_headers[name] = values;
		}
		values.Add(value);
	}

	public bool Contains(string name) => _headers.ContainsKey(name);

	public string GetHeader(string name)
		=> _headers.TryGetValue(name, out var values) && values.Count > 0 ? values[0] : string.Empty;

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		foreach (var kvp in _headers)
		{
			foreach (var value in kvp.Value)
			{
				yield return new KeyValuePair<string, string>(kvp.Key, value);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
#endif
