using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;

namespace Uno.UI
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class ResourceResolver
	{
		/// <summary>
		/// The ambient ALC context for resource resolution. Set during App.xaml initialization
		/// to ensure that resource dictionary lookups (including fallback paths like
		/// <see cref="RetrieveDictionaryForSource(string, string)"/>) can find resources
		/// registered in the correct ALC-scoped registry.
		/// </summary>
		[ThreadStatic]
		private static AssemblyLoadContext _currentResolutionAlc;

		/// <summary>
		/// Gets the current ambient ALC context for resource resolution.
		/// </summary>
		internal static AssemblyLoadContext CurrentResolutionAlc => _currentResolutionAlc;

		/// <summary>
		/// The master system resources dictionary.
		/// </summary>
		private static ResourceDictionary MasterDictionary =>
#if __NETSTD_REFERENCE__
			throw new InvalidOperationException();
#else
			Uno.UI.GlobalStaticResources.MasterDictionary;
#endif

		private static readonly Dictionary<string, Func<ResourceDictionary>> _registeredDictionariesByUri = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, ResourceDictionary> _registeredDictionariesByAssembly = new(StringComparer.Ordinal);
		/// <summary>
		/// This is used by hot reload (since converting the file path to a Source is impractical at runtime).
		/// </summary>
		private static readonly Dictionary<string, Func<ResourceDictionary>> _registeredDictionariesByFilepath = new Dictionary<string, Func<ResourceDictionary>>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// ALC-scoped resource dictionary registrations. Used to isolate resource dictionaries
		/// loaded from secondary AssemblyLoadContexts from those in the default ALC.
		/// </summary>
		private static readonly ConditionalWeakTable<System.Runtime.Loader.AssemblyLoadContext, Dictionary<string, Func<ResourceDictionary>>>
			_registeredDictionariesByUriByAlc = [];

		private static readonly object _alcDictionariesLock = new();

		private static int _assemblyRef = -1;

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{15E13473-560E-4601-86FF-C9E1EDB73701}");

			public const int InitGenericXamlStart = 1;
			public const int InitGenericXamlStop = 2;
		}

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		private static readonly Logger _log = typeof(ResourceResolver).Log();

		private static readonly Stack<XamlScope> _scopeStack;

		/// <summary>
		/// The current xaml scope for resource resolution.
		/// </summary>
		internal static XamlScope CurrentScope => _scopeStack.Peek();

		static ResourceResolver()
		{
			_scopeStack = new Stack<XamlScope>();
			_scopeStack.Push(XamlScope.Create()); //There should always be a base-level scope (this will be used when no template is being resolved)
		}

		/// <summary>
		/// Performs a one-time, typed resolution of a named resource, using Application.Resources.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static T ResolveResourceStatic<T>(object key, object context = null)
		{
			if (TryStaticRetrieval(new SpecializedResourceDictionary.ResourceKey(key), context, out var value) && value is T tValue)
			{
				return tValue;
			}

			return default(T);
		}

		/// <summary>
		/// Performs a one-time, typed resolution of a named resource, using Application.Resources.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object ResolveResourceStatic(
			object key,
			Type type,
			object context = null)
		{
			if (TryStaticRetrieval(new SpecializedResourceDictionary.ResourceKey(key), context, out var value))
			{
				if (value.GetType().Is(type))
				{
					return value;
				}
				else
				{
					var convertedValue = BindingPropertyHelper.Convert(type, value);
					if (convertedValue is null && _log.IsEnabled(LogLevel.Warning))
					{
						_log.LogWarning($"Unable to convert value '{value}' of type '{value.GetType()}' to type '{type}'");
					}

					return convertedValue;
				}
			}

			return null;
		}

		/// <summary>
		/// Performs a one-time resolution of a named resource, using Application.Resources.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		internal static bool ResolveResourceStatic(object key, out object value, object context = null)
			=> TryStaticRetrieval(new SpecializedResourceDictionary.ResourceKey(key), context, out value);

		/// <summary>
		/// Sets the default symbols font, assuming that the GlobalStaticResources have been initialized.
		/// </summary>
		/// <remarks>
		/// This method is needed to search for the resource dictionary containing the appropriate resources
		/// as the Application.Resources dictionary may change in structure based on how many resource dictionaries
		/// are included.
		/// </remarks>
		internal static void SetSymbolsFontFamily()
		{
			// We assume here that we only have one dictionary with theme resources for performance reasons
			if (FindSymbolFontFamilyDictionary() is { } genericDictionary)
			{
				var symbolsFontFamily = new FontFamily(global::Uno.UI.FeatureConfiguration.Font.SymbolsFont);

				var themes = new[] { "Default", "Light", "HighContrast" };

				foreach (var theme in themes)
				{
					if (genericDictionary.ThemeDictionaries.TryGetValue(theme, out var dictionary) && dictionary is ResourceDictionary themeDictionary)
					{
						themeDictionary["SymbolThemeFontFamily"] = symbolsFontFamily;
					}
					else
					{
						Debug.Fail($"Unable to find the {theme} theme dictionary to override font asset");
					}
				}
			}
			else
			{
				Debug.Fail("Unable to find theme dictionary to override font asset");
			}
		}

		private static ResourceDictionary FindSymbolFontFamilyDictionary()
		{
			// For loop to reduce allocations
			for (var mergedIndex = 0; mergedIndex < MasterDictionary.MergedDictionaries.Count; mergedIndex++)
			{
				var merged = MasterDictionary.MergedDictionaries[mergedIndex];

				foreach (var theme in merged.ThemeDictionaries)
				{
					if (theme.Value is ResourceDictionary themeDictionary && themeDictionary.ContainsKey("SymbolThemeFontFamily"))
					{
						return merged;
					}
				}
			}

			return null;
		}

