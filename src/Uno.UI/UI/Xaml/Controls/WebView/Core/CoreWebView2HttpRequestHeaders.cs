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
#if __SKIA__
	private readonly INativeHttpRequestHeaders _nativeHeaders;

	internal CoreWebView2HttpRequestHeaders(object nativeHeaders)
	{
		if (nativeHeaders is INativeHttpRequestHeaders wrapper)
		{
			_nativeHeaders = wrapper;
		}
		else
		{
			_nativeHeaders = new ReflectionNativeHttpRequestHeaders(nativeHeaders ?? throw new ArgumentNullException(nameof(nativeHeaders)));
		}
	}

	internal object NativeHeaders => _nativeHeaders is ReflectionNativeHttpRequestHeaders r ? r.Target : _nativeHeaders;

	public virtual string GetHeader(string name) => _nativeHeaders.GetHeader(name);

	public CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
		=> new CoreWebView2HttpHeadersCollectionIterator(_nativeHeaders.GetHeaders(name));

	public virtual bool Contains(string name) => _nativeHeaders.Contains(name);

	public virtual void SetHeader(string name, string value) => _nativeHeaders.SetHeader(name, value);

	public virtual void RemoveHeader(string name) => _nativeHeaders.RemoveHeader(name);

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _nativeHeaders.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endif
}
