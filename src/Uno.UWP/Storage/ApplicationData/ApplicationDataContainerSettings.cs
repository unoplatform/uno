using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Windows.Storage;

/// <summary>
/// Provides access to the settings in a settings container. The ApplicationDataContainer.Values property returns an object that can be cast to this type.
/// </summary>
public partial class ApplicationDataContainerSettings : IPropertySet, IObservableMap<string, object>, IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
{
	private readonly ApplicationDataContainer _container;
	private readonly ApplicationDataLocality _locality;

	internal ApplicationDataContainerSettings(ApplicationDataContainer container, ApplicationDataLocality locality)
	{
		_container = container ?? throw new ArgumentNullException(nameof(container));
		_locality = locality;

		InitializePlatform();
	}

	partial void InitializePlatform();

	/// <summary>
	/// Occurs when the map changes.
	/// </summary>
	public event MapChangedEventHandler<string, object> MapChanged;

	/// <summary>
	/// Gets the number of related application settings.
	/// </summary>
	public uint Size => (uint)Count;

	public bool Contains(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}

	public void CopyTo(global::System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new global::System.NotSupportedException();
	}

	public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

	public bool IsReadOnly => false;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
