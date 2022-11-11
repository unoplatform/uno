using System.Collections.Generic;

namespace Windows.Foundation.Collections;

/// <summary>
/// Notifies listeners of dynamic changes to a map, such as when items are added or removed.
/// </summary>
/// <typeparam name="K">Key type.</typeparam>
/// <typeparam name="V">Value type.</typeparam>
public partial interface IObservableMap<K, V> : IDictionary<K, V>
{
	/// <summary>
	/// Occurs when the map changes.
	/// </summary>
	event MapChangedEventHandler<K, V> MapChanged;
}
