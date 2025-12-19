#if __IOS__ || __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// iOS/macOS implementation of HTTP request headers for WebResourceRequested.
/// 
/// WKWEBVIEW LIMITATION:
/// =====================
/// WKWebView does NOT allow modifying request headers after a request is initiated.
/// The SetHeader() method tracks modifications for API compatibility, but these
/// modifications CANNOT be applied to the actual WKWebView request.
/// 
/// WORKAROUNDS:
/// 1. Cancel navigation and re-navigate with a new NSMutableURLRequest
/// 2. Use JavaScript injection to modify fetch/XHR headers
/// 3. Use a proxy server to inject headers
/// </summary>
public partial class CoreWebView2HttpRequestHeaders : IEnumerable<KeyValuePair<string, string>>
{
	private readonly Dictionary<string, string> _originalHeaders;
	private readonly Dictionary<string, string> _addedHeaders = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _removedHeaders = new(StringComparer.OrdinalIgnoreCase);

	internal CoreWebView2HttpRequestHeaders(NSDictionary? nativeHeaders = null)
	{
		_originalHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		
		if (nativeHeaders != null)
		{
			foreach (var key in nativeHeaders.Keys)
			{
				var keyStr = key.ToString();
				var valueStr = nativeHeaders[key]?.ToString() ?? string.Empty;
				_originalHeaders[keyStr] = valueStr;
			}
		}
	}

	/// <summary>
	/// Indicates whether headers have been modified.
	/// NOTE: On iOS/macOS, modifications are tracked but cannot be applied.
	/// </summary>
	internal bool HasModifications => _addedHeaders.Count > 0 || _removedHeaders.Count > 0;

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

	public CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
	{
		var headers = new List<KeyValuePair<string, string>>();
		var value = GetHeader(name);
		if (!string.IsNullOrEmpty(value))
		{
			headers.Add(new KeyValuePair<string, string>(name, value));
		}
		return new CoreWebView2HttpHeadersCollectionIterator(headers);
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
	/// WARNING: On iOS/macOS (WKWebView), this modification is tracked but CANNOT be applied
	/// to the actual request. WKWebView does not support modifying headers after request initiation.
	/// </summary>
	public void SetHeader(string name, string value)
	{
		_removedHeaders.Remove(name);
		_addedHeaders[name] = value;
	}

	/// <summary>
	/// Removes a header.
	/// WARNING: On iOS/macOS (WKWebView), this modification is tracked but CANNOT be applied
	/// to the actual request.
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

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
#endif
