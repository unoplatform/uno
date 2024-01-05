#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;

namespace Windows.Web.Http.Headers;

public partial class HttpRequestHeaderCollection : IDictionary<string, string>, IEnumerable<KeyValuePair<string, string>>, IStringable
{
	private readonly Dictionary<string, string> _dictionary = new(StringComparer.OrdinalIgnoreCase);
	private readonly HttpRequestMessage _requestMessage;

	internal HttpRequestHeaderCollection(HttpRequestMessage requestMessage)
	{
		_requestMessage = requestMessage;
	}

	public uint Size => (uint)_dictionary.Count;

	public Uri? Referer
	{
		get
		{
			if (_dictionary.TryGetValue("Referer", out var referer))
			{
				return new Uri(referer);
			}

			return null;
		}
		set
		{
			if (value is null)
			{
				_dictionary.Remove("Referer");
				return;
			}

			_dictionary["Referer"] = new Uri(_requestMessage.RequestUri!, value).AbsoluteUri;
		}
	}

	public uint? MaxForwards
	{
		get
		{
			if (_dictionary.TryGetValue("Max-Forwards", out var maxForwards))
			{
				return uint.Parse(maxForwards, NumberFormatInfo.InvariantInfo);
			}

			return null;
		}
		set
		{
			if (value is null)
			{
				_dictionary.Remove("Max-Forwards");
				return;
			}

			_dictionary["Max-Forwards"] = value.Value.ToString(NumberFormatInfo.InvariantInfo);
		}
	}

	public DateTimeOffset? IfUnmodifiedSince
	{
		get
		{
			if (_dictionary.TryGetValue("If-Unmodified-Since", out var ifUnModifiedSince))
			{
				return DateTimeOffset.ParseExact(ifUnModifiedSince, "ddd, dd MMM yyyy hh:mm:ss GMT", DateTimeFormatInfo.InvariantInfo);
			}

			return null;
		}
		set
		{
			if (value is null)
			{
				_dictionary.Remove("If-Unmodified-Since");
				return;
			}

			_dictionary["If-Unmodified-Since"] = value.Value.ToString("ddd, dd MMM yyyy hh:mm:ss GMT", DateTimeFormatInfo.InvariantInfo);
		}
	}

	public DateTimeOffset? IfModifiedSince
	{
		get
		{
			if (_dictionary.TryGetValue("If-Modified-Since", out var ifModifiedSince))
			{
				return DateTimeOffset.ParseExact(ifModifiedSince, "ddd, dd MMM yyyy hh:mm:ss GMT", DateTimeFormatInfo.InvariantInfo);
			}

			return null;
		}
		set
		{
			if (value is null)
			{
				_dictionary.Remove("If-Modified-Since");
				return;
			}

			_dictionary["If-Modified-Since"] = value.Value.ToString("ddd, dd MMM yyyy hh:mm:ss GMT", DateTimeFormatInfo.InvariantInfo);
		}
	}

	public string From
	{
		get
		{
			if (_dictionary.TryGetValue("From", out var from))
			{
				return from;
			}

			return string.Empty;
		}
		set
		{
			if (value is null)
			{
				throw new ArgumentNullException();
			}

			if (value.Length == 0)
			{
				_dictionary.Remove("From");
				return;
			}

			_dictionary["From"] = value;
		}
	}

	public DateTimeOffset? Date
	{
		get
		{
			if (_dictionary.TryGetValue("Date", out var ifModifiedSince))
			{
				return DateTimeOffset.ParseExact(ifModifiedSince, "ddd, dd MMM yyyy hh:mm:ss GMT", DateTimeFormatInfo.InvariantInfo);
			}

			return null;
		}
		set
		{
			if (value is null)
			{
				_dictionary.Remove("Date");
				return;
			}

			_dictionary["Date"] = value.Value.ToString("ddd, dd MMM yyyy hh:mm:ss GMT", DateTimeFormatInfo.InvariantInfo);
		}
	}

	public bool ContainsKey(string key)
		=> _dictionary.ContainsKey(key);

	public bool Remove(string key)
		=> _dictionary.Remove(key);

	public bool Remove(KeyValuePair<string, string> item)
	{
		return TryGetValue(item.Key, out var value) && value.Equals(item.Value) && Remove(item.Key);
	}

	public bool TryGetValue(string key, out string value)
	{
		if (key.Equals("Referer", StringComparison.OrdinalIgnoreCase))
		{
			value = Referer?.AbsoluteUri!;
			return value is not null;
		}

		return _dictionary.TryGetValue(key, out value!);
	}

	public string this[string key]
	{
		get
		{
			if (TryGetValue(key, out var value))
			{
				return value;
			}

			throw new KeyNotFoundException();
		}
		set
		{
			if (key.Equals("Referer", StringComparison.Ordinal))
			{
				_dictionary[key] = new Uri(_requestMessage.RequestUri!, value).AbsoluteUri;
			}
			else
			{
				_dictionary[key] = value;
			}
		}
	}

	public void Clear()
		=> _dictionary.Clear();

	public int Count => _dictionary.Count;

	public bool IsReadOnly => false;
}
