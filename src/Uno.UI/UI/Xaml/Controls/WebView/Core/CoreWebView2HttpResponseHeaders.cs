#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// HTTP response headers for custom WebResourceResponse.
/// </summary>
public partial class CoreWebView2HttpResponseHeaders : IEnumerable<KeyValuePair<string, string>>
{
	private readonly INativeHttpResponseHeaders _nativeHeaders;

	internal CoreWebView2HttpResponseHeaders(INativeHttpResponseHeaders nativeHeaders)
	{
		_nativeHeaders = nativeHeaders;
	}

	internal INativeHttpResponseHeaders NativeHeaders => _nativeHeaders;

	public void AppendHeader(string name, string value) => _nativeHeaders.AppendHeader(name, value);

	public bool Contains(string name) => _nativeHeaders.Contains(name);

	public string GetHeader(string name) => _nativeHeaders.GetHeader(name);

	public CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
	{
		var nativeIterator = _nativeHeaders.GetHeaders(name);
		if (nativeIterator is INativeHttpHeadersCollectionIterator iterator)
		{
			return new CoreWebView2HttpHeadersCollectionIterator(iterator);
		}
		throw new InvalidOperationException("The native headers collection did not return a valid iterator.");
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _nativeHeaders.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
