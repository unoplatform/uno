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
					// MUX Reference: CResourceDictionary::OnThemeDictionariesChanged (Resources.cpp:1392-1405), commit fc2f82117.
					// Remove active theme dictionary, because the theme dictionaries collection
					// has changed, and a better candidate for the active theme dictionary
					// may be available.
					_activeThemeDictionary = null;
					_activeTheme = ResourceKey.Empty;
					_activeThemeValue = Theme.None;
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
			Uno.UI.Xaml.Core.CoreServices.Instance.ThemeWalkResourceCache.RemoveCacheEntry(keyToRemove);
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
			|| ContainsKeyTheme(resourceKey)
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

		// Theme-aware leaf lookup. The Light/Dark sub-dictionary is selected by the ambient active theme
		// (GetActiveTheme — the core requested-theme-for-subtree slot when one is scoped, else the app/OS
		// base theme). MUX: CResourceDictionary::EnsureActiveThemeDictionary reads the same core ambient
		// (Resources.cpp:764-768); callers that resolve under a specific owner's theme scope the slot
		// (CoreServices.ScopeRequestedThemeForSubTree) rather than passing a theme here.
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

			if (GetActiveThemeDictionary() is { } activeThemeDictionary
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

		/// <summary>
		/// Tries to get a value and also returns the providing ResourceDictionary.
		/// </summary>
		/// <remarks>
		/// MUX Reference: CResourceDictionary::GetKeyForResourceResolutionNoRef + ResolveThemeResource
		/// The providing dictionary is the one that should be pinned for theme resource re-resolution.
		/// Critical: when a value is found in a theme sub-dictionary (via GetActiveThemeDictionary),
		/// the providing dictionary is THIS dictionary (the one owning ThemeDictionaries), not the
		/// inner theme sub-dictionary. This ensures re-querying on theme change automatically picks
		/// the new theme's sub-dictionary. The theme sub-dictionary is selected by the ambient active
		/// theme (GetActiveTheme), like CResourceDictionary::EnsureActiveThemeDictionary reading the
		/// core requested-theme-for-subtree slot (Resources.cpp:764-768).
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetValue(in ResourceKey resourceKey, out object value, out ResourceDictionary providingDictionary, bool shouldCheckSystem)
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
					TryMaterializeLazy(resourceKey, ref value);
					TryResolveAlias(ref value);
				}

#if DEBUG && DEBUG_SET_RESOURCE_SOURCE
				TryApplySource(value, resourceKey);
