using System;
using System.Collections;
using System.Collections.Generic;
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
	public event MapChangedEventHandler<string, object> MapChanged;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public object this[string key]
	{
		get
		{
			var serializedValue = _nativeApplicationSettings[key];
			if (serializedValue is null)
			{
				return null;
			}

			return DataTypeSerializer.Deserialize(serializedValue);
		}
		set
		{
			if (value is null)
			{
				_nativeApplicationSettings.Remove(key);
			}
			else
			{
				var serializedValue = DataTypeSerializer.Serialize(value);
				_nativeApplicationSettings[key] = serializedValue;
			}
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
			throw new global::System.NotSupportedException();
		}
	}

	public bool IsReadOnly => false;

	public global::System.Collections.Generic.ICollection<string> Keys
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public global::System.Collections.Generic.ICollection<object> Values
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public void Add(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public void Add(string key, object value)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Contains(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public bool ContainsKey(string key)
	{
		throw new global::System.NotSupportedException();
	}

	public void CopyTo(global::System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Remove(string key)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

	public bool TryGetValue(string key, out object value)
	{
		throw new global::System.NotSupportedException();
	}

	public void Clear()
	{
		throw new global::System.NotSupportedException();
	}

	public bool Contains(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public void CopyTo(global::System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Remove(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator()
	{
		throw new global::System.NotSupportedException();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
