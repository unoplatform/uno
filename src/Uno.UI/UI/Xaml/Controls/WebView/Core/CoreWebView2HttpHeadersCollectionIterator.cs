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
#if __SKIA__
	private readonly dynamic _nativeIterator;

	internal CoreWebView2HttpHeadersCollectionIterator(object nativeIterator)
	{
		_nativeIterator = nativeIterator ?? throw new ArgumentNullException(nameof(nativeIterator));
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
			_ => new KeyValuePair<string, string>(
				(string)value.GetType().GetProperty("Key")!.GetValue(value)!,
				(string)value.GetType().GetProperty("Value")!.GetValue(value)!)
		};
	}
#endif
}
