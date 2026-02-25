#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// HTTP request headers for WebResourceRequested event.
/// </summary>
public partial class CoreWebView2HttpRequestHeaders : IEnumerable<KeyValuePair<string, string>>
{
	private readonly INativeHttpRequestHeaders _nativeHeaders;

	internal CoreWebView2HttpRequestHeaders(INativeHttpRequestHeaders nativeHeaders)
	{
		_nativeHeaders = nativeHeaders;
	}

	internal object NativeHeaders => _nativeHeaders;

	public string GetHeader(string name) => _nativeHeaders.GetHeader(name);

	public CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
		=> new CoreWebView2HttpHeadersCollectionIterator(_nativeHeaders.GetHeaders(name));

	public bool Contains(string name) => _nativeHeaders.Contains(name);

	public void SetHeader(string name, string value) => _nativeHeaders.SetHeader(name, value);

	public void RemoveHeader(string name) => _nativeHeaders.RemoveHeader(name);

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _nativeHeaders.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