#if false
		// disabled because of https://github.com/mono/mono/issues/20195
		/// <summary>
		/// Retrieve a resource from top-level resources (Application-level and system level).
		/// </summary>
		/// <typeparam name="T">The expected resource type</typeparam>
		/// <param name="key">The resource key to search for</param>
		/// <param name="fallbackValue">Fallback value to use if no resource is found</param>
		/// <returns>The resource, is one of the given key and type is found, else <paramref name="fallbackValue"/></returns>
		/// <remarks>
		/// Use <see cref="ResolveTopLevelResource{T}(object, T)"/> when user-defined Application-level values should be considered (most
		/// of the time), use <see cref="GetSystemResource{T}(object)"/> if they shouldn't
		/// </remarks>
		internal static T ResolveTopLevelResource<T>(object key, T fallbackValue = default)
		{
			if (TryTopLevelRetrieval(key, context: null, out var value) && value is T tValue)
			{
				return tValue;
			}

			return fallbackValue;
		}
#endif

		/// <summary>
		/// Retrieve a resource from top-level resources (Application-level and system level).
		/// </summary>
		/// <typeparam name="T">The expected resource type</typeparam>
		/// <param name="key">The resource key to search for</param>
		/// <param name="fallbackValue">Fallback value to use if no resource is found</param>
		/// <returns>The resource, is one of the given key and type is found, else <paramref name="fallbackValue"/></returns>
		/// <remarks>
		/// Use <see cref="ResolveTopLevelResource{T}(object, T)"/> when user-defined Application-level values should be considered (most
		/// of the time), use <see cref="GetSystemResource{T}(object)"/> if they shouldn't
		/// </remarks>
		internal static double ResolveTopLevelResourceDouble(SpecializedResourceDictionary.ResourceKey key, double fallbackValue = default)
		{
			if (TryTopLevelRetrieval(key, context: null, out var value) && value is double tValue)
			{
				return tValue;
			}

			return fallbackValue;
		}


		/// <summary>
		/// Retrieve a resource from top-level resources (Application-level and system level).
		/// </summary>
		/// <typeparam name="T">The expected resource type</typeparam>
		/// <param name="key">The resource key to search for</param>
		/// <param name="fallbackValue">Fallback value to use if no resource is found</param>
		/// <returns>The resource, is one of the given key and type is found, else <paramref name="fallbackValue"/></returns>
		/// <remarks>
		/// Use <see cref="ResolveTopLevelResource{T}(object, T)"/> when user-defined Application-level values should be considered (most
		/// of the time), use <see cref="GetSystemResource{T}(object)"/> if they shouldn't
		/// </remarks>
		internal static object ResolveTopLevelResource(SpecializedResourceDictionary.ResourceKey key, object fallbackValue = default)
		{
			if (TryTopLevelRetrieval(key, context: null, out var value) && value is object tValue)
			{
				return tValue;
			}

			return fallbackValue;
		}

		/// <summary>
		/// Apply a StaticResource or ThemeResource assignment to a DependencyProperty of a DependencyObject. The assignment will be provisionally
		/// made immediately using Application.Resources if possible, and retried at load-time using the visual-tree scope.
		/// </summary>
		/// <param name="owner">Owner of the property</param>
		/// <param name="property">The property to assign</param>
		/// <param name="resourceKey">Key to the resource</param>
		/// <param name="isThemeResourceExtension">True for {ThemeResource Foo}, false for {StaticResource Foo}</param>
		/// <param name="context">Optional parameter that provides parse-time context</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void ApplyResource(DependencyObject owner, DependencyProperty property, object resourceKey, bool isThemeResourceExtension, bool isHotReloadSupported, object context = null)
			=> ApplyResource(
				owner: owner,
				property: property,
				resourceKey: resourceKey,
				isThemeResourceExtension: isThemeResourceExtension,
				isHotReloadSupported,
				fromXamlParser: false,
				context: context);

		/// <summary>
		/// Apply a StaticResource or ThemeResource assignment to a DependencyProperty of a DependencyObject. The assignment will be provisionally
		/// made immediately using Application.Resources if possible, and retried at load-time using the visual-tree scope.
		/// </summary>
		/// <param name="owner">Owner of the property</param>
		/// <param name="property">The property to assign</param>
		/// <param name="resourceKey">Key to the resource</param>
		/// <param name="isThemeResourceExtension">True for {ThemeResource Foo}, false for {StaticResource Foo}</param>
		/// <param name="fromXamlParser">True when the invocation is performed from generated markup code</param>
		/// <param name="context">Optional parameter that provides parse-time context</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void ApplyResource(DependencyObject owner, DependencyProperty property, object resourceKey, bool isThemeResourceExtension, bool isHotReloadSupported, bool fromXamlParser, object context = null)
		{
			var updateReason = ResourceUpdateReason.None;
			if (isThemeResourceExtension)
			{
				updateReason |= ResourceUpdateReason.ThemeResource;
			}
			else
			{
				updateReason |= ResourceUpdateReason.StaticResourceLoading;
			}
			if (isHotReloadSupported && !FeatureConfiguration.Xaml.ForceHotReloadDisabled)
			{
				updateReason |= ResourceUpdateReason.HotReload;
			}

			if (fromXamlParser && ResourceResolver.CurrentScope.Sources.IsEmpty)
			{
				updateReason |= ResourceUpdateReason.XamlParser;
			}

			ApplyResource(owner, property, new SpecializedResourceDictionary.ResourceKey(resourceKey), updateReason, context, null);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
		internal static void ApplyResource(DependencyObject owner, DependencyProperty property, SpecializedResourceDictionary.ResourceKey specializedKey, ResourceUpdateReason updateReason, object context, DependencyPropertyValuePrecedences? precedence)
		{
			// If the invocation comes from XAML and from theme resources, resolution
			// must happen lazily, done through walking the visual tree.
			var immediateResolution =
				(updateReason & ResourceUpdateReason.XamlParser) != 0
				&& (updateReason & ResourceUpdateReason.ThemeResource) != 0;

			// Set initial value based on statically-available top-level resources.
			if (!immediateResolution && TryStaticRetrieval(specializedKey, context, out var value))
			{
				owner.SetValue(property, BindingPropertyHelper.Convert(property.Type, value), precedence);

				// If it's {StaticResource Foo} and we managed to resolve it at parse-time, then we don't want to update it again (per UWP).
				updateReason &= ~ResourceUpdateReason.StaticResourceLoading;

				if (updateReason == ResourceUpdateReason.None)
				{
					// If there's no other reason, don't create a resource binding.
					return;
				}
			}

			(owner as IDependencyObjectStoreProvider).Store.SetResourceBinding(property, specializedKey, updateReason, context, precedence, null);
		}

		/// <summary>
		/// Apply a <see cref="Setter"/> in a visual state whose value is theme-bound.
		/// </summary>
		/// <param name="resourceKey">Key to the resource</param>
		/// <param name="context">Optional parameter that provides parse-time context</param>
		/// <param name="bindingPath">The binding path defined by the Setter target</param>
		/// <param name="precedence">Value precedence</param>
		/// <returns>
		/// True if the value was successfully applied and registered for theme updates, false if no theme resource was found or the target
		/// property is not a <see cref="DependencyProperty"/>.
		/// </returns>
		internal static bool ApplyVisualStateSetter(SpecializedResourceDictionary.ResourceKey resourceKey, object context, BindingPath bindingPath, DependencyPropertyValuePrecedences precedence, ResourceUpdateReason updateReason)
		{
			if (TryVisualTreeRetrieval(resourceKey, context, out var value)
				&& bindingPath.DataContext != null)
			{
				var property = DependencyProperty.GetProperty(bindingPath.DataContext.GetType(), bindingPath.LeafPropertyName);
				if (property != null && bindingPath.DataContext is IDependencyObjectStoreProvider provider)
				{
					// Set current resource value
					bindingPath.Value = value;
					provider.Store.SetResourceBinding(property, resourceKey, updateReason, context, precedence, bindingPath);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Try to retrieve a resource using the visual tree determined
		/// from the immediate scope.
		/// </summary>
		private static bool TryVisualTreeRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value)
		{
			var scope = CurrentScope.Sources.FirstOrDefault();

			if (scope != null)
			{
				var dictionaries = (scope.Target as IDependencyObjectStoreProvider)?.Store.GetResourceDictionaries(true);

				if (dictionaries != null)
				{
					foreach (var dict in dictionaries)
					{
						if (dict.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
			}

			var topLevel = TryTopLevelRetrieval(resourceKey, context, out value);
			if (!topLevel && _log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"Couldn't statically resolve resource {resourceKey.Key}");
			}
			return topLevel;
		}

		/// <summary>
		/// Try to retrieve a resource statically (at parse time). This will check resources in 'xaml scope' first, then top-level resources.
		/// </summary>
		internal static bool TryStaticRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value)
		{
			// This block is a manual enumeration to avoid the foreach pattern
			// See https://github.com/dotnet/runtime/issues/56309 for details
			var sourcesEnumerator = CurrentScope.Sources.GetEnumerator();

			while (sourcesEnumerator.MoveNext())
			{

				var source = sourcesEnumerator.Current;

				var dictionary = (source.Target as FrameworkElement)?.TryGetResources()
					?? source.Target as ResourceDictionary;

				if (dictionary != null
					&& dictionary.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
				{
					return true;
				}
			}

			var topLevel = TryTopLevelRetrieval(resourceKey, context, out value);
			if (!topLevel && _log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"Couldn't statically resolve resource {resourceKey.Key}");
			}
			return topLevel;
		}

		/// <summary>
		/// Tries to retrieve a resource from top-level resources (Application-level and system level).
		/// </summary>
		/// <param name="resourceKey">The resource key</param>
		/// <param name="value">Out parameter to which the retrieved resource is assigned.</param>
		/// <returns>True if the resource was found, false if not.</returns>
		internal static bool TryTopLevelRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value)
		{
			value = null;

			// When secondary ALCs are active, check the secondary ALC's Application.Resources first
			if (Application.HasSecondaryApps
				&& context is XamlParseContext parseContext
				&& parseContext.AssemblyLoadContext is { } alc)
			{
				var alcApp = Application.GetForAssemblyLoadContext(alc);
				if (alcApp is not null && alcApp != Application.Current)
				{
					if (alcApp.Resources.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
					{
						return true;
					}
				}
			}

			// Fall through to host app resources (also the only path when context is null or no secondary ALCs)
			return (Application.Current?.Resources.TryGetValue(resourceKey, out value, shouldCheckSystem: false) ?? false)
				|| TryAssemblyResourceRetrieval(resourceKey, context, out value)
				|| TrySystemResourceRetrieval(resourceKey, out value);
		}

		/// <summary>
		/// Tries to retrieve a resource from the same assembly as the retrieving context. Used when parsing third-party libraries
		/// (ie not application XAML, and not Uno.UI XAML)
		/// </summary>
		private static bool TryAssemblyResourceRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value)
		{
			value = null;
			if (!(context is XamlParseContext parseContext))
			{
				return false;
			}

			if (parseContext.AssemblyName == "Uno.UI")
			{
				return false;
			}

			if (parseContext.AssemblyName is not null)
			{
				if (_registeredDictionariesByAssembly.TryGetValue(parseContext.AssemblyName, out var assemblyDict))
				{
					foreach (var kvp in assemblyDict)
					{
						var rd = kvp.Value as ResourceDictionary;
						if (rd.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Try to retrieve a resource value from system-level resources.
		/// </summary>
		internal static bool TrySystemResourceRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, out object value) => MasterDictionary.TryGetValue(resourceKey, out value, shouldCheckSystem: false);

		internal static bool ContainsKeySystem(in SpecializedResourceDictionary.ResourceKey resourceKey) => MasterDictionary.ContainsKey(resourceKey, shouldCheckSystem: false);

		/// <summary>
		/// Get a system-level resource with the given key.
		/// </summary>
		/// <remarks>
		/// Use <see cref="ResolveTopLevelResource{T}(object, T)"/> when user-defined Application-level values should be considered (most
		/// of the time), use <see cref="GetSystemResource{T}(object)"/> if they shouldn't
		/// </remarks>
		internal static T GetSystemResource<T>(object key)
		{
			if (MasterDictionary.TryGetValue(key, out var value, shouldCheckSystem: false) && value is T t)
			{
				return t;
			}

			return default(T);
		}

		/// <summary>
		/// Push a new <see cref="XamlScope"/>, typically because a template is being materialized.
		/// </summary>
		/// <param name="scope"></param>
		internal static void PushNewScope(XamlScope scope) => _scopeStack.Push(scope);
		/// <summary>
		/// Push a new Resources source to the current xaml scope.
		/// </summary>
		internal static void PushSourceToScope(DependencyObject source) => PushSourceToScope((source as IWeakReferenceProvider).WeakReference);
		/// <summary>
		/// Push a new Resources source to the current xaml scope.
		/// </summary>
		internal static void PushSourceToScope(ManagedWeakReference source)
		{
			var current = _scopeStack.Pop();
			_scopeStack.Push(current.Push(source));
		}
		/// <summary>
		/// Pop Resources source from current xaml scope.
		/// </summary>
		internal static void PopSourceFromScope()
		{
			var current = _scopeStack.Pop();
			_scopeStack.Push(current.Pop());
		}
		/// <summary>
		/// Pop current <see cref="XamlScope"/>, typically because template materialization is complete.
		/// </summary>
		internal static void PopScope()
		{
			_scopeStack.Pop();
			if (_scopeStack.Count == 0)
			{
				throw new InvalidOperationException("Base scope should never be popped.");
			}
		}

		/// <summary>
		/// If tracing is enabled, writes an event for the initialization of system-level resources (Generic.xaml etc)
		/// </summary>
		internal static IDisposable WriteInitiateGlobalStaticResourcesEventActivity() => _trace.WriteEventActivity(TraceProvider.InitGenericXamlStart, TraceProvider.InitGenericXamlStop);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterResourceDictionaryBySource(string uri, XamlParseContext context, Func<ResourceDictionary> dictionary)
			=> RegisterResourceDictionaryBySource(uri, context, dictionary, null);
		/// <summary>
		/// Register a dictionary for a given source, this is used for retrieval when setting the Source property in code-behind or to an
		/// external resource.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterResourceDictionaryBySource(string uri, XamlParseContext context, Func<ResourceDictionary> dictionary, string filePath)
		{
			_registeredDictionariesByUri[uri] = dictionary;

			if (context != null)
			{
				var isGenericXaml = uri.EndsWith("themes/generic.xaml", StringComparison.OrdinalIgnoreCase);

				if (isGenericXaml || FeatureConfiguration.ResourceDictionary.IncludeUnreferencedDictionaries)
				{
					// We store the dictionaries inside a ResourceDictionary to utilize the lazy-loading machinery
					// to convert ResourceDictionary.ResourceInitializer into actual instances
					if (!_registeredDictionariesByAssembly.TryGetValue(context.AssemblyName, out var assemblyDict))
					{
						_registeredDictionariesByAssembly[context.AssemblyName] = assemblyDict = new();
					}

					var initializer = new ResourceDictionary.ResourceInitializer(dictionary);
					_assemblyRef++; // We don't actually use this key, we just need it to be unique
					assemblyDict[_assemblyRef] = initializer;
				}
			}

			if (filePath != null)
			{
				_registeredDictionariesByFilepath[filePath] = dictionary;
			}
		}

		/// <summary>
		/// Register a dictionary for a given source with ALC awareness.
		/// When called from a non-default AssemblyLoadContext, the registration is scoped to that ALC.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterResourceDictionaryBySource(
			string uri,
			XamlParseContext context,
			Func<ResourceDictionary> dictionary,
			string filePath,
			System.Runtime.Loader.AssemblyLoadContext alc)
		{
			// Always register in the ALC-scoped registry for non-default ALCs,
			// regardless of HasSecondaryApps. The flag may not be set yet at
			// registration time (it's set when Application.Start runs), but
			// registration happens earlier during GlobalStaticResources.Initialize.
			if (alc is not null && alc != System.Runtime.Loader.AssemblyLoadContext.Default)
			{
				lock (_alcDictionariesLock)
				{
					if (!_registeredDictionariesByUriByAlc.TryGetValue(alc, out var alcDict))
					{
						alcDict = new Dictionary<string, Func<ResourceDictionary>>(StringComparer.OrdinalIgnoreCase);
						_registeredDictionariesByUriByAlc.Add(alc, alcDict);
					}
					alcDict[uri] = dictionary;
				}

				// Note: For non-default ALCs, we don't register by filepath for hot reload
				// as hot reload is typically used in the main application context.
				return;
			}

			// Default ALC - use existing global registration
			RegisterResourceDictionaryBySource(uri, context, dictionary, filePath);
		}

		/// <summary>
		/// Retrieve the ResourceDictionary mapping to a given source. Throws an exception if none is found.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static ResourceDictionary RetrieveDictionaryForSource(Uri source)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			// Only do the expensive ALC lookup if we know secondary ALCs have registered resources
			if (Application.HasSecondaryApps)
			{
				var callingAlc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly());
				return RetrieveDictionaryForSource(source.AbsoluteUri, currentAbsolutePath: null, callingAlc);
			}

			return RetrieveDictionaryForSource(source.AbsoluteUri, currentAbsolutePath: null);
		}

		/// <summary>
		/// Retrieve the ResourceDictionary mapping to a given source. Throws an exception if none is found.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		public static ResourceDictionary RetrieveDictionaryForSource(string source, string currentAbsolutePath)
		{
			if (source == null)
			{
				// Null is unusual but valid in this context
				return new ResourceDictionary();
			}

			if (!XamlFilePathHelper.IsAbsolutePath(source))
			{
				// If we don't have an absolute path it must be a local resource reference
				source = XamlFilePathHelper.LocalResourcePrefix + XamlFilePathHelper.ResolveAbsoluteSource(currentAbsolutePath, source);
			}

			// When secondary ALCs are active, check the ALC-scoped registry first.
			// Use the ambient context if set, otherwise fall back to detecting the
			// calling assembly's ALC (for code compiled with older source generators
			// that don't wrap resource building with SetResolutionContext).
			if (Application.HasSecondaryApps)
			{
				var alc = _currentResolutionAlc
					?? AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly());

				if (alc is not null && alc != AssemblyLoadContext.Default)
				{
					lock (_alcDictionariesLock)
					{
						if (_registeredDictionariesByUriByAlc.TryGetValue(alc, out var alcDict) &&
							alcDict.TryGetValue(source, out var alcFactory))
						{
							return alcFactory();
						}
					}
				}
			}

			if (_registeredDictionariesByUri.TryGetValue(source, out var factory))
			{
				return factory();
			}

			// Last-resort fallback: when the ALC context couldn't be determined
			// (e.g., older generators without SetResolutionContext, JIT inlining
			// causing GetCallingAssembly to return a default-ALC assembly, or
			// indirect calls from Uno.UI framework code), search all ALC-scoped
			// registries. This handles the common single-secondary-ALC case.
			if (Application.HasSecondaryApps)
			{
				lock (_alcDictionariesLock)
				{
					foreach (var kvp in _registeredDictionariesByUriByAlc)
					{
						if (kvp.Value.TryGetValue(source, out var alcFallbackFactory))
						{
							return alcFallbackFactory();
						}
					}
				}
			}

			throw new InvalidOperationException($"Cannot locate resource from '{source}'");
		}

		/// <summary>
		/// Retrieve the ResourceDictionary mapping to a given source with ALC awareness.
		/// Checks the ALC-scoped registry first for non-default ALCs, then falls back to global registry.
		/// </summary>
		internal static ResourceDictionary RetrieveDictionaryForSource(
			string source,
			string currentAbsolutePath,
			System.Runtime.Loader.AssemblyLoadContext callingAlc)
		{
			if (source is null)
			{
				// Null is unusual but valid in this context
				return new ResourceDictionary();
			}

			if (!XamlFilePathHelper.IsAbsolutePath(source))
			{
				// If we don't have an absolute path it must be a local resource reference
				source = XamlFilePathHelper.LocalResourcePrefix + XamlFilePathHelper.ResolveAbsoluteSource(currentAbsolutePath, source);
			}

			// Try ALC-specific registry first (for non-default ALCs)
			if (callingAlc is not null && callingAlc != AssemblyLoadContext.Default)
			{
				lock (_alcDictionariesLock)
				{
					if (_registeredDictionariesByUriByAlc.TryGetValue(callingAlc, out var alcDict) &&
						alcDict.TryGetValue(source, out var alcFactory))
					{
						return alcFactory();
					}
				}
			}

			// Fall back to global (default ALC) registry
			if (_registeredDictionariesByUri.TryGetValue(source, out var factory))
			{
				return factory();
			}

			// Last-resort fallback: when callingAlc was Default (e.g., ambient context
			// wasn't set and caller fell back to Default) but the resource is only in
			// an ALC-scoped registry, search all ALC registries.
			if (Application.HasSecondaryApps)
			{
				lock (_alcDictionariesLock)
				{
					foreach (var kvp in _registeredDictionariesByUriByAlc)
					{
						if (kvp.Value.TryGetValue(source, out var alcFallbackFactory))
						{
							return alcFallbackFactory();
						}
					}
				}
			}

			throw new InvalidOperationException($"Cannot locate resource from '{source}'");
		}

		internal static bool TryRetrieveDictionaryForSource(Uri source, out ResourceDictionary resourceDictionary)
		{
			if (source?.AbsoluteUri is not { } absoluteUriString)
			{
				resourceDictionary = null;
				return false;
			}

			if (_registeredDictionariesByUri.TryGetValue(absoluteUriString, out var factory))
			{
				resourceDictionary = factory();
				return true;
			}

			resourceDictionary = null;
			return false;
		}

		internal static ResourceDictionary RetrieveDictionaryForFilePath(string filePath)
		{
			if (_registeredDictionariesByFilepath.TryGetValue(filePath, out var func))
			{
				return func();
			}

			return null;
		}

		/// <summary>
		/// Retrieves a resource for a {CustomResource} markup, with the <see cref="CustomXamlResourceLoader"/> currently set.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static T RetrieveCustomResource<T>(string resourceId, string objectType, string propertyName, string propertyType)
		{
			if (CustomXamlResourceLoader.Current == null)
			{
				throw new InvalidOperationException("No custom resource loader set.");
			}

			var resource = CustomXamlResourceLoader.Current.GetResourceInternal(resourceId, objectType, propertyName, propertyType);

			if (resource is T t)
			{
				return t;
			}

			return default(T);
		}

		/// <summary>
		/// Supports the use of StaticResource alias with ResourceKey in Xaml markup.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object ResolveStaticResourceAlias(string resourceKey, object parseContext)
			=> ResourceDictionary.GetStaticResourceAliasPassthrough(resourceKey, parseContext as XamlParseContext);

		internal static void UpdateSystemThemeBindings(ResourceUpdateReason updateReason) => MasterDictionary.UpdateThemeBindings(updateReason);

		/// <summary>
		/// Sets the ambient ALC context for resource resolution and returns an <see cref="IDisposable"/>
		/// that restores the previous context when disposed.
		/// </summary>
		/// <param name="alc">The <see cref="AssemblyLoadContext"/> to use for resource resolution.</param>
		/// <returns>An <see cref="IDisposable"/> that restores the previous ALC context.</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static IDisposable SetResolutionContext(AssemblyLoadContext alc)
		{
			var previous = _currentResolutionAlc;
			_currentResolutionAlc = alc;
			return new ResolutionContextScope(previous);
		}

		private sealed class ResolutionContextScope : IDisposable
		{
			private readonly AssemblyLoadContext _previous;

			public ResolutionContextScope(AssemblyLoadContext previous)
				=> _previous = previous;

			public void Dispose()
				=> _currentResolutionAlc = _previous;
		}
	}
}
