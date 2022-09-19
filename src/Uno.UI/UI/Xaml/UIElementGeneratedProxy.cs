using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Uno.UI.Xaml;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class UIElementGeneratedProxy
{
	private static readonly Dictionary<Type, RoutedEventFlag> _implementedRoutedEvents = new();

	public static RoutedEventFlag RegisterImplementedRoutedEvents(Type type, RoutedEventFlag routedEventFlags)
	{
		lock (_implementedRoutedEvents)
		{
			_implementedRoutedEvents[type] = routedEventFlags;
			return routedEventFlags;
		}
	}

	internal static bool TryGetImplementedRoutedEvents(Type type, out RoutedEventFlag routedEventFlags) =>
		_implementedRoutedEvents.TryGetValue(type, out routedEventFlags);
}