#endif
				providingDictionary = this;
				return true;
			}

			if (GetFromMerged(modifiedKey, out value, out providingDictionary))
			{
				return true;
			}

			// When found via theme dictionary, pin THIS dictionary (the owner of ThemeDictionaries),
			// not the inner theme sub-dictionary. This is critical for correct theme switching.
			if (GetActiveThemeDictionary() is { } activeThemeDictionary
				&& activeThemeDictionary.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
			{
				providingDictionary = this;
				return true;
			}

			if (shouldCheckSystem && !IsSystemDictionary)
			{
				if (ResourceResolver.TrySystemResourceRetrieval(modifiedKey, out value))
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

		private bool GetFromMerged(in ResourceKey resourceKey, out object value, out ResourceDictionary providingDictionary)
		{
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false))
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
			Uno.UI.Xaml.Core.CoreServices.Instance.ThemeWalkResourceCache.RemoveCacheEntry(resourceKey);

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

				// A lazy resource living in a theme sub-dictionary (e.g. the Light ApplicationPageBackgroundThemeBrush
				// whose Color is a {StaticResource SolidBackgroundFillColorBase}) bakes its nested {StaticResource}/
				// {ThemeResource} the FIRST time it materializes — then caches the result in _values forever. Under
				// an opposite-theme app (Light resource materialized while the app is Dark) that bakes the wrong
				// theme's value permanently. Scope the active theme to the theme this lookup is resolving under —
				// the ambient active theme (GetActiveTheme: the core requested-theme-for-subtree slot when scoped,
				// else the app/OS base theme) — so the initializer resolves nested refs in the sub-dictionary's own
				// theme. Mirrors WinUI resolving a deferred theme-dictionary resource under the requested theme
				// (EnsureActiveThemeDictionary, Resources.cpp:687-819, theme selection :764-768).
				var themeKey = GetActiveTheme();
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
		// The alias target resolves under the same ambient active theme as the alias lookup itself (the
		// dictionary leaves read GetActiveTheme), so an alias inside a theme sub-dictionary (e.g.
		// SystemControlFocusVisualPrimaryBrush → FocusStrokeColorOuterBrush) resolves its target in the
		// same theme as the alias. Matches WinUI's LookupThemeResource(theme, key) (xcpcore.cpp).
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
				_mergedDictionaries[index] = RetrieveDictionaryForSourceWithAlcAwareness(merged.Source);
			}
			else
			{
				throw new InvalidOperationException("The provided dictionary cannot be found in the merged list");
			}
		}

		private ResourceDictionary _activeThemeDictionary;

		// Uno: the active sub-dictionary's theme is cached on two axes — the ResourceKey carries the
		// base ("Light"/"Dark", or pre-app-theme-init "Default") dimension for the hot-path comparison
		// against GetActiveTheme(), and the packed Theme below carries WinUI's full m_activeTheme
		// shape including the high-contrast bits.
		private ResourceKey _activeTheme;

		// m_pActiveThemeDictionary's theme
		// MUX Reference: CResourceDictionary::m_activeTheme (Resources.h:511-512), commit fc2f82117.
		private Theme _activeThemeValue;

		// MUX Reference: CResourceDictionary::m_isHighContrast / MarkIsHighContrast
		// (Resources.h:350-353,507), commit fc2f82117. Set on the active theme sub-dictionary when it
		// was selected through the high-contrast keys (EnsureActiveThemeDictionary, Resources.cpp:800-803).
		internal bool IsHighContrast { get; private set; }

		internal void MarkIsHighContrast() => IsHighContrast = true;

		// MUX Reference: CResourceDictionary::GetKeyFromThemeDictionariesNoRef (Resources.cpp:644-685), commit fc2f82117 —
		// ensures the active theme dictionary, then looks the key up in it.
		private ResourceDictionary GetActiveThemeDictionary()
		{
			EnsureActiveThemeDictionary();
			return _activeThemeDictionary;
		}

		// MUX Reference: CResourceDictionary::EnsureActiveThemeDictionary (Resources.cpp:687-819), commit fc2f82117.
		private void EnsureActiveThemeDictionary()
		{
			if (_themeDictionaries is null)
			{
				return;
			}

			// Update active theme dictionary if theme has changed.
			// Don't update for old apps, for which theme change was not supported.

			// MUX (Resources.cpp:699-700) splits the base comparison per source:
			//   baseThemeChanged = !core->IsThemeRequestedForSubTree() && (Theming::GetBaseValue(m_activeTheme) != core->GetFrameworkTheming()->GetBaseTheme())
			//   requestedThemeChanged = core->IsThemeRequestedForSubTree() && (Theming::GetBaseValue(m_activeTheme) != core->GetRequestedThemeForSubTree())
			// Uno: GetActiveTheme() composes those same two sources (the core requested-theme-for-subtree
			// slot when one is scoped, else the app/OS base theme), so both collapse into one key comparison.
			var activeThemeKey = GetActiveTheme();
			var baseOrRequestedThemeChanged = !activeThemeKey.Equals(_activeTheme);

			// MUX (Resources.cpp:701): highContrastChanged = (m_activeTheme & Theme::HighContrastMask) != core->GetFrameworkTheming()->GetHighContrastTheme()
			// Uno: high contrast is a single app-global bool — the FrameworkTheming::GetHighContrastTheme()
			// analog collapsed to HighContrast/HighContrastNone.
			var highContrastTheme = Themes.IsHighContrast ? Theme.HighContrast : Theme.HighContrastNone;
			var highContrastChanged = Theming.GetHighContrastValue(_activeThemeValue) != highContrastTheme;

			if (_activeThemeDictionary is null ||                                       // No active theme dictionary
				(baseOrRequestedThemeChanged || highContrastChanged))                   // and theme changed
			{
				ResourceDictionary resources = null;
				bool isHighContrast = false;

#if UNO_HAS_ENHANCED_LIFECYCLE
				// If we already have an active theme dictionary, that means we're trying to
				// resolve a different one because of a theme switch.
				var themeSwitchOccurred = _activeThemeDictionary is not null;
#endif

				// Find theme dictionary corresponding to current theme

				// A high contrast theme was requested:
				// - If a subtree requested a theme, we will use the high contrast version of that theme.
				// - Otherwise, we will pick the high contrast for the app-wide theme.
				if (highContrastTheme != Theme.HighContrastNone)
				{
					// MUX (Resources.cpp:720-754): when a subtree requested a theme, switch on
					// core->GetRequestedThemeForSubTree() — Light → "HighContrastWhite", Dark →
					// "HighContrastBlack" ("We should never get here because IsThemeRequestedForSubTree()
					// returned true." otherwise); else switch on the OS high-contrast variant
					// (core->GetFrameworkTheming()->GetHighContrastTheme()) — HighContrastBlack /
					// HighContrastWhite / HighContrastCustom → the same-named key.
					// Uno: GetActiveTheme() already composes requested-or-base, so the subtree branch is
					// the base-derived variant key below, and the app-wide branch collapses onto it
					// because high contrast is a single bool.
					// TODO Uno: detect the OS high-contrast variant (White/Black/Custom —
					// SystemThemingInterop.GetSystemHighContrastTheme) and port the app-wide variant
					// switch 1:1.
					resources = GetThemeDictionary(GetHighContrastKeyForBaseTheme(activeThemeKey));

					if (resources is null)
					{
						resources = GetThemeDictionary(Themes.HighContrast);
					}

					isHighContrast = resources is not null;
				}

				if (resources is null)
				{
					// If a subtree requested a theme, get it. Otherwise get the app-wide theme.
					// Uno: GetActiveTheme() composes exactly that (activeThemeKey). MUX gates the keyed
					// lookup on requestedTheme != Theme::None (Resources.cpp:769); the pre-app-theme-init
					// "Default" key is Uno's None analog.
					if (!activeThemeKey.Equals(Themes.Default))
					{
						resources = GetThemeDictionary(activeThemeKey);
					}

					if (resources is null)
					{
						resources = GetThemeDictionary(Themes.Default);
					}
				}

				if (resources is not null)
				{
					// Remember active theme dictionary, to avoid finding it next
					// time if theme hasn't changed.
					var previousActiveThemeDictionary = _activeThemeDictionary;
					_activeThemeDictionary = resources;

					if (isHighContrast)
					{
						_activeThemeDictionary.MarkIsHighContrast();
					}

					// Remember active theme dictionary's theme
					_activeTheme = activeThemeKey;
					_activeThemeValue = GetActiveBaseTheme() | highContrastTheme;

					// Uno: keys cached as not-found may resolve in the newly-active sub-dictionary.
					// WinUI invalidates through the theme walk (CResourceDictionary::NotifyThemeChangedCore,
					// Resources.cpp:2206), which only covers walked dictionaries; invalidating at the switch
					// point covers dictionaries outside the walk (e.g. system dictionaries) too.
					if (!ReferenceEquals(previousActiveThemeDictionary, _activeThemeDictionary))
					{
						InvalidateNotFoundCache(false);
					}

#if UNO_HAS_ENHANCED_LIFECYCLE
					// Make sure we re-evaluate all ThemeResource expressions inside this
					// theme dictionary first if a theme switch occurred.
					if (themeSwitchOccurred)
					{
						// MUX: m_pActiveThemeDictionary->NotifyThemeChanged(m_activeTheme, highContrastChanged)
						// (Resources.cpp:809-814); the theme walk lives on the store.
						((IDependencyObjectStoreProvider)_activeThemeDictionary).Store.NotifyThemeChanged(_activeThemeValue, highContrastChanged);
					}
#endif
				}
			}
		}

		private static ResourceKey GetHighContrastKeyForBaseTheme(in ResourceKey baseTheme)
			=> baseTheme.Equals(Themes.Dark) ? Themes.HighContrastBlack : Themes.HighContrastWhite;

		// MUX: theme sub-dictionaries resolve with LookupScope::LocalOnly (self + merged + local theme,
		// Resources.cpp:725-784); the "HighContrast" and "Default" fallbacks additionally search the
		// global theme resources (LookupScope::GlobalTheme, Resources.cpp:758,789). Uno has no
		// global-theme key space for sub-dictionary names, so all lookups stay local
		// (shouldCheckSystem: false).
		private ResourceDictionary GetThemeDictionary(in ResourceKey theme)
		{
			object dict = null;
			if (_themeDictionaries?.TryGetValue(theme, out dict, shouldCheckSystem: false) ?? false)
			{
				return dict as ResourceDictionary;
			}

			return null;
		}

		private bool ContainsKeyTheme(in ResourceKey resourceKey)
		{
			return GetActiveThemeDictionary()?.ContainsKey(resourceKey, shouldCheckSystem: false) ?? ContainsKeyThemeMerged(resourceKey);
		}

		private bool ContainsKeyThemeMerged(in ResourceKey resourceKey)
		{
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].ContainsKeyTheme(resourceKey))
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

		// The single theme read of the resolution leaf: every theme sub-dictionary selection
		// (GetActiveThemeDictionary, lazy materialization) keys on this ambient.
