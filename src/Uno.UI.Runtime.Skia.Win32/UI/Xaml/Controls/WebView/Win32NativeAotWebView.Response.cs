#if NET10_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Web.WebView2.Core;

using Windows.Storage.Streams;

using DirectN;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class AotWebResourceResponse : INativeWebResourceResponse
{
	internal WebView2.ICoreWebView2WebResourceResponse NativeResponse { get; }

	public AotWebResourceResponse(WebView2.ICoreWebView2WebResourceResponse response)
	{
		NativeResponse = response;
		response.get_Headers(out var headers).ThrowOnError();
		Headers = new AotHttpResponseHeaders(headers);
	}

	public IRandomAccessStream Content
	{
		get
		{
			NativeResponse.get_Content(out var stream).ThrowOnError();
			return stream is null ? new InMemoryRandomAccessStream() : AotStreamHelpers.ConvertIStream(stream);
		}
		set
		{
			var bytes = AotStreamHelpers.ReadIRandomAccessStream(value);
			NativeResponse.put_Content(bytes.Length > 0 ? new ByteArrayIStream(bytes) : null!).ThrowOnError();
		}
	}

	public INativeHttpResponseHeaders Headers { get; }

	public int StatusCode
	{
		get { int v = default; NativeResponse.get_StatusCode(ref v).ThrowOnError(); return v; }
		set => NativeResponse.put_StatusCode(value).ThrowOnError();
	}

	public unsafe string ReasonPhrase
	{
		get { NativeResponse.get_ReasonPhrase(out var v).ThrowOnError(); return v.ToString()!; }
		set { fixed (char* p_value = value) NativeResponse.put_ReasonPhrase(new PWSTR(p_value)).ThrowOnError(); }
	}
}

internal sealed class AotHttpResponseHeaders : INativeHttpResponseHeaders
{
	private readonly WebView2.ICoreWebView2HttpResponseHeaders _headers;

	public AotHttpResponseHeaders(WebView2.ICoreWebView2HttpResponseHeaders headers) => _headers = headers;

	public unsafe void AppendHeader(string name, string value)
	{
		fixed (char* p_name = name, p_value = value)
			_headers.AppendHeader(new PWSTR(p_name), new PWSTR(p_value)).ThrowOnError();
	}

	public unsafe bool Contains(string name)
	{
		BOOL result = default;
		fixed (char* p_name = name)
			_headers.Contains(new PWSTR(p_name), ref result).ThrowOnError();
		return result.Value != 0;
	}

	public unsafe string GetHeader(string name)
	{
		fixed (char* p_name = name)
		{
			_headers.GetHeader(new PWSTR(p_name), out var value).ThrowOnError();
			return value.ToString()!;
		}
	}

	public unsafe object GetHeaders(string name)
	{
		fixed (char* p_name = name)
		{
			_headers.GetHeaders(new PWSTR(p_name), out var iter).ThrowOnError();
			return new AotHttpHeadersCollectionIterator(iter);
		}
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		_headers.GetIterator(out var iter).ThrowOnError();
		return new AotHeadersEnumerator(iter);
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#endif // NET10_0_OR_GREATER
