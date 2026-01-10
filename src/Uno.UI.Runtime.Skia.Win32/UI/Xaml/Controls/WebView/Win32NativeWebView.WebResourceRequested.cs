extern alias mswebview2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Storage.Streams;
using Microsoft.Web.WebView2.Core;
using NativeWebView = mswebview2::Microsoft.Web.WebView2.Core;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32WebResourceRequestedEventArgsWrapper : INativeWebResourceRequestedEventArgs
{
	private readonly NativeWebView.CoreWebView2WebResourceRequestedEventArgs _args;

	public Win32WebResourceRequestedEventArgsWrapper(NativeWebView.CoreWebView2WebResourceRequestedEventArgs args)
	{
		_args = args;
		Request = new Win32WebResourceRequest(_args.Request);
	}

	public INativeWebResourceRequest Request { get; }

	public INativeWebResourceResponse? Response
	{
		get => _args.Response is { } response ? new Win32WebResourceResponse(response) : null;
		set
		{
			if (value is Win32WebResourceResponse wr)
			{
				_args.Response = wr.NativeResponse;
			}
			else
			{
				// Underlying _args.Response may be a non-nullable type in this build;
				// use the null-forgiving operator to explicitly assign null when no response is provided.
				_args.Response = null!;
			}
		}
	}

	public CoreWebView2WebResourceContext ResourceContext => (CoreWebView2WebResourceContext)_args.ResourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => (CoreWebView2WebResourceRequestSourceKinds)_args.RequestedSourceKind;

	public Deferral GetDeferral()
	{
		var nativeDeferral = _args.GetDeferral();
		return new Deferral(() => nativeDeferral.Complete());
	}
}

internal class Win32WebResourceRequest : INativeWebResourceRequest
{
	private readonly NativeWebView.CoreWebView2WebResourceRequest _request;

	public Win32WebResourceRequest(NativeWebView.CoreWebView2WebResourceRequest request)
	{
		_request = request;
		Headers = new Win32HttpRequestHeaders(_request.Headers);
	}

	public string Uri
	{
		get => _request.Uri;
		set => _request.Uri = value;
	}

	public string Method
	{
		get => _request.Method;
		set => _request.Method = value;
	}

	public IRandomAccessStream Content
	{
		get => _request.Content == null ? new InMemoryRandomAccessStream() : ConvertStream(_request.Content);
		set => _request.Content = value?.AsStreamForRead();
	}

	public INativeHttpRequestHeaders Headers { get; }

	internal static IRandomAccessStream ConvertStream(Stream stream)
	{
		var randomAccessStream = new InMemoryRandomAccessStream();
		var outputStream = randomAccessStream.GetOutputStreamAt(0);
		var dataWriter = new DataWriter(outputStream);
		var memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		dataWriter.WriteBytes(memoryStream.ToArray());
		dataWriter.StoreAsync().AsTask().Wait();
		randomAccessStream.Seek(0);
		return randomAccessStream;
	}
}

internal class Win32HttpRequestHeaders : INativeHttpRequestHeaders
{
	private readonly NativeWebView.CoreWebView2HttpRequestHeaders _headers;

	public Win32HttpRequestHeaders(NativeWebView.CoreWebView2HttpRequestHeaders headers)
	{
		_headers = headers;
	}

	public string GetHeader(string name) => _headers.GetHeader(name);

	public INativeHttpHeadersCollectionIterator GetHeaders(string name)
		=> new Win32HttpHeadersCollectionIterator(_headers.GetHeaders(name));

	public bool Contains(string name) => _headers.Contains(name);

	public void SetHeader(string name, string value) => _headers.SetHeader(name, value);

	public void RemoveHeader(string name) => _headers.RemoveHeader(name);

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class Win32HttpHeadersCollectionIterator : INativeHttpHeadersCollectionIterator
{
	private readonly NativeWebView.CoreWebView2HttpHeadersCollectionIterator _iterator;
	private bool _hasCurrent;

	public Win32HttpHeadersCollectionIterator(NativeWebView.CoreWebView2HttpHeadersCollectionIterator iterator)
	{
		_iterator = iterator;
		_hasCurrent = _iterator.MoveNext();
	}

	public object Current => _iterator.Current;

	public bool HasCurrent => _hasCurrent;

	public bool MoveNext()
	{
		_hasCurrent = _iterator.MoveNext();
		return _hasCurrent;
	}

	public uint GetMany(object items)
	{
		if (items is not KeyValuePair<string, string>[] array)
		{
			return 0;
		}

		uint count = 0;
		while (count < array.Length && HasCurrent)
		{
			array[count++] = _iterator.Current;
			MoveNext();
		}
		return count;
	}
}

internal class Win32WebResourceResponse : INativeWebResourceResponse
{
	public NativeWebView.CoreWebView2WebResourceResponse NativeResponse { get; }

	public Win32WebResourceResponse(NativeWebView.CoreWebView2WebResourceResponse response)
	{
		NativeResponse = response;
		Headers = new Win32HttpResponseHeaders(response.Headers);
	}

	public IRandomAccessStream Content
	{
		get => NativeResponse.Content == null ? new InMemoryRandomAccessStream() : Win32WebResourceRequest.ConvertStream(NativeResponse.Content);
		set => NativeResponse.Content = value?.AsStreamForRead();
	}

	public INativeHttpResponseHeaders Headers { get; }

	public int StatusCode
	{
		get => NativeResponse.StatusCode;
		set => NativeResponse.StatusCode = value;
	}

	public string ReasonPhrase
	{
		get => NativeResponse.ReasonPhrase;
		set => NativeResponse.ReasonPhrase = value;
	}
}

internal class Win32HttpResponseHeaders : INativeHttpResponseHeaders
{
	private readonly NativeWebView.CoreWebView2HttpResponseHeaders _headers;

	public Win32HttpResponseHeaders(NativeWebView.CoreWebView2HttpResponseHeaders headers)
	{
		_headers = headers;
	}

	public void AppendHeader(string name, string value) => _headers.AppendHeader(name, value);

	public bool Contains(string name) => _headers.Contains(name);

	public string GetHeader(string name) => _headers.GetHeader(name);

	public object GetHeaders(string name) => new Win32HttpHeadersCollectionIterator(_headers.GetHeaders(name));

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
