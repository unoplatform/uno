// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Sdk.MacDev;

public class PDictionary : PObjectContainer, IEnumerable<KeyValuePair<string?, PObject>>
{
	static readonly byte[] BeginMarkerBytes = Encoding.ASCII.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
	static readonly byte[] EndMarkerBytes = Encoding.ASCII.GetBytes("</plist>");

	private readonly Dictionary<string, PObject> _dict;
	private readonly List<string> _order;

	public PObject? this[string key]
	{
		get
		{
			if (_dict.TryGetValue(key, out var value))
			{
				return value;
			}

			return null;
		}
		set
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			var exists = _dict.TryGetValue(key, out var existing);
			if (!exists)
			{
				_order.Add(key);
			}

			if (value is not null)
			{
				_dict[key] = value;
			}

			if (exists)
			{
				OnChildReplaced(key, existing!, value!);
			}
			else
			{
				OnChildAdded(key, value!);
			}
		}
	}

	public void Add(string key, PObject value)
	{
		_dict.Add(key, value);
		_order.Add(key);

		OnChildAdded(key, value);
	}

	public void InsertAfter(string keyBefore, string key, PObject value)
	{
		_dict.Add(key, value);
		_order.Insert(_order.IndexOf(keyBefore) + 1, key);

		OnChildAdded(key, value);
	}

	public override int Count => _dict.Count;

	#region IEnumerable[KeyValuePair[System.String,PObject]] implementation
	public IEnumerator<KeyValuePair<string?, PObject>> GetEnumerator()
	{
		foreach (var key in _order)
		{
			yield return new KeyValuePair<string?, PObject>(key, _dict[key]);
		}
	}
	#endregion

	#region IEnumerable implementation
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion

	public PDictionary()
	{
		_dict = [];
		_order = [];
	}

	public override PObject Clone()
	{
		var clone = new PDictionary();

		foreach (var kv in this)
		{
			clone.Add(kv.Key!, kv.Value.Clone());
		}

		return clone;
	}

	public bool ContainsKey(string name) => _dict.ContainsKey(name);

	public bool Remove(string key)
	{
		if (_dict.TryGetValue(key, out var obj))
		{
			_dict.Remove(key);
			_order.Remove(key);
			OnChildRemoved(key, obj);
			return true;
		}
		return false;
	}

	public void Clear()
	{
		_dict.Clear();
		_order.Clear();
		OnCleared();
	}

	public bool ChangeKey(PObject obj, string newKey) => ChangeKey(obj, newKey, null);

	public bool ChangeKey(PObject obj, string newKey, PObject? newValue)
	{
		var oldkey = GetKey(obj);
		if (oldkey is null || _dict.ContainsKey(newKey))
		{
			return false;
		}

		_dict.Remove(oldkey);
		_dict.Add(newKey, newValue ?? obj);
		_order[_order.IndexOf(oldkey)] = newKey;
		if (newValue is not null)
		{
			OnChildRemoved(oldkey, obj);
			OnChildAdded(newKey, newValue);
		}
		else
		{
			OnChildRemoved(oldkey, obj);
			OnChildAdded(newKey, obj);
		}
		return true;
	}

	public string? GetKey(PObject obj)
	{
		foreach (var pair in _dict)
		{
			if (pair.Value == obj)
			{
				return pair.Key;
			}
		}
		return null;
	}

	public T? Get<T>(string key) where T : PObject
	{
		if (!_dict.TryGetValue(key, out var obj))
		{
			return null;
		}

		return obj as T;
	}

	public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value) where T : PObject
	{
		if (_dict.TryGetValue(key, out var obj) && obj is T tobj)
		{
			value = tobj;
			return true;
		}

		value = default;
		return false;
	}

	static int IndexOf(byte[] haystack, int startIndex, byte[] needle)
	{
		var maxLength = haystack.Length - needle.Length;
		int n;

		for (var i = startIndex; i < maxLength; i++)
		{
			for (n = 0; n < needle.Length; n++)
			{
				if (haystack[i + n] != needle[n])
				{
					break;
				}
			}

			if (n == needle.Length)
			{
				return i;
			}
		}

		return -1;
	}

	public static new PDictionary? FromByteArray(byte[] array, int startIndex, int length, out bool isBinary) => (PDictionary?)PObject.FromByteArray(array, startIndex, length, out isBinary);

	public static new PDictionary? FromByteArray(byte[] array, out bool isBinary) => (PDictionary?)PObject.FromByteArray(array, out isBinary);

	public static PDictionary? FromBinaryXml(byte[] array)
	{
		//find the raw plist within the .mobileprovision file
		var start = IndexOf(array, 0, BeginMarkerBytes);
		int length;

		if (start < 0 || (length = (IndexOf(array, start, EndMarkerBytes) - start)) < 1)
		{
			throw new Exception("Did not find XML plist in buffer.");
		}

		length += EndMarkerBytes.Length;

		return FromByteArray(array, start, length, out var _);
	}

	public new static PDictionary? FromFile(string fileName) => FromFile(fileName, out _);

	public new static Task<PDictionary?> FromFileAsync(string fileName) => Task.Run(() =>
	{
		return FromFile(fileName, out _);
	});

	public new static PDictionary? FromFile(string fileName, out bool isBinary) => (PDictionary?)PObject.FromFile(fileName, out isBinary);

	public static PDictionary? FromBinaryXml(string fileName) => FromBinaryXml(File.ReadAllBytes(fileName));

	protected override bool Reload(PropertyListFormat.ReadWriteContext ctx)
	{
		SuppressChangeEvents = true;
		var result = ctx.ReadDict(this);
		SuppressChangeEvents = false;
		if (result)
		{
			OnChanged(EventArgs.Empty);
		}

		return result;
	}

	public override string ToString() => string.Format(CultureInfo.InvariantCulture, "[PDictionary: Items={0}]", _dict.Count);

	public void SetString(string key, string value)
	{
		var result = Get<PString>(key);

		if (result is null)
		{
			this[key] = new PString(value);
		}
		else
		{
			result.Value = value;
		}
	}

	public PString GetString(string key)
	{
		var result = Get<PString>(key);

		if (result is null)
		{
			this[key] = result = new PString("");
		}

		return result;
	}

	public PArray GetArray(string key)
	{
		var result = Get<PArray>(key);

		if (result is null)
		{
			this[key] = result = [];
		}

		return result;
	}

	public override PObjectType Type => PObjectType.Dictionary;
}
