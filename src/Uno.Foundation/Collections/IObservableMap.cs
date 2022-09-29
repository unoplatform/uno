#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Windows.Foundation.Collections
{
	public delegate void MapChangedEventHandler<K, V>([In] IObservableMap<K, V> sender, [In] IMapChangedEventArgs<K> @event);

	public partial interface IObservableMap<K, V> : IDictionary<K, V>
	{
		/// <summary>Occurs when the map changes.</summary>
		event MapChangedEventHandler<K, V> MapChanged;
	}
}
