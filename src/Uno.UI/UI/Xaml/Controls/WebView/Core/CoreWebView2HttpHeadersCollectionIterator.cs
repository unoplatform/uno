#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// HTTP headers collection iterator.
/// </summary>
public partial class CoreWebView2HttpHeadersCollectionIterator : IIterator<KeyValuePair<string, string>>
{
	private readonly INativeHttpHeadersCollectionIterator _nativeIterator;

	internal CoreWebView2HttpHeadersCollectionIterator(INativeHttpHeadersCollectionIterator nativeIterator)
	{
		_nativeIterator = nativeIterator;
	}

	public KeyValuePair<string, string> Current => ConvertToKeyValuePair(_nativeIterator.Current);

	public bool HasCurrent => _nativeIterator.HasCurrent;

	public bool MoveNext() => _nativeIterator.MoveNext();

	public uint GetMany(KeyValuePair<string, string>[] items)
	{
		return _nativeIterator.GetMany(items);
	}

	private static KeyValuePair<string, string> ConvertToKeyValuePair(object value)
	{
		return value switch
		{
			KeyValuePair<string, string> pair => pair,
			_ => throw new NotSupportedException()
		};
	}
}
