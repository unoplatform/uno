#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using System.Threading;
using Uno.Collections;
using System.Collections;

#if __ANDROID__
using _View = Android.Views.View;
#elif __APPLE_UIKIT__
using _View = UIKit.UIView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private class NameToPropertyDictionary
		{
			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(PropertyCacheEntry.DefaultComparer, usePooling: false);

			internal bool TryGetValue(PropertyCacheEntry key, out DependencyProperty? result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (DependencyProperty?)value;

					return true;
				}

				result = null;
				return false;
			}

			internal void Add(PropertyCacheEntry key, DependencyProperty? dependencyProperty)
			{
				// Do not cache entries whose owner type is from a collectible (non-default) ALC. The key is
				// a composite (CachedType + name), so it cannot be weak-keyed like the Type-keyed caches;
				// caching it here would retain the type (via DependencyProperty._ownerType) and pin its
				// ALC, and the eviction sweep loses the race to the unloading app re-populating it. Such
				// lookups re-resolve through the (weak-keyed) registry — cheap and pin-free. Framework
				// (default-ALC) types are cached as before.
				var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(key.CachedType.Assembly);
				if (alc is not null && alc != global::System.Runtime.Loader.AssemblyLoadContext.Default)
				{
					return;
				}

				_entries.Add(key, dependencyProperty);
			}

			internal void Remove(PropertyCacheEntry propertyCacheEntry)
				=> _entries.Remove(propertyCacheEntry);

			internal int Count => _entries.Count;

			internal void Clear() => _entries.Clear();

			/// <summary>
			/// Removes entries whose <see cref="PropertyCacheEntry.CachedType"/> belongs to
			/// a non-default (collectible) <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.
			/// </summary>
			internal void RemoveNonDefaultAlcEntries()
			{
				var defaultAlc = global::System.Runtime.Loader.AssemblyLoadContext.Default;
				var keysToRemove = new global::System.Collections.Generic.List<PropertyCacheEntry>();

				foreach (PropertyCacheEntry key in _entries.Keys)
				{
					var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(key.CachedType.Assembly);
					if (alc is not null && alc != defaultAlc)
					{
						keysToRemove.Add(key);
					}
				}

				foreach (var key in keysToRemove)
				{
					_entries.Remove(key);
				}
			}
		}
	}
}
