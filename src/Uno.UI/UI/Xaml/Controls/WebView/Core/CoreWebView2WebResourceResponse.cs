#nullable enable

using System;
#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA
using System.Collections.Generic;
using System.IO;
#endif
#if __ANDROID__ || ANDROID_SKIA
using Android.Webkit;
#endif
#if __SKIA__
using Windows.Storage.Streams;
#endif

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a custom response for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceResponse
{
#if __SKIA__
	private readonly dynamic _nativeResponse;
	private CoreWebView2HttpResponseHeaders? _headers;

	internal CoreWebView2WebResourceResponse(object nativeResponse)
	{
		_nativeResponse = nativeResponse ?? throw new ArgumentNullException(nameof(nativeResponse));
	}

	internal dynamic NativeResponse => _nativeResponse;

	public IRandomAccessStream Content
	{
		get => _nativeResponse.Content;
		set => _nativeResponse.Content = value;
	}

	public CoreWebView2HttpResponseHeaders Headers
		=> _headers ??= new CoreWebView2HttpResponseHeaders(_nativeResponse.Headers);

	public int StatusCode
	{
		get => _nativeResponse.StatusCode;
		set => _nativeResponse.StatusCode = value;
	}

	public string ReasonPhrase
	{
		get => _nativeResponse.ReasonPhrase;
		set => _nativeResponse.ReasonPhrase = value;
	}
#elif __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA
	private CoreWebView2HttpResponseHeaders? _headers;
	private int _statusCode = 200;
	private string _reasonPhrase = "OK";
	private global::Windows.Storage.Streams.IRandomAccessStream? _content;

	internal CoreWebView2WebResourceResponse() { }

	public global::Windows.Storage.Streams.IRandomAccessStream Content
	{
		get => _content ?? throw new InvalidOperationException("Content has not been set.");
		set => _content = value;
	}

	public CoreWebView2HttpResponseHeaders Headers
		=> _headers ??= new CoreWebView2HttpResponseHeaders();

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

#if __ANDROID__ || ANDROID_SKIA
	private const string DefaultMimeType = "text/html";
	private const string DefaultEncoding = "UTF-8";

	/// <summary>
	/// Creates a native Android WebResourceResponse from this response.
	/// </summary>
	internal WebResourceResponse? ToNativeResponse()
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

		return new WebResourceResponse(
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
#endif
#endif
}
