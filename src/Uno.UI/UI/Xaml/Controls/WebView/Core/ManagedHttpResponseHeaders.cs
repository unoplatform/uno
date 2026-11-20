#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// An ordered, case-insensitive HTTP response header list that may hold several values per name.
/// </summary>
internal sealed class ManagedHttpResponseHeaders : INativeHttpResponseHeaders
{
	private readonly List<KeyValuePair<string, string>> _headers = new();

	/// <summary>
	/// Parses a WebView2 header string: <c>Name: Value</c> pairs separated by CRLF.
	/// </summary>
	/// <remarks>
	/// Parsing is lenient: lines are trimmed, blank lines and lines without a name are skipped, and only the first
	/// colon separates the name from the value.
	/// </remarks>
	public static ManagedHttpResponseHeaders Parse(string? headers)
	{
		var result = new ManagedHttpResponseHeaders();

		if (string.IsNullOrEmpty(headers))
		{
			return result;
		}

		foreach (var line in headers.Split('\n'))
		{
			var separator = line.IndexOf(':');
			if (separator < 0)
			{
				continue;
			}

			var name = line.Substring(0, separator).Trim();
			if (name.Length == 0)
			{
				continue;
			}

			result.AppendHeader(name, line.Substring(separator + 1).Trim());
		}

		return result;
	}

	public void AppendHeader(string name, string value)
	{
		ArgumentNullException.ThrowIfNull(name);

		_headers.Add(new(name, value ?? string.Empty));
	}

	public bool Contains(string name) => IndexOf(name) >= 0;

	/// <exception cref="ArgumentException">No header is named <paramref name="name"/>.</exception>
	public string GetHeader(string name)
	{
		var index = IndexOf(name);

		return index >= 0
			? _headers[index].Value
			: throw new ArgumentException($"The response has no '{name}' header.", nameof(name));
	}

	public object GetHeaders(string name)
		=> new ManagedHttpHeadersCollectionIterator(_headers.FindAll(header => IsMatch(header, name)));

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private int IndexOf(string name) => _headers.FindIndex(header => IsMatch(header, name));

	private static bool IsMatch(KeyValuePair<string, string> header, string name)
		=> string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Iterates a snapshot of headers with WinRT <c>IIterator</c> semantics: positioned on the first item when created.
/// </summary>
internal sealed class ManagedHttpHeadersCollectionIterator : INativeHttpHeadersCollectionIterator
{
	private readonly IReadOnlyList<KeyValuePair<string, string>> _headers;
	private int _index;

	public ManagedHttpHeadersCollectionIterator(IReadOnlyList<KeyValuePair<string, string>> headers) => _headers = headers;

	public object Current
		=> HasCurrent ? _headers[_index] : throw new InvalidOperationException("The iterator has no current header.");

	public bool HasCurrent => _index < _headers.Count;

	public bool MoveNext()
	{
		if (HasCurrent)
		{
			_index++;
		}

		return HasCurrent;
	}

	public uint GetMany(object items)
	{
		if (items is not KeyValuePair<string, string>[] array)
		{
			return 0;
		}

		uint count = 0;
		while (count < array.Length && HasCurrent)
		{
			array[count++] = _headers[_index++];
		}

		return count;
	}
}
