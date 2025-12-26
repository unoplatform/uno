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
#if __SKIA__
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
#elif __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA
	private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);

	internal CoreWebView2HttpResponseHeaders() { }

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

	public CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
	{
		var headers = new List<KeyValuePair<string, string>>();
		if (_headers.TryGetValue(name, out var values))
		{
			foreach (var value in values)
			{
				headers.Add(new KeyValuePair<string, string>(name, value));
			}
		}
		return new CoreWebView2HttpHeadersCollectionIterator(headers);
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
#endif
}
