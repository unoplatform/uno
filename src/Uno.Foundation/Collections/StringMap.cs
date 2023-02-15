using System.Collections;

namespace Windows.Foundation.Collections;

/// <summary>
/// An associative collection, also known as a map or a dictionary.
/// </summary>
public sealed partial class StringMap :
	IDictionary<string, string>,
	IEnumerable<KeyValuePair<string, string>>,
	IObservableMap<string, string>
{
	private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

	/// <summary>
	/// Creates and initializes a new instance of the string map.
	/// </summary>
	public StringMap()
	{
	}

	/// <summary>
	/// Occurs when the observable map has changed.
	/// </summary>
	public event MapChangedEventHandler<string, string> MapChanged;

	/// <summary>
	/// Gets the number of items contained in the string map.
	/// </summary>
	public uint Size => (uint)_strings.Count;

	/// <summary>
	/// Gets teh number of items contained in the string map.
	/// </summary>
	public int Count => _strings.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds an item to the string map.
	/// </summary>
	/// <param name="key">The key to insert.</param>
	/// <param name="value">The value to insert.</param>
	public void Add(string key, string value)
	{
		_strings.Add(key, value);
		MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemInserted, key));
	}

	/// <summary>
	/// Indicates whether the string map has an item with the specified key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns>True if found.</returns>
	public bool ContainsKey(string key) => _strings.ContainsKey(key);

	/// <summary>
	/// Removes a key from the string map.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>True if the key was found.</returns>
	public bool Remove(string key)
	{
		var result = _strings.Remove(key);
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
	public bool TryGetValue(string key, out string value) => _strings.TryGetValue(key, out value);

	/// <summary>
	/// Gets or sets a value for the specified key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>Value.</returns>
	public string this[string key]
	{
		get => _strings[key];
		set
		{
			// Add or update and raise map changed accrodingly			
			if (_strings.TryGetValue(key, out var existingValue))
			{
				if (value != existingValue)
				{
					_strings[key] = value;
					MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemChanged, key));
				}
			}
			else
			{
				_strings.Add(key, value);
				MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemInserted, key));
			}
		}
	}

	/// <summary>
	/// Returns all keys in the string map.
	/// </summary>
	public ICollection<string> Keys => _strings.Keys;

	/// <summary>
	/// Returns all values in the string map.
	/// </summary>
	public ICollection<string> Values => _strings.Values;

	/// <summary>
	/// Adds an item to the string map.
	/// </summary>
	/// <param name="item">Item to be added.</param>
	public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

	/// <summary>
	/// Clears the string map.
	/// </summary>
	public void Clear() => _strings.Clear();

	/// <summary>
	/// Checks if the string map contains the specified item.
	/// </summary>
	/// <param name="item">Item to check.</param>
	/// <returns>True if found.</returns>
	public bool Contains(KeyValuePair<string, string> item) => _strings.Contains(item);

	/// <summary>
	/// Copies the string map to an array.
	/// </summary>
	/// <param name="array">Array to copy to.</param>
	/// <param name="arrayIndex">Index of the start of the copy.</param>
	public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
	{
		foreach (var item in _strings)
		{
			array[arrayIndex++] = item;
		}
	}

	/// <summary>
	/// Removes an item from the string map.
	/// </summary>
	/// <param name="item">Item to remove.</param>
	/// <returns>True if found.</returns>
	public bool Remove(KeyValuePair<string, string> item) => Remove(item.Key);

	/// <summary>
	/// Returns an enumerator for the string map.
	/// </summary>
	/// <returns>Enumerator.</returns>
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _strings.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
