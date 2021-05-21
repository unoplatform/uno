//#define DEBUG_SET_RESOURCE_SOURCE
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

using ResourceKey = Windows.UI.Xaml.SpecializedResourceDictionary.ResourceKey;
using System.Runtime.CompilerServices;

namespace Windows.UI.Xaml
{
	public partial class ResourceDictionary : DependencyObject, IDependencyObjectParse, IDictionary<object, object>
	{
		private readonly SpecializedResourceDictionary _values = new SpecializedResourceDictionary();
		private readonly List<ResourceDictionary> _mergedDictionaries = new List<ResourceDictionary>();
		private ResourceDictionary _themeDictionaries;

		/// <summary>
		/// If true, there may be lazily-set values in the dictionary that need to be initialized.
		/// </summary>
		private bool _hasUnmaterializedItems = false;

		public ResourceDictionary()
		{
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
		public IDictionary<object, object> ThemeDictionaries => _themeDictionaries ??= new ResourceDictionary();

		/// <summary>
		/// Is this a ResourceDictionary created from system resources, ie within the Uno.UI assembly?
		/// </summary>
		internal bool IsSystemDictionary { get; set; }

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
			Set(new ResourceKey(key), value, throwIfPresent: false);
			return true;
		}

		public bool Remove(object key) => _values.Remove(new ResourceKey(key));

		public bool Remove(KeyValuePair<object, object> key) => _values.Remove(new ResourceKey(key.Key));

		public void Clear() => _values.Clear();

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
		internal bool TryGetValue(in ResourceKey resourceKey, out object value, bool shouldCheckSystem) =>
			TryGetValue(resourceKey, ResourceKey.Empty, out value, shouldCheckSystem);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryGetValue(in ResourceKey resourceKey, ResourceKey activeTheme, out object value, bool shouldCheckSystem)
		{
			if (_values.TryGetValue(resourceKey, out value))
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

			if (activeTheme.IsEmpty)
			{
				activeTheme = Themes.Active;
			}

			if (GetFromMerged(resourceKey, out value))
			{
				return true;
			}

			if (GetFromTheme(resourceKey, activeTheme, out value))
			{
				return true;
			}

			if (shouldCheckSystem && !IsSystemDictionary) // We don't fall back on system resources from within a system-defined dictionary, to avoid an infinite recurse
			{
				return ResourceResolver.TrySystemResourceRetrieval(resourceKey, out value);
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
			set => Set(new ResourceKey(key), value, throwIfPresent: false);
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
			}
			else
			{
				_values[resourceKey] = value;
			}
		}

		/// <summary>
		/// If retrieved element is a <see cref="LazyInitializer"/> stub, materialize the actual object and replace the stub.
		/// </summary>
		private void TryMaterializeLazy(in ResourceKey key, ref object value)
		{
			if (value is LazyInitializer lazyInitializer)
			{
				object newValue = null;
#if !HAS_EXPENSIVE_TRYFINALLY
				try
#endif
				{
					_values.Remove(key); // Temporarily remove the key to make this method safely reentrant, if it's a framework- or application-level theme dictionary
					ResourceResolver.PushNewScope(lazyInitializer.CurrentScope);
					newValue = lazyInitializer.Initializer();
				}
#if !HAS_EXPENSIVE_TRYFINALLY
				finally
#endif
				{
					value = newValue;
					_values[key] = newValue; // If Initializer threw an exception this will push null, to avoid running buggy initialization again and again (and avoid surfacing initializer to consumer code)
					ResourceResolver.PopScope();
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

		private ResourceDictionary _activeThemeDictionary;
		private ResourceKey _activeTheme;

		private ResourceDictionary GetActiveThemeDictionary(in ResourceKey activeTheme)
		{
			if (!activeTheme.Equals(_activeTheme))
			{
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

		private bool GetFromTheme(in ResourceKey resourceKey, in ResourceKey activeTheme, out object value)
		{
			var dict = GetActiveThemeDictionary(activeTheme);

			if (dict != null && dict.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
			{
				return true;
			}

			return GetFromThemeMerged(resourceKey, activeTheme, out value);
		}

		private bool GetFromThemeMerged(in ResourceKey resourceKey, in ResourceKey activeTheme, out object value)
		{
			var count = _mergedDictionaries.Count;

			for (int i = count - 1; i >= 0; i--)
			{
				if (_mergedDictionaries[i].GetFromTheme(resourceKey, activeTheme, out value))
				{
					return true;
				}
			}

			value = null;

			return false;
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
		private void CopyFrom(ResourceDictionary source)
		{
			_values.Clear();
			_mergedDictionaries.Clear();
			_themeDictionaries?.Clear();

			_values.AddRange(source._values);
			_mergedDictionaries.AddRange(source._mergedDictionaries);
			if (source._themeDictionaries != null)
			{
				_themeDictionaries ??= new ResourceDictionary();
				_themeDictionaries.CopyFrom(source._themeDictionaries);
			}
		}

		public global::System.Collections.Generic.ICollection<object> Keys
			=> _values.Keys.Select(k => ConvertKey(k.Key)).ToList();

		private static object ConvertKey(ResourceKey resourceKey)
			=> resourceKey.IsType ? Type.GetType(resourceKey.Key) : (object)resourceKey.Key;

		// TODO: this doesn't handle lazy initializers or aliases
		public global::System.Collections.Generic.ICollection<object> Values => _values.Values;

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
					yield return new KeyValuePair<object, object>(ConvertKey(kvp.Key.Key), aliased);
				}
				else
				{
					yield return new KeyValuePair<object, object>(ConvertKey(kvp.Key.Key), kvp.Value);
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
		internal void UpdateThemeBindings()
		{
			foreach (var item in _values.Values)
			{
				if (item is IDependencyObjectStoreProvider provider && provider.Store.Parent == null)
				{
					provider.Store.UpdateResourceBindings(isThemeChangedUpdate: true, containingDictionary: this);
				}
			}

			foreach (var mergedDict in _mergedDictionaries)
			{
				mergedDict.UpdateThemeBindings();
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

		internal static void SetActiveTheme(SpecializedResourceDictionary.ResourceKey key)
			=> Themes.Active = key;

		private static class Themes
		{
			public static SpecializedResourceDictionary.ResourceKey Light { get; } = "Light";
			public static SpecializedResourceDictionary.ResourceKey Default { get; } = "Default";
			public static SpecializedResourceDictionary.ResourceKey Active { get; set; } = Light;
		}
	}
}
