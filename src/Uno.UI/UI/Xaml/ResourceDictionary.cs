#define DEBUG_SET_RESOURCE_SOURCE
using System;
using System.Collections.Generic;
using System.Threading;
using Uno.UI;
using Uno.Extensions;
using System.ComponentModel;
using Uno.UI.Xaml;
using System.Linq;
using System.Diagnostics;
using Windows.UI.Input.Spatial;

using ResourceKey = Microsoft.UI.Xaml.SpecializedResourceDictionary.ResourceKey;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Microsoft.UI.Xaml
{
	public partial class ResourceDictionary : DependencyObject, IDependencyObjectParse, IDictionary<object, object>
	{
		private readonly SpecializedResourceDictionary _values = new SpecializedResourceDictionary();
		private readonly ObservableCollection<ResourceDictionary> _mergedDictionaries = new();
		private ResourceDictionary _themeDictionaries;
		private ResourceDictionary _parent;
		private ManagedWeakReference _sourceDictionary;
		private HashSet<ResourceKey> _keyNotFoundCache;

		/// <summary>
		/// This event is fired when a key that has value of type <see cref="ResourceDictionary"/> is added or changed in the current <see cref="ResourceDictionary" />
		/// </summary>
		private event EventHandler ResourceDictionaryValueChange;

		/// <summary>
		/// If true, there may be lazily-set values in the dictionary that need to be initialized.
		/// </summary>
		private bool _hasUnmaterializedItems;

		public ResourceDictionary()
		{
			_mergedDictionaries.CollectionChanged += (s, e) =>
			{
				if (e.OldItems != null)
				{
					foreach (ResourceDictionary oldDict in e.OldItems)
					{
						oldDict._parent = null;
					}
				}
				if (e.NewItems != null)
				{
					foreach (ResourceDictionary newDict in e.NewItems)
					{
						newDict._parent = this;
					}

					InvalidateNotFoundCache(true);
				}
			};
		}

		private Uri _source;
		public Uri Source
		{
			get => _source;
			set
			{
				if (!IsParsing) // If we're parsing, the Source is being set as a 'FYI', don't try to resolve it
				{
					var sourceDictionary = ResourceResolver.RetrieveDictionaryForSource(value);

					CopyFrom(sourceDictionary);
				}

				_source = value;
			}
		}

		public IList<ResourceDictionary> MergedDictionaries => _mergedDictionaries;
		public IDictionary<object, object> ThemeDictionaries => GetOrCreateThemeDictionaries();

		/// <summary>
		/// Determines if this instance is empty
		/// </summary>
		internal bool IsEmpty
			=> Count == 0
				&& ThemeDictionaries.Count == 0
				&& MergedDictionaries.Count == 0;

		private ResourceDictionary GetOrCreateThemeDictionaries()
		{
			if (_themeDictionaries is null)
			{
				_themeDictionaries = new ResourceDictionary() { _parent = this };
				_themeDictionaries.ResourceDictionaryValueChange += (sender, e) =>
				{
					// Invalidate the cache whenever a theme dictionary is added/removed.
					// This is safest and avoids handling edge cases.
					// Note that adding or removing theme dictionary isn't very common,
					// so invalidating the cache shouldn't be a performance issue.
					_activeTheme = ResourceKey.Empty;
				};
			}

			return _themeDictionaries;
		}

		/// <summary>
		/// Is this a ResourceDictionary created from system resources, ie within the Uno.UI assembly?
		/// </summary>
		internal bool IsSystemDictionary { get; set; }


		private HashSet<ResourceKey> KeyNotFoundCache
			=> _keyNotFoundCache ??= new(SpecializedResourceDictionary.ResourceKeyComparer.Default);

		internal object Lookup(object key)
		{
			if (!TryGetValue(key, out var value))
			{
				return null;
			}

			return value;
		}

		internal object Lookup(string key)
		{
			if (!TryGetValue(key, out var value))
			{
				return null;
			}

			return value;
		}

		/// <remarks>This method does not exist in C# UWP API
		/// and can be removed as breaking change later.</remarks>
		public bool HasKey(object key) => ContainsKey(key);

		/// <remarks>This method does not exist in C# UWP API
		/// and can be removed as breaking change later.</remarks>
		public bool Insert(object key, object value)
		{
			if (key is { })
			{
				Set(new ResourceKey(key), value, throwIfPresent: false);
				return true;
			}
			else
			{
				// This case is present to support XAML resources trimming
				// https://github.com/unoplatform/uno/issues/6564
				return false;
			}
		}

		public bool Remove(object key)
		{
			var keyToRemove = new ResourceKey(key);
#if __SKIA__ || __WASM__ || __ANDROID__
			if (_values.TryGetValue(keyToRemove, out var value))
			{
				_values.Remove(keyToRemove);
				if (value is FrameworkElement fe)
				{
#if UNO_HAS_ENHANCED_LIFECYCLE
					fe.LeaveImpl(new LeaveParams());
#else
					fe.PerformOnUnloaded(isFromResources: true);
#endif
				}

				ResourceDictionaryValueChange?.Invoke(this, EventArgs.Empty);
				return true;
			}
#else
			if (_values.Remove(keyToRemove))
			{
				ResourceDictionaryValueChange?.Invoke(this, EventArgs.Empty);
				return true;
			}
#endif

			return false;
		}

		public bool Remove(KeyValuePair<object, object> key) => Remove(key.Key);

		public void Clear()
		{
			_values.Clear();
			ResourceDictionaryValueChange?.Invoke(this, EventArgs.Empty);
		}

		public void Add(object key, object value) => Set(new ResourceKey(key), value, throwIfPresent: true);

		public bool ContainsKey(object key) => ContainsKey(key, shouldCheckSystem: true);

		public bool ContainsKey(object key, bool shouldCheckSystem)
		{
			return ContainsKey(new ResourceKey(key), shouldCheckSystem);
		}

		internal bool ContainsKey(ResourceKey resourceKey, bool shouldCheckSystem)
		{
			return _values.ContainsKey(resourceKey)
			|| ContainsKeyMerged(resourceKey)
			|| ContainsKeyTheme(resourceKey, Themes.Active)
			|| (shouldCheckSystem && !IsSystemDictionary && ResourceResolver.ContainsKeySystem(resourceKey));
		}

		public bool TryGetValue(object key, out object value)
			=> TryGetValue(key, out value, shouldCheckSystem: true);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetValue(object key, out object value, bool shouldCheckSystem)
			=> TryGetValue(new ResourceKey(key), out value, shouldCheckSystem);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(string resourceKey, out object value, bool shouldCheckSystem)
			=> TryGetValue(new ResourceKey(resourceKey), out value, shouldCheckSystem);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(Type resourceKey, out object value, bool shouldCheckSystem)
			=> TryGetValue(new ResourceKey(resourceKey), out value, shouldCheckSystem);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(in ResourceKey resourceKey, out object value, bool shouldCheckSystem)
		{
			bool useKeysNotFoundCache = resourceKey.ShouldFilter;
			var modifiedKey = resourceKey;

			if (useKeysNotFoundCache)
			{
				if (!shouldCheckSystem && KeyNotFoundCache.Contains(resourceKey))
				{
					value = null;
					return false;
				}

				modifiedKey = modifiedKey with { ShouldFilter = false };
			}

			if (_values.TryGetValue(modifiedKey, out value))
			{
				if (value is SpecialValue)
				{
					TryMaterializeLazy(resourceKey, ref value);
					TryResolveAlias(ref value);
				}

#if DEBUG && DEBUG_SET_RESOURCE_SOURCE
				TryApplySource(value, resourceKey);
#endif
				return true;
			}

			if (GetFromMerged(modifiedKey, out value))
			{
				return true;
			}

			if (GetActiveThemeDictionary(Themes.Active) is { } activeThemeDictionary
				&& activeThemeDictionary.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
			{
				return true;
			}

			if (shouldCheckSystem && !IsSystemDictionary) // We don't fall back on system resources from within a system-defined dictionary, to avoid an infinite recurse
			{
				return ResourceResolver.TrySystemResourceRetrieval(modifiedKey, out value);
			}

			if (useKeysNotFoundCache && !shouldCheckSystem)
			{
				KeyNotFoundCache.Add(resourceKey);
			}

			return false;
		}

		public object this[object key]
		{
			get
			{
				object value;
				TryGetValue(key, out value);

				return value;
			}
			set
			{
				if (!(key is null))
				{
					Set(new ResourceKey(key), value, throwIfPresent: false);
				}
			}
		}

		private void Set(in ResourceKey resourceKey, object value, bool throwIfPresent)
		{
			if (throwIfPresent && _values.ContainsKey(resourceKey))
			{
				throw new ArgumentException("An entry with the same key already exists.");
			}

			if (value is WeakResourceInitializer lazyResourceInitializer)
			{
				value = lazyResourceInitializer.Initializer;
			}

			if (value is ResourceInitializer resourceInitializer)
			{
				_hasUnmaterializedItems = true;
				_values[resourceKey] = new LazyInitializer(ResourceResolver.CurrentScope, resourceInitializer);
				ResourceDictionaryValueChange?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				_values.AddOrUpdate(resourceKey, value, out var previousValue);

				if (previousValue is ResourceDictionary previousDictionary)
				{
					previousDictionary._parent = null;
				}

				if (value is ResourceDictionary newDictionary)
				{
					newDictionary._parent = this;
					ResourceDictionaryValueChange?.Invoke(this, EventArgs.Empty);
				}
			}

			InvalidateNotFoundCache(true, resourceKey);
		}

		/// <summary>
		/// If retrieved element is a <see cref="LazyInitializer"/> stub, materialize the actual object and replace the stub.
		/// </summary>
		private void TryMaterializeLazy(in ResourceKey key, ref object value)
		{
			if (value is LazyInitializer lazyInitializer)
			{
				object newValue = null;
				bool hasEmptyCurrentScope = lazyInitializer.CurrentScope.Sources.IsEmpty;
				try
				{
					_values.Remove(key); // Temporarily remove the key to make this method safely reentrant, if it's a framework- or application-level theme dictionary

					if (!hasEmptyCurrentScope)
					{
						ResourceResolver.PushNewScope(lazyInitializer.CurrentScope);
					}

					// Lazy initialized resources must also resolve using the current dictionary
					// In previous versions of Uno (4 and earlier), this used to not be needed because all ResourceDictionary
					// files where implicitly available at the app level.
					if (!FeatureConfiguration.ResourceDictionary.IncludeUnreferencedDictionaries)
					{
						ResourceResolver.PushSourceToScope(this);
					}

					newValue = lazyInitializer.Initializer();
				}
				finally
				{
					value = newValue;
					_values[key] = newValue; // If Initializer threw an exception this will push null, to avoid running buggy initialization again and again (and avoid surfacing initializer to consumer code)
					if (newValue is ResourceDictionary)
					{
						ResourceDictionaryValueChange?.Invoke(this, EventArgs.Empty);
					}

					if (!FeatureConfiguration.ResourceDictionary.IncludeUnreferencedDictionaries)
					{
						ResourceResolver.PopSourceFromScope();
					}

					if (!hasEmptyCurrentScope)
					{
						ResourceResolver.PopScope();
					}
				}
			}
		}

		/// <summary>
		/// If <paramref name="value"/> is a <see cref="StaticResourceAliasRedirect"/>, replace it with the target of ResourceKey, or null if no matching resource is found.
		/// </summary>
		/// <returns>True if <paramref name="value"/> is a <see cref="StaticResourceAliasRedirect"/>, false otherwise</returns>
		private bool TryResolveAlias(ref object value)
		{
			if (value is StaticResourceAliasRedirect alias)
			{
				ResourceResolver.ResolveResourceStatic(alias.ResourceKey, out var resourceKeyTarget, alias.ParseContext);
				value = resourceKeyTarget;
				return true;
			}

			return false;
		}

		private bool GetFromMerged(in ResourceKey resourceKey, out object value)
		{
			// Check last dictionary first - //https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/resourcedictionary-and-xaml-resource-references#merged-resource-dictionaries
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].TryGetValue(resourceKey, out value, shouldCheckSystem: false))
				{
					return true;
				}
			}

			value = null;

			return false;
		}

		private bool ContainsKeyMerged(in ResourceKey resourceKey)
		{
			for (int i = _mergedDictionaries.Count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].ContainsKey(resourceKey, shouldCheckSystem: false))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Refreshes the provided dictionary with the latest version of the dictionary (during hot reload)
		/// </summary>
		/// <param name="merged">A dictionary present in the merged dictionaries</param>
		internal void RefreshMergedDictionary(ResourceDictionary merged)
		{
			if (merged.Source is null)
			{
				throw new InvalidOperationException("Unable to refresh dictionary without a Source being set");
			}

			var index = _mergedDictionaries.IndexOf(merged);
			if (index != -1)
			{
				_mergedDictionaries[index] = ResourceResolver.RetrieveDictionaryForSource(merged.Source);
			}
			else
			{
				throw new InvalidOperationException("The provided dictionary cannot be found in the merged list");
			}
		}

		private ResourceDictionary _activeThemeDictionary;
		private ResourceKey _activeTheme;

		private ResourceDictionary GetActiveThemeDictionary(in ResourceKey activeTheme)
		{
			if (!activeTheme.Equals(_activeTheme))
			{
				InvalidateNotFoundCache(false);
				_activeTheme = activeTheme;
				_activeThemeDictionary = GetThemeDictionary(activeTheme) ?? GetThemeDictionary(Themes.Default);
			}

			return _activeThemeDictionary;
		}

		private ResourceDictionary GetThemeDictionary(in ResourceKey theme)
		{
			object dict = null;
			if (_themeDictionaries?.TryGetValue(theme, out dict, shouldCheckSystem: false) ?? false)
			{
				return dict as ResourceDictionary;
			}

			return null;
		}

		private bool ContainsKeyTheme(in ResourceKey resourceKey, in ResourceKey activeTheme)
		{
			return GetActiveThemeDictionary(activeTheme)?.ContainsKey(resourceKey, shouldCheckSystem: false) ?? ContainsKeyThemeMerged(resourceKey, activeTheme);
		}

		private bool ContainsKeyThemeMerged(in ResourceKey resourceKey, in ResourceKey activeTheme)
		{
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].ContainsKeyTheme(resourceKey, activeTheme))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Copy another dictionary's contents, this is used when setting the <see cref="Source"/> property
		/// </summary>
		internal void CopyFrom(ResourceDictionary source)
		{
			_values.Clear();
			_mergedDictionaries.Clear();
			_themeDictionaries?.Clear();

			// In a foreign library that uses merged-dict with 'Source=...', there can be multiple instances of res-dict from one single xaml file.
			// And, the instance referenced in App.xaml may not be the same one used in merged-dict of library-a\textbox.xaml.
			// In order to ensure the theme updates covers all of them, we need to keep track of this source instance for theme updates before it is lost.
			_sourceDictionary = WeakReferencePool.RentWeakReference(this, source);

			_values.EnsureCapacity(source._values.Count);

			foreach (var pair in source._values)
			{
				var (key, value) = pair;

				// Lazy resource initialization needs the current XamlScope
				// to resolve values, and the originally defined scope that 
				// was set when the source dictionary was created may not be 
				// value for the current XAML scope. We rewrite the initializer
				// in order for the name resolution to work properly.
				if (value is LazyInitializer lazy)
				{
					value = new LazyInitializer(ResourceResolver.CurrentScope, lazy.Initializer);
				}

				_values.Add(key, value);
			}

			_mergedDictionaries.AddRange(source._mergedDictionaries);
			if (source._themeDictionaries != null)
			{
				GetOrCreateThemeDictionaries().CopyFrom(source._themeDictionaries);
			}
		}

		public global::System.Collections.Generic.ICollection<object> Keys
			=> _values.Keys.Select(k => ConvertKey(k)).ToList();

		private static object ConvertKey(ResourceKey resourceKey)
			=> resourceKey.TypeKey ?? (object)resourceKey.Key;

		// TODO: this doesn't handle lazy initializers or aliases
		public global::System.Collections.Generic.ICollection<object> Values => _values.Values;

		internal SpecializedResourceDictionary.ValueCollection ValuesInternal => _values.Values;

		public void Add(global::System.Collections.Generic.KeyValuePair<object, object> item) => Add(item.Key, item.Value);

		public bool Contains(global::System.Collections.Generic.KeyValuePair<object, object> item) => _values.ContainsKey(new ResourceKey(item.Key));

		[Uno.NotImplemented]
		public void CopyTo(global::System.Collections.Generic.KeyValuePair<object, object>[] array, int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		private bool _isParsing;
		/// <summary>
		/// True if the element is in the process of being parsed from Xaml.
		/// </summary>
		/// <remarks>This property shouldn't be set from user code. It's public to allow being set from generated code.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsParsing
		{
			get => _isParsing;
			set
			{
				if (!value)
				{
					throw new InvalidOperationException($"{nameof(IsParsing)} should never be set from user code.");
				}

				_isParsing = value;
				if (_isParsing)
				{
					ResourceResolver.PushSourceToScope(this);
				}
			}
		}

		public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
		{
			TryMaterializeAll();

			foreach (var kvp in _values)
			{
				var aliased = kvp.Value;
				if (TryResolveAlias(ref aliased))
				{
					yield return new KeyValuePair<object, object>(ConvertKey(kvp.Key), aliased);
				}
				else
				{
					yield return new KeyValuePair<object, object>(ConvertKey(kvp.Key), kvp.Value);
				}
			}
		}

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Ensure all lazily-set values are materialized, prior to enumeration.
		/// </summary>
		private void TryMaterializeAll()
		{
			if (!_hasUnmaterializedItems)
			{
				return;
			}

			var unmaterialized = new List<KeyValuePair<ResourceKey, object>>();

			foreach (var kvp in _values)
			{
				if (kvp.Value is LazyInitializer lazyInitializer)
				{
					unmaterialized.Add(kvp);
				}
			}

			foreach (var kvp in unmaterialized)
			{
				var value = kvp.Value;
				TryMaterializeLazy(kvp.Key, ref value);
			}

			_hasUnmaterializedItems = false;
		}

		public void CreationComplete()
		{
			if (!IsParsing)
			{
				throw new InvalidOperationException($"Called without matching {nameof(IsParsing)} call. This method should never be called from user code.");
			}

			_isParsing = false;
			ResourceResolver.PopSourceFromScope();
		}

		/// <summary>
		/// Update theme bindings on DependencyObjects in the dictionary.
		/// </summary>
		internal void UpdateThemeBindings(ResourceUpdateReason updateReason)
		{
			foreach (var item in _values.Values)
			{
				if (item is IDependencyObjectStoreProvider provider)
				{
					provider.Store.UpdateResourceBindings(updateReason, containingDictionary: this);
				}
			}

			foreach (var mergedDict in _mergedDictionaries)
			{
				mergedDict.UpdateThemeBindings(updateReason);
			}

			if (_sourceDictionary?.Target is ResourceDictionary target)
			{
				target.UpdateThemeBindings(updateReason);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public delegate object ResourceInitializer();

		private class SpecialValue { }

		/// <summary>
		/// Allows resources to be initialized on-demand using correct scope.
		/// </summary>
		private class LazyInitializer : SpecialValue
		{
			public XamlScope CurrentScope { get; }
			public ResourceInitializer Initializer { get; }

			public LazyInitializer(XamlScope currentScope, ResourceInitializer initializer)
			{
				CurrentScope = currentScope;
				Initializer = initializer;
			}
		}

		/// <summary>
		/// Allows resources set by a StaticResource alias to be resolved with the correct theme at time of resolution (eg in response to the
		/// app theme changing).
		/// </summary>
		private class StaticResourceAliasRedirect : SpecialValue
		{
			public StaticResourceAliasRedirect(string resourceKey, XamlParseContext parseContext)
			{
				ResourceKey = resourceKey;
				ParseContext = parseContext;
			}

			public string ResourceKey { get; }
			public XamlParseContext ParseContext { get; }
		}

		internal static object GetStaticResourceAliasPassthrough(string resourceKey, XamlParseContext parseContext) => new StaticResourceAliasRedirect(resourceKey, parseContext);

		internal static ResourceKey GetActiveTheme() => Themes.Active;

		internal static void SetActiveTheme(SpecializedResourceDictionary.ResourceKey key)
			=> Themes.Active = key;

		internal void InvalidateNotFoundCache(bool propagate)
		{
			if (propagate)
			{
				// Traverse dictionary sub-tree iteratively as it has less overhead.
				var current = this;

				while (current is not null)
				{
					current._keyNotFoundCache?.Clear();

					current = current._parent;
				}
			}
			else
			{
				_keyNotFoundCache?.Clear();
			}
		}

		internal void InvalidateNotFoundCache(bool propagate, in ResourceKey key)
		{
			if (propagate)
			{
				// Traverse dictionary sub-tree iteratively as it has less overhead.
				var current = this;

				while (current is not null)
				{
					current._keyNotFoundCache?.Remove(key);
					current = current._parent;
				}
			}
			else
			{
				_keyNotFoundCache?.Remove(key);
			}
		}


		private static class Themes
		{
			public static SpecializedResourceDictionary.ResourceKey Light { get; } = "Light";
			public static SpecializedResourceDictionary.ResourceKey Default { get; } = "Default";
			public static SpecializedResourceDictionary.ResourceKey Active { get; set; } = Default;
		}
	}
}
