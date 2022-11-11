#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Uno.Foundation.Collections;

/// <summary>
/// Represents a variant of dictionary that allows adding a value for a null key.
/// </summary>
/// <typeparam name="TKey">Key.</typeparam>
/// <typeparam name="TValue">Value.</typeparam>
internal class NullableKeyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	  where TKey : class
{
	private readonly Dictionary<TKey, TValue> _dictionary;
	private TValue _nullKeyValue = default!;
	private bool _containsNullKey;

	public NullableKeyDictionary() =>
		_dictionary = new Dictionary<TKey, TValue>();

	public NullableKeyDictionary(IEqualityComparer<TKey> comparer) =>
		_dictionary = new Dictionary<TKey, TValue>(comparer);

	public bool ContainsKey(TKey? key) => key == null ? _containsNullKey : _dictionary.ContainsKey(key);

	public void Add(TKey? key, TValue value)
	{
		if (key == null)
		{
			if (_containsNullKey)
			{
				throw new ArgumentException("Value already present");
			}

			_nullKeyValue = value;
			_containsNullKey = true;
		}
		else
		{
			_dictionary.Add(key, value);
		}
	}

	public bool Remove(TKey? key)
	{
		if (key != null)
		{
			return _dictionary.Remove(key);
		}

		if (_containsNullKey)
		{
			_nullKeyValue = default!;
			_containsNullKey = false;
			return true;
		}

		return false;
	}

	public bool TryGetValue(TKey? key, out TValue value)
	{
		if (key != null)
		{
			return _dictionary.TryGetValue(key, out value!);
		}
		else
		{
			value = _containsNullKey ? _nullKeyValue : default!;
			return _containsNullKey;
		}
	}

	public TValue this[TKey? key]
	{
		get
		{
			if (key != null)
			{
				return _dictionary[key];
			}
			else
			{
				if (!_containsNullKey)
				{
					throw new KeyNotFoundException("Null key is not present");
				}

				return _nullKeyValue!;
			}
		}
		set
		{
			if (key != null)
			{
				_dictionary[key] = value;
			}
			else
			{
				_nullKeyValue = value;
				_containsNullKey = true;
			}
		}
	}

	public ICollection<TKey> Keys
	{
		get
		{
			if (!_containsNullKey)
			{
				return _dictionary.Keys;
			}
			else
			{
				var keys = new List<TKey>(_dictionary.Keys);
				keys.Add(null!);
				return keys;
			}
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			if (!_containsNullKey)
			{
				return _dictionary.Values;
			}
			else
			{
				var values = new List<TValue>(_dictionary.Values);
				values.Add(_nullKeyValue);
				return values;
			}
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
		{
			yield return kvp;
		}

		if (_containsNullKey)
		{
			yield return new KeyValuePair<TKey, TValue>(null!, _nullKeyValue);
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

	public void Clear()
	{
		_dictionary.Clear();
		_nullKeyValue = default!;
		_containsNullKey = false;
	}

	public bool Contains(KeyValuePair<TKey, TValue> item) =>
		TryGetValue(item.Key, out var val) ? Equals(item.Value, val) : false;

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		foreach (var pair in this)
		{
			array[arrayIndex] = pair;
			arrayIndex++;
		}
	}

	public bool Remove(KeyValuePair<TKey, TValue> item) => Contains(item) ? Remove(item.Key) : false;

	public int Count => _containsNullKey ? _dictionary.Count + 1 : _dictionary.Count;

	public bool IsReadOnly => false;
}
