#if __ANDROID__ || __IOS__ || __MACOS__ || ANDROID_SKIA || UIKIT_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Native platform implementation for HTTP headers collection iterator.
/// </summary>
public partial class CoreWebView2HttpHeadersCollectionIterator : IIterator<KeyValuePair<string, string>>
{
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
}
#endif