#if UNO_HAS_ENHANCED_LIFECYCLE
		// MUX Reference: CResourceDictionary::EnsureActiveThemeDictionary theme selection
		// (Resources.cpp:764-768) — "core->IsThemeRequestedForSubTree() ?
		// core->GetRequestedThemeForSubTree() : core->GetFrameworkTheming()->GetBaseTheme()".
		// The requested-theme-for-subtree slot (set from the owner's m_theme at WinUI's scoped
		// set-points) takes precedence; Themes.Active remains the app/OS base fallback until
		// FrameworkTheming drives it (Phase 6).
		internal static ResourceKey GetActiveTheme()
			=> Uno.UI.Xaml.Core.CoreServices.HasInstance && Uno.UI.Xaml.Core.CoreServices.Instance.IsThemeRequestedForSubTree()
				? GetThemeKey(Uno.UI.Xaml.Core.CoreServices.Instance.GetRequestedThemeForSubTree())
				: Themes.Active;
#else
		internal static ResourceKey GetActiveTheme() => Themes.Active;
#endif

		// Maps a per-object Theme (CDependencyObject::m_theme) to the BASE Light/Dark sub-dictionary key —
		// the analog of WinUI's RequestedThemeForSubTree. High contrast is composed separately at the
		// resolution leaf (GetActiveThemeDictionary reads the OS/app-global high-contrast state, as WinUI's
		// EnsureActiveThemeDictionary reads FrameworkTheming, Resources.cpp:718,740), so the key stays base.
		internal static ResourceKey GetThemeKey(Theme theme)
			=> Theming.GetBaseValue(theme) == Theme.Light ? Themes.Light : Themes.Dark;

		// The active base theme expressed as a base Theme. This is the single owner-less fallback for
		// {ThemeResource} resolution: it is what the lazy-materialization leaf keys on (GetActiveTheme),
		// matching WinUI's EnsureActiveThemeDictionary theme selection (Resources.cpp:764-768) — the
		// requested-theme-for-subtree slot when one is scoped, else the app/OS base theme
		// (FrameworkTheming::GetBaseTheme; Themes.Active until Phase 6). "Default" (pre-app-theme-init)
		// resolves to the app's ActualElementTheme so the result is always a concrete Light/Dark.
		internal static Theme GetActiveBaseTheme()
		{
			var active = GetActiveTheme();
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

			// The application/OS base theme. The per-subtree theme lives in the core
			// requested-theme-for-subtree slot (CCoreServices::m_requestedThemeForSubTree), which
			// GetActiveTheme reads first; Active is the fallback for lookups with no scoped subtree
			// theme, and is strictly "Light"/"Dark".
			public static SpecializedResourceDictionary.ResourceKey Active { get; set; } = Default;

			// The OS/app-global high-contrast state (MUX: FrameworkTheming::HasHighContrastTheme). Read live
			// from the accessibility settings — high contrast is orthogonal to the Light/Dark base theme and
			// is composed at the resolution leaf (GetActiveThemeDictionary), matching WinUI reading it from
			// FrameworkTheming rather than from the per-object/subtree theme.
			public static bool IsHighContrast => SystemThemeHelper.IsHighContrast;
		}
	}
}
