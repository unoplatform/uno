#if __ANDROID__ || __IOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA || WASM_SKIA
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// Native platform implementation for HTTP response headers.
/// </summary>
internal class NativeCoreWebView2HttpResponseHeaders : INativeHttpResponseHeaders
{
	private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);

	internal NativeCoreWebView2HttpResponseHeaders() { }

	public void AppendHeader(string name, string value)
	{
		if (!_headers.TryGetValue(name, out var values))
		{
			values = new List<string>();
			_headers[name] = values;
		}
		values.Add(value);
	}

	public bool Contains(string name) => _headers.ContainsKey(name);

	public string GetHeader(string name)
		=> _headers.TryGetValue(name, out var values) && values.Count > 0 ? values[0] : string.Empty;

	public object GetHeaders(string name)
	{
		var headers = new List<KeyValuePair<string, string>>();
		if (_headers.TryGetValue(name, out var values))
		{
			foreach (var value in values)
			{
				headers.Add(new KeyValuePair<string, string>(name, value));
			}
		}
		return new NativeCoreWebView2HttpHeadersCollectionIterator(headers);
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		foreach (var kvp in _headers)
		{
			foreach (var value in kvp.Value)
			{
				yield return new KeyValuePair<string, string>(kvp.Key, value);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
#endif
