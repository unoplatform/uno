#if __ANDROID__ || __IOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA || WASM_SKIA
#nullable enable

using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// Native platform implementation for HTTP headers collection iterator.
/// </summary>
internal class NativeCoreWebView2HttpHeadersCollectionIterator : INativeHttpHeadersCollectionIterator
{
	private readonly IEnumerator<KeyValuePair<string, string>> _enumerator;
	private bool _hasCurrent;

	internal NativeCoreWebView2HttpHeadersCollectionIterator(IEnumerable<KeyValuePair<string, string>> headers)
	{
		_enumerator = headers.GetEnumerator();
		_hasCurrent = _enumerator.MoveNext();
	}

	public object Current => _hasCurrent ? _enumerator.Current : default;

	public bool HasCurrent => _hasCurrent;

	public bool MoveNext()
	{
		_hasCurrent = _enumerator.MoveNext();
		return _hasCurrent;
	}

	public uint GetMany(object items)
	{
		if (items is not KeyValuePair<string, string>[] array)
		{
			return 0;
		}

		uint count = 0;
		while (count < array.Length && _hasCurrent)
		{
			array[count++] = _enumerator.Current;
			_hasCurrent = _enumerator.MoveNext();
		}
		return count;
	}
}
#endif
