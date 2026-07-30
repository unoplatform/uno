#if NET10_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Web.WebView2.Core;

using Windows.Foundation;
using Windows.Storage.Streams;

using DirectN;

namespace Uno.UI.Runtime.Skia.Win32;

// --- AOT-specific WebResourceRequested wrappers ---
// These implement the same INative* interfaces as Win32NativeWebView.WebResourceRequested.cs
// for the NET10+ NativeAOT code path, working directly with WebView2Aot COM interfaces.

internal sealed class AotWebResourceRequestedEventArgsWrapper : INativeWebResourceRequestedEventArgs
{
	private readonly WebView2.ICoreWebView2WebResourceRequestedEventArgs _args;
	private readonly WebView2.ICoreWebView2WebResourceRequestedEventArgs2? _args2;

	public AotWebResourceRequestedEventArgsWrapper(WebView2.ICoreWebView2WebResourceRequestedEventArgs args)
	{
		_args = args;
		_args2 = args as WebView2.ICoreWebView2WebResourceRequestedEventArgs2;
		_args.get_Request(out var request).ThrowOnError();
		Request = new AotWebResourceRequest(request);
	}

	public INativeWebResourceRequest Request { get; }

	public INativeWebResourceResponse? Response
	{
		get
		{
			_args.get_Response(out var response).ThrowOnError();
			return response is null ? null : new AotWebResourceResponse(response);
		}
		set
		{
			if (value is AotWebResourceResponse wr)
			{
				_args.put_Response(wr.NativeResponse).ThrowOnError();
			}
			else
			{
				_args.put_Response(null!).ThrowOnError();
			}
		}
	}

	public CoreWebView2WebResourceContext ResourceContext
	{
		get
		{
			WebView2.COREWEBVIEW2_WEB_RESOURCE_CONTEXT context = default;
			_args.get_ResourceContext(ref context).ThrowOnError();
			return (CoreWebView2WebResourceContext)(int)context;
		}
	}

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
	{
		get
		{
			if (_args2 is null) return default;
			WebView2.COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS kinds = default;
			_args2.get_RequestedSourceKind(ref kinds).ThrowOnError();
			return (CoreWebView2WebResourceRequestSourceKinds)(int)kinds;
		}
	}

	public Deferral GetDeferral()
	{
		_args.GetDeferral(out var deferral).ThrowOnError();
		return new Deferral(() => deferral.Complete());
	}
}

internal sealed class AotWebResourceRequest : INativeWebResourceRequest
{
	private readonly WebView2.ICoreWebView2WebResourceRequest _request;

	public AotWebResourceRequest(WebView2.ICoreWebView2WebResourceRequest request)
	{
		_request = request;
		_request.get_Headers(out var headers).ThrowOnError();
		Headers = new AotHttpRequestHeaders(headers);
	}

	public unsafe string Uri
	{
		get { _request.get_Uri(out var v).ThrowOnError(); return v.ToString()!; }
		set { fixed (char* p_value = value) _request.put_Uri(new PWSTR(p_value)).ThrowOnError(); }
	}

	public unsafe string Method
	{
		get { _request.get_Method(out var v).ThrowOnError(); return v.ToString()!; }
		set { fixed (char* p_value = value) _request.put_Method(new PWSTR(p_value)).ThrowOnError(); }
	}

	public IRandomAccessStream Content
	{
		get
		{
			_request.get_Content(out var stream).ThrowOnError();
			return stream is null ? new InMemoryRandomAccessStream() : AotStreamHelpers.ConvertIStream(stream);
		}
		set
		{
			var bytes = AotStreamHelpers.ReadIRandomAccessStream(value);
			_request.put_Content(bytes.Length > 0 ? new ByteArrayIStream(bytes) : null!).ThrowOnError();
		}
	}

	public INativeHttpRequestHeaders Headers { get; }
}

internal sealed class AotHttpRequestHeaders : INativeHttpRequestHeaders
{
	private readonly WebView2.ICoreWebView2HttpRequestHeaders _headers;

	public AotHttpRequestHeaders(WebView2.ICoreWebView2HttpRequestHeaders headers) => _headers = headers;

	public unsafe string GetHeader(string name)
	{
		fixed (char* p_name = name)
		{
			_headers.GetHeader(new PWSTR(p_name), out var value).ThrowOnError();
			return value.ToString()!;
		}
	}

