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
#elif __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA
	private readonly IEnumerator<KeyValuePair<string, string>> _enumerator;
	private bool _hasCurrent;

	internal CoreWebView2HttpHeadersCollectionIterator(IEnumerable<KeyValuePair<string, string>> headers)
	{
		_enumerator = headers.GetEnumerator();
		_hasCurrent = _enumerator.MoveNext();
	}

	public KeyValuePair<string, string> Current => _hasCurrent ? _enumerator.Current : default;

	public bool HasCurrent => _hasCurrent;

	public bool MoveNext()
	{
		_hasCurrent = _enumerator.MoveNext();
		return _hasCurrent;
	}

	public uint GetMany(KeyValuePair<string, string>[] items)
	{
		uint count = 0;
		while (count < items.Length && _hasCurrent)
		{
			items[count++] = _enumerator.Current;
			_hasCurrent = _enumerator.MoveNext();
		}
		return count;
	}
#endif
}
