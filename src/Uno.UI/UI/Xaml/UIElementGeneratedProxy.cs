using System;
using System.Collections.Generic;

namespace Uno.UI.Xaml;

public static class UIElementGeneratedProxy
{
	private static readonly Dictionary<Type, RoutedEventFlag> _implementedRoutedEvents = new();

	public static void RegisterImplementedRoutedEvents(Type type, RoutedEventFlag routedEventFlags)
	{
		lock (_implementedRoutedEvents)
		{
			_implementedRoutedEvents[type] = routedEventFlags;
		}
	}

	internal static bool TryGetImplementedRoutedEvents(Type type, out RoutedEventFlag routedEventFlags) =>
		_implementedRoutedEvents.TryGetValue(type, out routedEventFlags);
}
