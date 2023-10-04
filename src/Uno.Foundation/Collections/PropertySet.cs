#nullable enable

using System.Collections;

namespace Windows.Foundation.Collections;

/// <summary>
/// Represents a property set, which is a set of PropertyValue objects with string keys.
/// </summary>
public sealed partial class PropertySet :
	IDictionary<string, object?>,
	IEnumerable<KeyValuePair<string, object?>>,
	IObservableMap<string, object?>,
	IPropertySet
{
	private readonly Dictionary<string, object?> _dictionary = new Dictionary<string, object?>();

	/// <summary>
	/// Creates and initializes a new instance of the property set.
	/// </summary>
	public PropertySet()
	{
	}

	/// <summary>
	/// Occurs when the observable map has changed.
	/// </summary>
	public event MapChangedEventHandler<string, object?>? MapChanged;

#if HAS_UNO_WINUI
	event MapChangedEventHandler<string, object> IObservableMap<string, object>.MapChanged
	{
		add => MapChanged += value;
		remove => MapChanged -= value;
	}
#endif

	/// <summary>
	/// Gets the number of items contained in the property set.
	/// </summary>
	public uint Size => (uint)_dictionary.Count;

	/// <summary>
	/// Gets teh number of items contained in the property set.
	/// </summary>
	public int Count => _dictionary.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds an item to the property set.
	/// </summary>
	/// <param name="key">The key to insert.</param>
	/// <param name="value">The value to insert.</param>
	public void Add(string key, object? value)
	{
		_dictionary.Add(key, value);
		MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemInserted, key));
	}

	/// <summary>
	/// Indicates whether the property set has an item with the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns>True if found.</returns>
	public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

	/// <summary>
	/// Removes a key from the property set.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>True if the key was found.</returns>
	public bool Remove(string key)
	{
		var result = _dictionary.Remove(key);
		if (result)
		{
			MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemRemoved, key));
		}

		return result;
	}

	/// <summary>
	/// Tries to get the value for the specified key.
	/// </summary>
	/// <param name="key">The key to retrieve.</param>
	/// <param name="value">The value correspodning with the key.</param>
	/// <returns>True if found.</returns>
	public bool TryGetValue(string key, out object? value)
	{
		if (key is null)
		{
			throw new ArgumentNullException(nameof(key));
		}
		return _dictionary.TryGetValue(key, out value);
	}

	/// <summary>
	/// Gets or sets a value for the specified key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>Value.</returns>
	public object? this[string key]
	{
		get => _dictionary[key];
		set
		{
			// Add or update and raise map changed accrodingly			
			if (_dictionary.TryGetValue(key, out var existingValue))
			{
				if (value != existingValue)
				{
					_dictionary[key] = value;
					MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemChanged, key));
				}
			}
			else
			{
				_dictionary.Add(key, value);
				MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemInserted, key));
			}
		}
	}

	/// <summary>
	/// Returns all keys in the property set.
	/// </summary>
	public ICollection<string> Keys => _dictionary.Keys;

	/// <summary>
	/// Returns all values in the property set.
	/// </summary>
	public ICollection<object?> Values => _dictionary.Values;

	/// <summary>
	/// Adds an item to the property set.
	/// </summary>
	/// <param name="item">Item to be added.</param>
	public void Add(KeyValuePair<string, object?> item) => Add(item.Key, item.Value);

	/// <summary>
	/// Clears the property set.
	/// </summary>
	public void Clear()
	{
		_dictionary.Clear();
		MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.Reset, null));
	}

	/// <summary>
	/// Checks if the property set contains the specified item.
	/// </summary>
	/// <param name="item">Item to check.</param>
	/// <returns>True if found.</returns>
	public bool Contains(KeyValuePair<string, object?> item) =>
		TryGetValue(item.Key, out var val) && Equals(item.Value, val);

	/// <summary>
	/// Copies the property set to an array.
	/// </summary>
	/// <param name="array">Array to copy to.</param>
	/// <param name="arrayIndex">Index of the start of the copy.</param>
	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}

		if (arrayIndex < 0 || arrayIndex >= array.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(arrayIndex), "The specified index is out of bounds of the specified array.");
		}

		// Check now, before starting to copy elements
		if (array.Length - arrayIndex < Count)
		{
			throw new ArgumentException(nameof(array), "The specified space is not sufficient to copy the elements from this Collection.");
		}

		foreach (var item in _dictionary)
		{
			array[arrayIndex++] = item;
		}
	}

	/// <summary>
	/// Removes an item from the property set.
	/// </summary>
	/// <param name="item">Item to remove.</param>
	/// <returns>True if found.</returns>
	public bool Remove(KeyValuePair<string, object?> item) =>
		Contains(item) && Remove(item.Key);

	/// <summary>
	/// Returns an enumerator for the property set.
	/// </summary>
	/// <returns>Enumerator.</returns>
	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dictionary.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
