#if __ANDROID__ || ANDROID_SKIA
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// Android-specific implementation for HTTP request headers.
/// </summary>
internal partial class NativeCoreWebView2HttpRequestHeaders : INativeHttpRequestHeaders
{
	private readonly Dictionary<string, string> _originalHeaders;
	private readonly Dictionary<string, string> _addedHeaders = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _removedHeaders = new(StringComparer.OrdinalIgnoreCase);

	internal NativeCoreWebView2HttpRequestHeaders(IDictionary<string, string>? headers = null)
	{
		_originalHeaders = headers != null
			? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
			: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Indicates whether headers have been modified.
	/// </summary>
	internal bool HasModifications => _addedHeaders.Count > 0 || _removedHeaders.Count > 0;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal Dictionary<string, string> GetEffectiveHeaders()
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// Start with original headers
		foreach (var kvp in _originalHeaders)
		{
			if (!_removedHeaders.Contains(kvp.Key))
			{
				result[kvp.Key] = kvp.Value;
			}
		}

		// Apply added/modified headers
		foreach (var kvp in _addedHeaders)
		{
			result[kvp.Key] = kvp.Value;
		}

		return result;
	}

	public string GetHeader(string name)
	{
		if (_removedHeaders.Contains(name))
		{
			return string.Empty;
		}
		if (_addedHeaders.TryGetValue(name, out var addedValue))
		{
			return addedValue;
		}
		return _originalHeaders.TryGetValue(name, out var value) ? value : string.Empty;
	}

	public INativeHttpHeadersCollectionIterator GetHeaders(string name)
	{
		var headers = new List<KeyValuePair<string, string>>();
		var value = GetHeader(name);
		if (!string.IsNullOrEmpty(value))
		{
			headers.Add(new KeyValuePair<string, string>(name, value));
		}
		return new NativeCoreWebView2HttpHeadersCollectionIterator(headers);
	}

	public bool Contains(string name)
	{
		if (_removedHeaders.Contains(name))
		{
			return false;
		}
		return _addedHeaders.ContainsKey(name) || _originalHeaders.ContainsKey(name);
	}

	/// <summary>
	/// Sets or adds a header.
	/// </summary>
	public void SetHeader(string name, string value)
	{
		_removedHeaders.Remove(name);
		_addedHeaders[name] = value;
	}

	/// <summary>
	/// Removes a header.
	/// </summary>
	public void RemoveHeader(string name)
	{
		_addedHeaders.Remove(name);
		_removedHeaders.Add(name);
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		foreach (var kvp in _originalHeaders)
		{
			if (!_removedHeaders.Contains(kvp.Key) && !_addedHeaders.ContainsKey(kvp.Key))
			{
				yield return kvp;
			}
		}
		foreach (var kvp in _addedHeaders)
		{
			yield return kvp;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
#endif
