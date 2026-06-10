//#define DEBUG_SET_RESOURCE_SOURCE
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml.Data;
using Uno.Extensions;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.UI.Input.Spatial;
using ResourceKey = Microsoft.UI.Xaml.SpecializedResourceDictionary.ResourceKey;

namespace Microsoft.UI.Xaml
{
	public partial class ResourceDictionary : DependencyObject, IDependencyObjectParse, IDictionary<object, object>
	{
		private readonly SpecializedResourceDictionary _values;
		private readonly ObservableCollection<ResourceDictionary> _mergedDictionaries = new();
		private ResourceDictionary _themeDictionaries;
		private ResourceDictionary _parent;
		private ManagedWeakReference _sourceDictionary;
		private ManagedWeakReference _owner;
		private HashSet<ResourceKey> _keyNotFoundCache;

		/// <summary>
		/// This event is fired when a key that has value of type <see cref="ResourceDictionary"/> is added or changed in the current <see cref="ResourceDictionary" />
		/// </summary>
		private event EventHandler ResourceDictionaryValueChange;

		/// <summary>
		/// If true, there may be lazily-set values in the dictionary that need to be initialized.
		/// </summary>
		private bool _hasUnmaterializedItems;

		public ResourceDictionary() : this(0)
		{
		}

		/// <summary>
		/// Creates a new <see cref="ResourceDictionary"/> with the specified initial capacity, to reduce internal resize operations.
		/// </summary>
		/// <param name="initialCapacity">The initial number of elements the dictionary can contain before resizing.</param>
		internal ResourceDictionary(int initialCapacity)
		{
			_values = new SpecializedResourceDictionary(initialCapacity);
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

		/// <summary>
		/// Creates a new <see cref="ResourceDictionary"/> with the specified initial capacity, to reduce internal resize operations.
		/// This method is intended for use by XAML-generated code and is not meant to be called directly from user code.
		/// </summary>
		/// <param name="initialCapacity">The initial number of elements the dictionary can contain before resizing.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCapacity"/> is negative.</exception>
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static ResourceDictionary CreateWithCapacity(int initialCapacity)
		{
			if (initialCapacity < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(initialCapacity));
			}

			return new ResourceDictionary(initialCapacity);
		}

		private Uri _source;
		public Uri Source
		{
			get => _source;
			set
			{
				if (!IsParsing) // If we're parsing, the Source is being set as a 'FYI', don't try to resolve it
				{
					var sourceDictionary = RetrieveDictionaryForSourceWithAlcAwareness(value);
					CopyFrom(sourceDictionary);
				}

				_source = value;
			}
		}

		/// <summary>
		/// Retrieves a dictionary for the given source, with ALC awareness when secondary ALCs have registered resources.
		/// </summary>
		private ResourceDictionary RetrieveDictionaryForSourceWithAlcAwareness(Uri source)
		{
			// Only do the expensive ALC lookup if we know secondary ALCs have registered resources
			if (Application.HasSecondaryApps)
			{
				// Use the ambient resolution context if available (set during App.xaml initialization),
				// because GetType().Assembly always returns Uno.UI which is in the default ALC.
				var callingAlc = ResourceResolver.CurrentResolutionAlc
					?? global::System.Runtime.Loader.AssemblyLoadContext.Default;
				return ResourceResolver.RetrieveDictionaryForSource(
					source?.OriginalString,
					currentAbsolutePath: null,
					callingAlc);
			}

			return ResourceResolver.RetrieveDictionaryForSource(source);
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

			// MUX: CResourceDictionary::RemoveKey invalidates the theme walk cache for this key.
			ThemeWalkResourceCache.Instance.RemoveCacheEntry(keyToRemove);
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
			|| ContainsKeyTheme(resourceKey, GetActiveTheme())
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

		// Theme-aware convenience overloads that forward the resolving owner's effective theme key to the
		// leaf lookup, mirroring the parameterless object/string overloads above.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(object key, in ResourceKey themeKey, out object value, bool shouldCheckSystem)
			=> TryGetValue(new ResourceKey(key), themeKey, out value, shouldCheckSystem);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(string resourceKey, in ResourceKey themeKey, out object value, bool shouldCheckSystem)
			=> TryGetValue(new ResourceKey(resourceKey), themeKey, out value, shouldCheckSystem);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(in ResourceKey resourceKey, out object value, bool shouldCheckSystem)
			=> TryGetValue(resourceKey, GetActiveTheme(), out value, shouldCheckSystem);

		// Theme-aware leaf lookup. The Light/Dark sub-dictionary is selected by the explicitly-passed
		// <paramref name="themeKey"/> (the resolving owner's effective theme). The parameterless overload
		// above forwards GetActiveTheme() (the app/OS base theme) — the permanent fallback for owner-less /
		// app-level lookups, e.g. the public Resources.TryGetValue(key, out value), which has no resolving
		// element. MUX: matches SetThemeResourceBinding resolving against the owner's own m_theme
		// (Theming.cpp:368-376), with the theme threaded as a parameter rather than an ambient slot.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(in ResourceKey resourceKey, in ResourceKey themeKey, out object value, bool shouldCheckSystem)
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
					TryMaterializeLazy(resourceKey, ref value, themeKey);
					TryResolveAlias(ref value, themeKey);
				}

#if DEBUG && DEBUG_SET_RESOURCE_SOURCE
				TryApplySource(value, resourceKey);
#endif
				return true;
			}

