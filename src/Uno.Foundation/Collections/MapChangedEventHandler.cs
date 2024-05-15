using System.Runtime.InteropServices;

namespace Windows.Foundation.Collections;

/// <summary>
/// Represents the method that handles the changed event of an observable map.
/// </summary>
/// <typeparam name="K">Key type.</typeparam>
/// <typeparam name="V">Value type.</typeparam>
/// <param name="sender">The observable map that changed.</param>
/// <param name="event">The description of the change that occurred in the map.</param>
public delegate void MapChangedEventHandler<K, V>(
	[In] IObservableMap<K, V> sender,
	[In] IMapChangedEventArgs<K> @event);
