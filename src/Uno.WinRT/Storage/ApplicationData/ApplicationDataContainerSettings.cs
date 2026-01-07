using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Specialized;
using Uno.Storage;
using Windows.Foundation.Collections;

namespace Windows.Storage;

/// <summary>
/// Provides access to the settings in a settings container. The ApplicationDataContainer.Values
/// property returns an object that can be cast to this type.
/// </summary>
public partial class ApplicationDataContainerSettings : IPropertySet, IObservableMap<string, object>, IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
{
	private readonly ApplicationDataContainer _container;
	private readonly ApplicationDataLocality _locality;
	private readonly NativeApplicationSettings _nativeApplicationSettings;

	internal ApplicationDataContainerSettings(ApplicationDataContainer container, ApplicationDataLocality locality)
	{
		_container = container ?? throw new ArgumentNullException(nameof(container));
		_locality = locality;

		_nativeApplicationSettings = NativeApplicationSettings.GetForLocality(locality);
	}

	/// <summary>
	/// Occurs when the map changes.
	/// </summary>
	/// <remarks>
	/// Note: WinUI's ApplicationDataContainerSettings does not raise this event even though
	/// the class implements IObservableMap. Uno Platform raises this event for consistency
	/// with other IObservableMap implementations.
	/// </remarks>
	public event MapChangedEventHandler<string, object> MapChanged;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	public object this[string key]
	{
		get => _nativeApplicationSettings[_container.GetSettingKey(key)];
		set
		{
			var exists = ContainsKey(key);
			_nativeApplicationSettings[_container.GetSettingKey(key)] = value;
			MapChanged?.Invoke(this, new MapChangedEventArgs(
				exists ? CollectionChange.ItemChanged : CollectionChange.ItemInserted,
				key));
		}
	}

	/// <summary>
	/// Gets the number of related application settings.
	/// </summary>
	public uint Size => (uint)Count;

	public int Count
	{
		get
		{
			// Public settings count is equal to the total number of settings in the container excluding internal settings.
			var allSettingsCount = _nativeApplicationSettings.GetKeysWithPrefix(_container.ContainerPath).Count();
			var internalSettingsPath = _container.GetSettingKey(ApplicationDataContainer.InternalSettingPrefix);
			var internalSettingsCount = _nativeApplicationSettings.GetKeysWithPrefix(internalSettingsPath).Count();

			return allSettingsCount - internalSettingsCount;
		}
	}

	public bool IsReadOnly => false;

	public ICollection<string> Keys => _nativeApplicationSettings
		.GetKeys(IsCurrentContainerPublicKey)
		.ToArray();

	public ICollection<object> Values => _nativeApplicationSettings.GetValues(Keys).ToArray();

	public void Add(string key, object value)
	{
		if (ContainsKey(key))
		{
			throw new ArgumentException("An item with the same key has already been added.");
		}

		if (value != null)
		{
			_nativeApplicationSettings[_container.GetSettingKey(key)] = value;
			MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemInserted, key));
		}
	}

	public void Add(KeyValuePair<string, object> item)
		=> Add(item.Key, item.Value);

	public bool Contains(global::System.Collections.Generic.KeyValuePair<string, object> item) =>
		ContainsKey(item.Key) &&
		Equals(
			_nativeApplicationSettings[_container.GetSettingKey(item.Key)],
			item.Value
		);

	public bool ContainsKey(string key) => _nativeApplicationSettings.ContainsKey(_container.GetSettingKey(key));

	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
	{
		if (array is null)
		{
			throw new ArgumentNullException(nameof(array));
		}
		if (arrayIndex < 0 || arrayIndex >= array.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		}
		if (array.Length - arrayIndex < Count)
		{
			throw new ArgumentException("The number of elements in the source dictionary is greater than the available space from arrayIndex to the end of the destination array.");
		}
		foreach (var kvp in this)
		{
			array[arrayIndex++] = kvp;
		}
	}

	public bool Remove(string key)
	{
		var removed = _nativeApplicationSettings.Remove(_container.GetSettingKey(key));
		if (removed)
		{
			MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.ItemRemoved, key));
		}
		return removed;
	}

	public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

	public bool TryGetValue(string key, out object value) =>
		_nativeApplicationSettings.TryGetValue(_container.GetSettingKey(key), out value);

	public void Clear()
	{
		_container.DeleteAllSubcontainers();
		_nativeApplicationSettings.RemoveKeys(IsCurrentContainerPublicKey);
		MapChanged?.Invoke(this, new MapChangedEventArgs(CollectionChange.Reset, null));
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		foreach (var key in Keys)
		{
			yield return new KeyValuePair<string, object>(key, this[key]);
		}
	}

	internal void ClearIncludingInternal() =>
		_nativeApplicationSettings.RemoveKeys(IsCurrentContainerInternalKey);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private bool IsCurrentContainerKey(string key) => key.StartsWith(_container.ContainerPath, StringComparison.Ordinal);

	private bool IsInternalKey(string key) =>
		key.StartsWith(_container.GetSettingKey(ApplicationDataContainer.InternalSettingPrefix), StringComparison.Ordinal);

	private bool IsCurrentContainerPublicKey(string key) =>
		IsCurrentContainerKey(key) && !IsInternalKey(key);

	private bool IsCurrentContainerInternalKey(string key) =>
		IsCurrentContainerKey(key) && IsInternalKey(key);
}
