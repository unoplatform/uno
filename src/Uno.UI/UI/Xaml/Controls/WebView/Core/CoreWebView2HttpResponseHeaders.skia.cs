#nullable enable

#if __SKIA__
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2HttpResponseHeaders : IEnumerable<KeyValuePair<string, string>>
{
	private readonly dynamic _nativeHeaders;

	internal CoreWebView2HttpResponseHeaders(object nativeHeaders)
	{
		_nativeHeaders = nativeHeaders ?? throw new ArgumentNullException(nameof(nativeHeaders));
	}

	internal dynamic NativeHeaders => _nativeHeaders;

	public void AppendHeader(string name, string value) => _nativeHeaders.AppendHeader(name, value);

	public bool Contains(string name) => _nativeHeaders.Contains(name);

	public string GetHeader(string name) => _nativeHeaders.GetHeader(name);

	public CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
		=> new CoreWebView2HttpHeadersCollectionIterator(_nativeHeaders.GetHeaders(name));

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		if (_nativeHeaders is IEnumerable enumerable)
		{
			foreach (var item in enumerable)
			{
				yield return ConvertToKeyValuePair(item);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private static KeyValuePair<string, string> ConvertToKeyValuePair(object item)
	{
		return item switch
		{
			KeyValuePair<string, string> pair => pair,
			_ => new KeyValuePair<string, string>(
				(string)item.GetType().GetProperty("Key")!.GetValue(item)!,
				(string)item.GetType().GetProperty("Value")!.GetValue(item)!)
		};
	}
}
#endif