	public unsafe INativeHttpHeadersCollectionIterator GetHeaders(string name)
	{
		fixed (char* p_name = name)
		{
			_headers.GetHeaders(new PWSTR(p_name), out var iter).ThrowOnError();
			return new AotHttpHeadersCollectionIterator(iter);
		}
	}

	public unsafe bool Contains(string name)
	{
		BOOL result = default;
		fixed (char* p_name = name)
			_headers.Contains(new PWSTR(p_name), ref result).ThrowOnError();
		return result.Value != 0;
	}

	public unsafe void SetHeader(string name, string value)
	{
		fixed (char* p_name = name, p_value = value)
			_headers.SetHeader(new PWSTR(p_name), new PWSTR(p_value)).ThrowOnError();
	}

	public unsafe void RemoveHeader(string name)
	{
		fixed (char* p_name = name)
			_headers.RemoveHeader(new PWSTR(p_name)).ThrowOnError();
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		_headers.GetIterator(out var iter).ThrowOnError();
		return new AotHeadersEnumerator(iter);
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class AotHttpHeadersCollectionIterator : INativeHttpHeadersCollectionIterator
{
	private readonly WebView2.ICoreWebView2HttpHeadersCollectionIterator _iterator;
	private bool _hasCurrent;
	private KeyValuePair<string, string> _current;

	public AotHttpHeadersCollectionIterator(WebView2.ICoreWebView2HttpHeadersCollectionIterator iterator)
	{
		_iterator = iterator;
		BOOL hasCurrent = default;
		_iterator.get_HasCurrentHeader(ref hasCurrent).ThrowOnError();
		_hasCurrent = hasCurrent.Value != 0;
		if (_hasCurrent) LoadCurrent();
	}

	private void LoadCurrent()
	{
		_iterator.GetCurrentHeader(out var name, out var value).ThrowOnError();
		_current = new(name.ToString()!, value.ToString()!);
	}

	public object Current => _current;
	public bool HasCurrent => _hasCurrent;

	public bool MoveNext()
	{
		if (!_hasCurrent) return false;
		BOOL hasNext = default;
		_iterator.MoveNext(ref hasNext).ThrowOnError();
		_hasCurrent = hasNext.Value != 0;
		if (_hasCurrent) LoadCurrent();
		return _hasCurrent;
	}

	public uint GetMany(object items)
	{
		if (items is not KeyValuePair<string, string>[] array) return 0;
		uint count = 0;
		while (count < array.Length && _hasCurrent)
		{
			array[count++] = _current;
			MoveNext();
		}
		return count;
	}
}

internal sealed class AotHeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
{
	private readonly WebView2.ICoreWebView2HttpHeadersCollectionIterator _iterator;
	private bool _started;
	private bool _hasCurrent;
	private KeyValuePair<string, string> _current;

	public AotHeadersEnumerator(WebView2.ICoreWebView2HttpHeadersCollectionIterator iterator)
	{
		_iterator = iterator;
		BOOL hasCurrent = default;
		_iterator.get_HasCurrentHeader(ref hasCurrent).ThrowOnError();
		_hasCurrent = hasCurrent.Value != 0;
	}

	public KeyValuePair<string, string> Current => _current;
	object IEnumerator.Current => _current;

	public bool MoveNext()
	{
		if (!_started)
		{
			_started = true;
			if (!_hasCurrent) return false;
			_iterator.GetCurrentHeader(out var name, out var value).ThrowOnError();
			_current = new(name.ToString()!, value.ToString()!);
			BOOL hasNext = default;
			_iterator.MoveNext(ref hasNext).ThrowOnError();
			_hasCurrent = hasNext.Value != 0;
			return true;
		}

		if (!_hasCurrent) return false;
		_iterator.GetCurrentHeader(out var n, out var v).ThrowOnError();
		_current = new(n.ToString()!, v.ToString()!);
		BOOL hn = default;
		_iterator.MoveNext(ref hn).ThrowOnError();
		_hasCurrent = hn.Value != 0;
		return true;
	}

	public void Reset() => throw new NotSupportedException();
	public void Dispose() { }
}

#endif // NET10_0_OR_GREATER
