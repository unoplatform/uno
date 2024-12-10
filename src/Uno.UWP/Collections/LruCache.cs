using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Collections;

public partial class LruCache<TKey, TValue>
{
	// essentially, a KeyValuePair where the value is mutable
	private class KeyedItem
	{
		public TKey Key { get; init; }
		public TValue Value { get; set; }

		public KeyedItem(TKey key, TValue value)
		{
			this.Key = key;
			this.Value = value;
		}
	}
}

/// <summary>
/// A least-recently-used cache, keeping only the last n added/updated items.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public partial class LruCache<TKey, TValue> where TKey : notnull
{
	private readonly Dictionary<TKey, LinkedListNode<KeyedItem>> _map = new();
	private readonly LinkedList<KeyedItem> _list = new();
	private int _capacity;

	public int Count => _map.Count;
	public int Capacity
	{
		get => _capacity;
		set => UpdateCapacity(value);
	}

	public TValue this[TKey key]
	{
		get => Get(key);
		set => Put(key, value);
	}

	public LruCache(int capacity)
	{
		if (capacity < 0) throw new ArgumentOutOfRangeException("capacity must be positive or zero.");

		this._capacity = capacity;
	}

	public TValue Get(TKey key)
	{
		return TryGetValue(key, out var value) ? value : default;
	}
	public bool TryGetValue(TKey key, out TValue value)
	{
		if (_map.TryGetValue(key, out var node))
		{
			_list.Remove(node);
			_list.AddFirst(node);

			value = node.Value.Value;
			return true;
		}
		else
		{
			value = default;
			return false;
		}
	}
	public void Put(TKey key, TValue value)
	{
		if (_capacity == 0) return;

		if (_map.TryGetValue(key, out var node))
		{
			node.Value.Value = value;

			_list.Remove(node);
			_list.AddFirst(node);
		}
		else
		{
			while (_map.Count >= _capacity)
			{
				_map.Remove(_list.Last.Value.Key);
				_list.RemoveLast();
			}

			_map.Add(key, _list.AddFirst(new KeyedItem(key, value)));
		}
	}

	public void UpdateCapacity(int capacity)
	{
		if (capacity < 0) throw new ArgumentOutOfRangeException("capacity must be positive or zero.");

		_capacity = capacity;
		if (_capacity == 0)
		{
			_map.Clear();
			_list.Clear();
		}
		else
		{
			while (_map.Count > _capacity)
			{
				_map.Remove(_list.Last.Value.Key);
				_list.RemoveLast();
			}
		}
	}
}