			if (GetFromMerged(modifiedKey, themeKey, out value))
			{
				return true;
			}

			if (GetActiveThemeDictionary(themeKey) is { } activeThemeDictionary
				&& activeThemeDictionary.TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false))
			{
				return true;
			}

			if (shouldCheckSystem && !IsSystemDictionary) // We don't fall back on system resources from within a system-defined dictionary, to avoid an infinite recurse
			{
				return ResourceResolver.TrySystemResourceRetrieval(modifiedKey, themeKey, out value);
			}

			if (useKeysNotFoundCache && !shouldCheckSystem)
			{
				KeyNotFoundCache.Add(resourceKey);
			}

			return false;
		}

		/// <summary>
		/// Tries to get a value and also returns the providing ResourceDictionary.
		/// </summary>
		/// <remarks>
		/// MUX Reference: CResourceDictionary::GetKeyForResourceResolutionNoRef + ResolveThemeResource
		/// The providing dictionary is the one that should be pinned for theme resource re-resolution.
		/// Critical: when a value is found in a theme sub-dictionary (via GetActiveThemeDictionary),
		/// the providing dictionary is THIS dictionary (the one owning ThemeDictionaries), not the
		/// inner theme sub-dictionary. This ensures re-querying on theme change automatically picks
		/// the new theme's sub-dictionary.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(in ResourceKey resourceKey, out object value, out ResourceDictionary providingDictionary, bool shouldCheckSystem)
			=> TryGetValue(resourceKey, GetActiveTheme(), out value, out providingDictionary, shouldCheckSystem);

		// Theme-aware leaf lookup (providing-dictionary variant). See the value-only overload above. The
		// providing dictionary pinned for a theme hit is THIS dictionary (the owner of ThemeDictionaries),
		// so re-querying after a theme change picks the new sub-dictionary.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(in ResourceKey resourceKey, in ResourceKey themeKey, out object value, out ResourceDictionary providingDictionary, bool shouldCheckSystem)
		{
			bool useKeysNotFoundCache = resourceKey.ShouldFilter;
			var modifiedKey = resourceKey;

			if (useKeysNotFoundCache)
			{
				if (!shouldCheckSystem && KeyNotFoundCache.Contains(resourceKey))
				{
					value = null;
					providingDictionary = null;
					return false;
				}

				modifiedKey = modifiedKey with { ShouldFilter = false };
			}

			if (_values.TryGetValue(modifiedKey, out value))
			{
				if (value is SpecialValue)
				{
					TryMaterializeLazy(resourceKey, ref value, themeKey);
					TryResolveAlias(ref value, themeKey);
				}

#if DEBUG && DEBUG_SET_RESOURCE_SOURCE
				TryApplySource(value, resourceKey);
#endif
				providingDictionary = this;
				return true;
			}

			if (GetFromMerged(modifiedKey, themeKey, out value, out providingDictionary))
			{
				return true;
			}

			// When found via theme dictionary, pin THIS dictionary (the owner of ThemeDictionaries),
			// not the inner theme sub-dictionary. This is critical for correct theme switching.
			if (GetActiveThemeDictionary(themeKey) is { } activeThemeDictionary
				&& activeThemeDictionary.TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false))
			{
				providingDictionary = this;
				return true;
			}

			if (shouldCheckSystem && !IsSystemDictionary)
			{
				if (ResourceResolver.TrySystemResourceRetrieval(modifiedKey, themeKey, out value))
				{
					// System resources are always available at top-level;
					// no need to pin a specific dictionary -- RefreshValue()
					// will fall back to TryTopLevelRetrieval.
					providingDictionary = null;
					return true;
				}
			}

			if (useKeysNotFoundCache && !shouldCheckSystem)
			{
				KeyNotFoundCache.Add(resourceKey);
			}

			providingDictionary = null;
			return false;
		}

		// Thread the resolving owner's theme into the merged-dictionary recursion so merged dictionaries
		// select the same theme sub-dictionary as the root lookup.
		private bool GetFromMerged(in ResourceKey resourceKey, in ResourceKey themeKey, out object value, out ResourceDictionary providingDictionary)
		{
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].TryGetValue(resourceKey, themeKey, out value, out providingDictionary, shouldCheckSystem: false))
				{
					return true;
				}
			}

			value = null;
			providingDictionary = null;
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

			// MUX: CResourceDictionary::AddKey invalidates the theme walk cache for this key
			// because a new resource can shadow entries from other dictionaries in the lookup chain.
			ThemeWalkResourceCache.Instance.RemoveCacheEntry(resourceKey);

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
			=> TryMaterializeLazy(key, ref value, GetActiveTheme());

		private void TryMaterializeLazy(in ResourceKey key, ref object value, in ResourceKey themeKey)
		{
			if (value is LazyInitializer lazyInitializer)
			{
				object newValue = null;
				bool hasEmptyCurrentScope = lazyInitializer.CurrentScope.Sources.IsEmpty;

				// A lazy resource living in a theme sub-dictionary (e.g. the Light ApplicationPageBackgroundThemeBrush
				// whose Color is a {StaticResource SolidBackgroundFillColorBase}) bakes its nested {StaticResource}/
				// {ThemeResource} against the ambient active theme (GetActiveTheme) the FIRST time it materializes —
				// then caches the result in _values forever. Under an opposite-theme app (Light resource materialized
				// while the app is Dark) that bakes the wrong theme's value permanently. Scope the active theme to the
				// theme this lookup is resolving for, so the initializer resolves nested refs in the sub-dictionary's
				// own theme. Mirrors WinUI resolving a deferred theme-dictionary resource under the requested theme
				// (EnsureActiveThemeDictionary, Resources.cpp:687-819).
				var previousActiveTheme = Themes.Active;
				var overrideActiveTheme = themeKey.Key is not null && !themeKey.Equals(previousActiveTheme);

				try
				{
					_values.Remove(key); // Temporarily remove the key to make this method safely reentrant, if it's a framework- or application-level theme dictionary

					if (overrideActiveTheme)
					{
						Themes.Active = themeKey;
					}

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

					if (overrideActiveTheme)
					{
						Themes.Active = previousActiveTheme;
					}
				}

				// A lazily-materialized resource (e.g. a SolidColorBrush whose Color is a {ThemeResource})
				// resolved its theme references above against the process-global active theme. Re-resolve them
				// against the owning element's effective theme so the resource matches the theme of the element
				// hosting this dictionary, matching WinUI's per-owner {ThemeResource} resolution. Only when the
				// owner already has an established (non-None) theme — otherwise the global fallback stands.
				if (value is IDependencyObjectStoreProvider materializedProvider
					&& GetResourceOwner() is { } resourceOwner
					&& ((IDependencyObjectStoreProvider)resourceOwner).Store.GetTheme() != Theme.None)
				{
					materializedProvider.Store.UpdateResourceBindings(
						ResourceUpdateReason.ThemeResource,
						resourceContextProvider: resourceOwner,
						containingDictionary: this);
				}
			}
		}

		/// <summary>
		/// If <paramref name="value"/> is a <see cref="StaticResourceAliasRedirect"/>, replace it with the target of ResourceKey, or null if no matching resource is found.
		/// </summary>
		/// <returns>True if <paramref name="value"/> is a <see cref="StaticResourceAliasRedirect"/>, false otherwise</returns>
		private bool TryResolveAlias(ref object value)
			=> TryResolveAlias(ref value, GetActiveTheme());

		// Resolve the StaticResource alias target against the passed owner theme, so an alias inside a theme
		// sub-dictionary (e.g. SystemControlFocusVisualPrimaryBrush → FocusStrokeColorOuterBrush) resolves
		// its target in the same theme as the alias. Matches WinUI's LookupThemeResource(theme, key) (xcpcore.cpp).
		private bool TryResolveAlias(ref object value, in ResourceKey themeKey)
		{
			if (value is StaticResourceAliasRedirect alias)
			{
				ResourceResolver.ResolveResourceStatic(alias.ResourceKey, themeKey, out var resourceKeyTarget, alias.ParseContext);
				value = resourceKeyTarget;
				return true;
			}

			return false;
		}

		// Theme-threaded merged-dictionary recursion (value-only variant).
		private bool GetFromMerged(in ResourceKey resourceKey, in ResourceKey themeKey, out object value)
		{
			// Check last dictionary first - //https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/resourcedictionary-and-xaml-resource-references#merged-resource-dictionaries
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false))
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
				_mergedDictionaries[index] = RetrieveDictionaryForSourceWithAlcAwareness(merged.Source);
			}
			else
			{
				throw new InvalidOperationException("The provided dictionary cannot be found in the merged list");
			}
		}

		private ResourceDictionary _activeThemeDictionary;
		private ResourceKey _activeTheme;
		private bool _activeThemeHighContrast;

		private ResourceDictionary GetActiveThemeDictionary(in ResourceKey activeTheme)
		{
			// High contrast is an OS/app-global dimension: WinUI reads it from FrameworkTheming, not from
			// the per-object theme (Resources.cpp:718,740), while the base Light/Dark comes from the
			// resolving owner's theme (the passed key — the analog of WinUI's RequestedThemeForSubTree).
			// Cache invalidates when either the base theme key or the high-contrast state changes (MUX:
			// EnsureActiveThemeDictionary's baseThemeChanged | highContrastChanged guard, Resources.cpp:699-704).
			var highContrast = Themes.IsHighContrast;
			if (!activeTheme.Equals(_activeTheme) || highContrast != _activeThemeHighContrast)
			{
				InvalidateNotFoundCache(false);
				_activeTheme = activeTheme;
				_activeThemeHighContrast = highContrast;
				_activeThemeDictionary = ResolveActiveThemeDictionary(activeTheme, highContrast);
			}

			return _activeThemeDictionary;
		}

		// MUX: CResourceDictionary::EnsureActiveThemeDictionary (Resources.cpp:687-819). When high contrast
		// is active, the high-contrast sub-dictionary wins: map the base theme to its high-contrast variant
		// (Light → HighContrastWhite, Dark → HighContrastBlack), else the generic "HighContrast" key; if
		// none is defined, fall through to the base Light/Dark theme, then "Default". Full high-contrast
		// brush parity (OS-wide variants and runtime change propagation) is a follow-up.
		private ResourceDictionary ResolveActiveThemeDictionary(in ResourceKey baseTheme, bool highContrast)
		{
			if (highContrast)
			{
				var highContrastDictionary =
					GetThemeDictionary(GetHighContrastKeyForBaseTheme(baseTheme))
					?? GetThemeDictionary(Themes.HighContrast);

				if (highContrastDictionary is not null)
				{
					return highContrastDictionary;
				}
			}

			return GetThemeDictionary(baseTheme) ?? GetThemeDictionary(Themes.Default);
		}

		private static ResourceKey GetHighContrastKeyForBaseTheme(in ResourceKey baseTheme)
			=> baseTheme.Equals(Themes.Dark) ? Themes.HighContrastBlack : Themes.HighContrastWhite;

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

		/// <summary>
		/// Enumerates key-value pairs without materializing lazy entries.
		/// Lazy or alias entries are resolved transiently (the resolved value is returned
		/// but NOT stored back to the dictionary, preserving theme-aware re-resolution).
		/// </summary>
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public IEnumerable<KeyValuePair<object, object>> GetKeyValuePairsNonMaterialized()
		{
			// Snapshot entries to avoid InvalidOperationException if an initializer
			// causes _values to mutate during enumeration.
			var snapshot = new List<KeyValuePair<SpecializedResourceDictionary.ResourceKey, object>>(_values.Count);
			foreach (var kvp in _values)
			{
				snapshot.Add(kvp);
			}

			foreach (var kvp in snapshot)
			{
				var value = kvp.Value;
				if (value is LazyInitializer lazyInitializer)
				{
					// Resolve lazily but do NOT store back — preserves re-resolution capability
					bool pushedScope = false;
					bool pushedSource = false;

					try
					{
						bool hasEmptyCurrentScope = lazyInitializer.CurrentScope.Sources.IsEmpty;
						if (!hasEmptyCurrentScope)
						{
							ResourceResolver.PushNewScope(lazyInitializer.CurrentScope);
							pushedScope = true;
						}

						if (!FeatureConfiguration.ResourceDictionary.IncludeUnreferencedDictionaries)
						{
							ResourceResolver.PushSourceToScope(this);
							pushedSource = true;
						}

						value = lazyInitializer.Initializer();
					}
					catch
					{
						value = null;
					}
					finally
					{
						if (pushedSource)
						{
							ResourceResolver.PopSourceFromScope();
						}

						if (pushedScope)
						{
							ResourceResolver.PopScope();
						}
					}
				}

				if (value is StaticResourceAliasRedirect alias)
				{
					ResourceResolver.ResolveResourceStatic(alias.ResourceKey, out var target, alias.ParseContext);
					value = target;
				}

				yield return new KeyValuePair<object, object>(ConvertKey(kvp.Key), value);
			}
		}

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
		// The FrameworkElement that hosts this dictionary as its Resources, if any. WinUI resolves a
		// {ThemeResource} declared on a resource (e.g. a SolidColorBrush in FrameworkElement.Resources whose
		// Color is a {ThemeResource}) against the OWNING element's effective theme. Uno's resolution keys on
		// the resolving owner's theme (ThemeResolution.ResolveOwnerTheme); a standalone resource DO has no
		// inheritance parent, so without this back-reference it would fall back to the process-global active
		// theme. Capturing the owner lets materialization and theme-change re-resolution use the element theme.
		private bool _ownerIsAmbiguous;

		internal void SetResourceOwner(DependencyObject owner)
		{
			if (owner is null)
			{
				ReturnOwnerWeakReference();
				return;
			}

			if (_ownerIsAmbiguous)
			{
				return;
			}

			if (_owner?.Target is { } existing)
			{
				if (ReferenceEquals(existing, owner))
				{
					// Same owner — keep the rented reference rather than renting another. FrameworkElement.Resources
					// sets the owner on both getter initialization and setter, so this is hit repeatedly.
					return;
				}

				// A dictionary instance shared as the Resources of more than one element has no single owning
				// theme. Rather than attribute one element's theme to the other's resources (last-writer-wins),
				// mark the owner ambiguous so GetResourceOwner returns null and resolution falls back to the
				// app/OS theme — the safe, deterministic choice for a shared dictionary. WinUI carries the theme
				// on each consuming DO instead; for a single literally-shared resource instance (one brush, one
				// Color) only one theme can win regardless.
				ReturnOwnerWeakReference();
				_ownerIsAmbiguous = true;
				return;
			}

			// A prior owner may have been collected, leaving a stale reference — return it before renting a new one.
			ReturnOwnerWeakReference();
			_owner = WeakReferencePool.RentWeakReference(this, owner);
		}

		private void ReturnOwnerWeakReference()
		{
			if (_owner is not null)
			{
				WeakReferencePool.ReturnWeakReference(this, _owner);
				_owner = null;
			}
		}

		/// <summary>
		/// Finds the nearest <see cref="FrameworkElement"/> that owns this dictionary (directly, or via the
		/// merged/theme-dictionary parent chain), used as the resource-context theme provider.
		/// </summary>
		private FrameworkElement GetResourceOwner()
		{
			for (var current = this; current is not null; current = current._parent)
			{
				if (current._owner?.Target is FrameworkElement owner)
				{
					return owner;
				}
			}

			return null;
		}

		internal void UpdateThemeBindings(ResourceUpdateReason updateReason)
		{
			// Resolve resources' {ThemeResource} values against the owning element's effective theme rather
			// than the process-global active theme (see SetResourceOwner). For app/standalone dictionaries
			// with no owner this is null and resolution falls back to the app/OS theme, as before.
			var owner = GetResourceOwner();

			foreach (var item in _values.Values)
			{
				if (item is IDependencyObjectStoreProvider provider)
				{
					provider.Store.UpdateResourceBindings(updateReason, resourceContextProvider: owner, containingDictionary: this);
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

		// The per-subtree theme is threaded as a parameter (the resolving owner's effective theme,
		// ThemeResolution.ResolveOwnerTheme); GetActiveTheme returns the application/OS base theme, used only
		// as the fallback for resource lookups with no owner context (e.g. app-level Application.Resources).
		internal static ResourceKey GetActiveTheme() => Themes.Active;

		// Maps a per-object Theme (CDependencyObject::m_theme) to the BASE Light/Dark sub-dictionary key —
		// the analog of WinUI's RequestedThemeForSubTree. High contrast is composed separately at the
		// resolution leaf (GetActiveThemeDictionary reads the OS/app-global high-contrast state, as WinUI's
		// EnsureActiveThemeDictionary reads FrameworkTheming, Resources.cpp:718,740), so the key stays base.
		internal static ResourceKey GetThemeKey(Theme theme)
			=> Theming.GetBaseValue(theme) == Theme.Light ? Themes.Light : Themes.Dark;

		// The application/OS base theme (Themes.Active) expressed as a base Theme. This is the single
		// owner-less fallback for {ThemeResource} resolution: it is what the lazy-materialization leaf keys on
		// (GetActiveTheme), and the analog of WinUI's FrameworkTheming::GetBaseTheme used by
		// EnsureActiveThemeDictionary when no subtree theme is requested (Resources.cpp:765). Kept coherent with
		// Application.RequestedTheme (Application.cs:229-244). "Default" (pre-app-theme-init) resolves to the
		// app's ActualElementTheme so the result is always a concrete Light/Dark.
		internal static Theme GetActiveBaseTheme()
		{
			var active = Themes.Active;
			if (active.Equals(Themes.Light))
			{
				return Theme.Light;
			}
			if (active.Equals(Themes.Dark))
			{
				return Theme.Dark;
			}

			return Theming.FromElementTheme(Application.Current?.ActualElementTheme ?? ElementTheme.Light);
		}

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
			public static SpecializedResourceDictionary.ResourceKey Dark { get; } = "Dark";
			public static SpecializedResourceDictionary.ResourceKey Default { get; } = "Default";

			// High-contrast theme sub-dictionary keys (MUX EnsureActiveThemeDictionary, Resources.cpp:
			// 725-758). HighContrastWhite/Black are the Light/Dark high-contrast variants; HighContrast is
			// the generic fallback key.
			public static SpecializedResourceDictionary.ResourceKey HighContrast { get; } = "HighContrast";
			public static SpecializedResourceDictionary.ResourceKey HighContrastWhite { get; } = "HighContrastWhite";
			public static SpecializedResourceDictionary.ResourceKey HighContrastBlack { get; } = "HighContrastBlack";

			// The application/OS base theme. The per-subtree theme (the analog of WinUI's
			// CCoreServices::m_requestedThemeForSubTree) is threaded as the resolving owner's effective theme
			// (ThemeResolution.ResolveOwnerTheme); Active is the fallback for lookups with no owner context,
			// and is strictly "Light"/"Dark".
			public static SpecializedResourceDictionary.ResourceKey Active { get; set; } = Default;

			// The OS/app-global high-contrast state (MUX: FrameworkTheming::HasHighContrastTheme). Read live
			// from the accessibility settings — high contrast is orthogonal to the Light/Dark base theme and
			// is composed at the resolution leaf (GetActiveThemeDictionary), matching WinUI reading it from
			// FrameworkTheming rather than from the per-object/subtree theme.
			public static bool IsHighContrast => SystemThemeHelper.IsHighContrast;
		}
	}
}
