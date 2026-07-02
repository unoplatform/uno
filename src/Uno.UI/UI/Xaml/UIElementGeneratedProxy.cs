using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Uno.UI.Xaml;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class UIElementGeneratedProxy
{
	// Keyed by control Type through a ConditionalWeakTable (weak keys): a control type from a collectible
	// AssemblyLoadContext (plugin / preview hosting) must not be pinned here (Type -> RuntimeType ->
	// LoaderAllocator). A miss here would be misread as "implements no routed events", so the entry must
	// still be cached (not skipped) — weak keys keep caching while letting a collectible type collect once
	// otherwise unreferenced. StrongBox wraps the enum value (CWT values must be reference types).
	private static readonly ConditionalWeakTable<Type, StrongBox<RoutedEventFlag>> _implementedRoutedEvents = new();

	public static RoutedEventFlag RegisterImplementedRoutedEvents(Type type, RoutedEventFlag routedEventFlags)
	{
		_implementedRoutedEvents.AddOrUpdate(type, new StrongBox<RoutedEventFlag>(routedEventFlags));
		return routedEventFlags;
	}

	internal static bool TryGetImplementedRoutedEvents(Type type, out RoutedEventFlag routedEventFlags)
	{
		if (_implementedRoutedEvents.TryGetValue(type, out var box))
		{
			routedEventFlags = box.Value;
			return true;
		}

		routedEventFlags = default;
		return false;
	}

	internal static void ClearCachesForNonDefaultAlc()
	{
		// Weak keys self-clean; eagerly drop collectible-ALC entries on teardown too.
		var keysToRemove = new List<Type>();
		foreach (var entry in _implementedRoutedEvents)
		{
			var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(entry.Key.Assembly);
			if (alc is not null && alc != System.Runtime.Loader.AssemblyLoadContext.Default)
			{
				keysToRemove.Add(entry.Key);
			}
		}

		foreach (var key in keysToRemove)
		{
			_implementedRoutedEvents.Remove(key);
		}
	}
}
