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

		// Phase 4 (D3, Mechanism 1) — theme-aware one-time resolution: resolves the named resource (and any
		// StaticResource alias chain) against the explicitly passed owner theme rather than the process-global
		// active theme. Used by ResourceDictionary.TryResolveAlias so a {StaticResource} alias inside a theme
		// sub-dictionary (e.g. SystemControlFocusVisualPrimaryBrush → FocusStrokeColorOuterBrush) resolves its
		// target in the same theme as the alias, matching WinUI's LookupThemeResource(theme, key).
		[EditorBrowsable(EditorBrowsableState.Never)]
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		internal static bool ResolveResourceStatic(object key, in SpecializedResourceDictionary.ResourceKey themeKey, out object value, object context = null)
			=> TryStaticRetrieval(new SpecializedResourceDictionary.ResourceKey(key), themeKey, context, out value);

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
		/// Retrieve a top-level resource resolving the Light/Dark sub-dictionary against the explicitly
		/// passed <paramref name="themeKey"/> (the resolving owner's effective theme) rather than the
		/// process-global active theme. Mirrors WinUI resolving against <c>targetObject->GetTheme()</c>
		/// (D3, Mechanism 1 — architecture.md §6); replaces the band-aid theme push at owner-theme call sites.
		/// </summary>
		internal static object ResolveTopLevelResource(SpecializedResourceDictionary.ResourceKey key, in SpecializedResourceDictionary.ResourceKey themeKey, object fallbackValue = default)
		{
			if (TryTopLevelRetrieval(key, themeKey, context: null, out var value) && value is object tValue)
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
			var isThemeResource = (updateReason & ResourceUpdateReason.ThemeResource) != 0;

			// If the invocation comes from XAML and from theme resources, resolution
			// must happen lazily, done through walking the visual tree.
			var immediateResolution =
				(updateReason & ResourceUpdateReason.XamlParser) != 0
				&& isThemeResource;

			var effectivePrecedence = precedence ?? DependencyPropertyValuePrecedences.Local;

			// Set the initial value from statically-available top-level resources.
			// This uses the 3-parameter TryStaticRetrieval overload, which does not capture
			// the providing ResourceDictionary. For theme resources, dictionary pinning is
			// deferred until the load-time re-resolution path.
			// R1 (resolution scope): resolve the initial value AND capture the providing dictionary, mirroring
			// WinUI's CThemeResource::SetInitialValueAndTargetDictionary (ThemeResource.cpp:39-51). For a
			// {ThemeResource} the providing dictionary is pinned into the ThemeResourceReference so RefreshValue
			// can re-resolve from it after the element is reparented (e.g. popup/flyout/tooltip content moved under
			// PopupRoot), where the load-time visual-tree walk can no longer reach the opener's local
			// ThemeDictionaries. ResourceDictionary.TryGetValue returns the dictionary that OWNS the
			// ThemeDictionaries (not the Light/Dark sub-dictionary — see ResourceDictionary.cs:394-401), so a later
			// theme change re-selects the correct sub-dictionary on re-query.
			if (!immediateResolution && TryStaticRetrieval(specializedKey, context, out var value, out var providingDictionary))
			{
				owner.SetValue(property, BindingPropertyHelper.Convert(property.Type, value), precedence);

				// If it's {StaticResource Foo} and we managed to resolve it at parse-time, then we don't want to update it again (per UWP).
				updateReason &= ~ResourceUpdateReason.StaticResourceLoading;

				if (isThemeResource)
				{
					// Pin the providing dictionary captured above (R1). The _resourceBindings tree-walk at load
					// time may also re-pin it for in-tree content, but parse-time pinning is what lets reparented
					// (popup/flyout/tooltip) content re-resolve a locally-declared {ThemeResource} from the opener's
					// dictionary, since its load-time walk dead-ends at the PopupRoot.
					var themeRef = new ThemeResourceReference(
						specializedKey, providingDictionary, value, isResolved: true, context,
						updateReason, effectivePrecedence);
					(owner as IDependencyObjectStoreProvider)?.Store.SetThemeResourceBinding(property, themeRef);

					// Also register a ResourceBinding for the initial tree-walk resolution
					// at load time, which will also pin the providing dictionary.
					(owner as IDependencyObjectStoreProvider)?.Store.SetResourceBinding(property, specializedKey, updateReason, context, precedence, null);
					return;
				}

				if (updateReason == ResourceUpdateReason.None)
				{
					// If there's no other reason, don't create a resource binding.
					return;
				}

				// Non-theme persistent binding (HotReload) -- use old path
				(owner as IDependencyObjectStoreProvider)?.Store.SetResourceBinding(property, specializedKey, updateReason, context, precedence, null);
				return;
			}

			if (isThemeResource)
			{
				// Couldn't resolve yet (deferred until loading). Create a ThemeResourceReference
				// without a pinned dictionary -- it will be resolved via tree-walk at load time.
				var themeRef = new ThemeResourceReference(
					specializedKey, null, null, isResolved: false, context,
					updateReason, effectivePrecedence);
				(owner as IDependencyObjectStoreProvider)?.Store.SetThemeResourceBinding(property, themeRef);
			}

			// Also register in the old ResourceBinding path to ensure deferred resolution
			// on loading still works. The _resourceBindings path handles the initial tree-walk
			// resolution, and the _themeResources path handles efficient theme-change re-resolution.
			(owner as IDependencyObjectStoreProvider)?.Store.SetResourceBinding(property, specializedKey, updateReason, context, precedence, null);
		}

		/// <summary>
		/// Apply a pre-existing <see cref="ThemeResourceReference"/> from a Setter to a target DependencyObject.
		/// </summary>
		/// <remarks>
		/// MUX Reference: When a Style setter with PreserveThemeResourceExtension is applied to a control,
		/// the stored CThemeResource is used to create a live binding on the target element.
		/// </remarks>
		internal static void ApplyThemeResource(DependencyObject owner, DependencyProperty property, ThemeResourceReference themeRef, DependencyPropertyValuePrecedences? precedence)
		{
			var effectivePrecedence = precedence ?? themeRef.Precedence;

			// D3 (Mechanism 1): resolve the current value from the pinned dictionary against the OWNER's
			// effective theme (the setter target), threaded as a parameter — the style / visual-state setter
			// call sites no longer push the element's theme onto the global stack.
			var value = themeRef.RefreshValue(ThemeResolution.ResolveOwnerTheme(owner));
			// MUX Reference: Theming.cpp:385-393 — SetValue uses baseValueSource (resolved precedence)
			owner.SetValue(property, BindingPropertyHelper.Convert(property.Type, value), effectivePrecedence);

			// Clone the reference for the target DO (with target's precedence, sharing pinned dictionary)
			var targetRef = themeRef.CloneForTarget(effectivePrecedence);
			(owner as IDependencyObjectStoreProvider)?.Store.SetThemeResourceBinding(property, targetRef);
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
			// D3 (Mechanism 1): resolve the setter's {ThemeResource} against the TARGET element's effective theme
			// (bindingPath.DataContext is the setter target), threaded as a parameter — the visual-state setter
			// call site no longer pushes the element's theme onto the global stack.
			var themeKey = ResourceDictionary.GetThemeKey(ThemeResolution.ResolveOwnerTheme(bindingPath.DataContext as DependencyObject));
			if (TryVisualTreeRetrieval(resourceKey, themeKey, context, out var value, out var providingDictionary)
				&& bindingPath.DataContext != null)
			{
				var property = DependencyProperty.GetProperty(bindingPath.DataContext.GetType(), bindingPath.LeafPropertyName);
				if (property != null && bindingPath.DataContext is IDependencyObjectStoreProvider provider)
				{
					// Set current resource value
					bindingPath.Value = value;

					if ((updateReason & ResourceUpdateReason.ThemeResource) != 0)
					{
						// Use pinned-dictionary path for theme resources
						var themeRef = new ThemeResourceReference(
							resourceKey, providingDictionary, value, isResolved: true, context,
							updateReason, precedence, bindingPath);
						provider.Store.SetThemeResourceBinding(property, themeRef);
					}

					// Always register ResourceBinding for re-pin at load time and
					// HotReload support, matching the dual-registration in ApplyResource.
					provider.Store.SetResourceBinding(property, resourceKey, updateReason, context, precedence, bindingPath);

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Try to retrieve a resource from the visual tree, also returning the providing dictionary.
		/// </summary>
		private static bool TryVisualTreeRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value, out ResourceDictionary providingDictionary)
		{
			var scope = CurrentScope.Sources.FirstOrDefault();

			if (scope != null)
			{
				var dictionaries = (scope.Target as IDependencyObjectStoreProvider)?.Store.GetResourceDictionaries(true);

				if (dictionaries != null)
				{
					foreach (var dict in dictionaries)
					{
						if (dict.TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
			}

			var topLevel = TryTopLevelRetrieval(resourceKey, context, out value, out providingDictionary);
			if (!topLevel && _log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"Couldn't statically resolve resource {resourceKey.Key}");
			}
			return topLevel;
		}

		// Phase 4 (D3, Mechanism 1) — theme-aware visual-tree retrieval: select the Light/Dark sub-dictionary
		// (incl. StaticResource aliases) against the explicitly passed owner theme rather than the process-global
		// active theme. Used by ApplyVisualStateSetter so a visual-state setter's {ThemeResource} resolves
		// against the setter target's effective theme.
		private static bool TryVisualTreeRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, in SpecializedResourceDictionary.ResourceKey themeKey, object context, out object value, out ResourceDictionary providingDictionary)
		{
			var scope = CurrentScope.Sources.FirstOrDefault();

			if (scope != null)
			{
				var dictionaries = (scope.Target as IDependencyObjectStoreProvider)?.Store.GetResourceDictionaries(true);

				if (dictionaries != null)
				{
					foreach (var dict in dictionaries)
					{
						if (dict.TryGetValue(resourceKey, themeKey, out value, out providingDictionary, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
			}

			var topLevel = TryTopLevelRetrieval(resourceKey, themeKey, context, out value, out providingDictionary);
			if (!topLevel && _log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"Couldn't statically resolve resource {resourceKey.Key}");
			}
			return topLevel;
		}

		// Old TryVisualTreeRetrieval overload removed - replaced by the overload
		// that also returns the providing dictionary for theme resource pinning.

		/// <summary>
		/// Try to retrieve a resource statically (at parse time). This will check resources in 'xaml scope' first, then top-level resources.
		/// </summary>
		internal static bool TryStaticRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value)
		{
			foreach (var source in CurrentScope.Sources)
			{

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
		/// Try to retrieve a resource statically, selecting the Light/Dark sub-dictionary against the
		/// explicitly passed <paramref name="themeKey"/> (the resolving owner's effective theme) rather than
		/// the process-global active theme (D3, Mechanism 1 — architecture.md §6).
		/// </summary>
		internal static bool TryStaticRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, in SpecializedResourceDictionary.ResourceKey themeKey, object context, out object value)
		{
			foreach (var source in CurrentScope.Sources)
			{
				var dictionary = (source.Target as FrameworkElement)?.TryGetResources()
					?? source.Target as ResourceDictionary;

				if (dictionary != null
					&& dictionary.TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false))
				{
					return true;
				}
			}

			var topLevel = TryTopLevelRetrieval(resourceKey, themeKey, context, out value);
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

			// Resource lookup priority when secondary-ALC apps are registered:
			//  1. If parseContext.AssemblyLoadContext identifies a specific secondary app
			//     (i.e. a non-default ALC), try that app first, then other secondary apps.
			//     Brush XAML in a secondary-ALC assembly is uniquely owned by that app, and
			//     a secondary app's overrides beat sibling-secondary apps' defaults.
			//  2. The host (Application.Current). Default-ALC parse contexts (and lookups
			//     with no parse context) resolve here — the host owns those keys and must
			//     not be silently overruled by a secondary app's same-key value.
			//  3. Last-resort: iterate registered secondary apps. Required when a brush in
			//     a shared (default-ALC) assembly references a key via {StaticResource X}
			//     and X exists ONLY in a secondary app's Application.Resources — the parse
			//     context points at the default ALC, so we cannot identify the owning app
			//     up front.
			//  4. Assembly / system resources.
			//
			// NOTE: do NOT iterate secondary apps before the host for default-ALC parse
			// contexts. The host's own UI elements emit default-ALC parse contexts; if we
			// query secondary apps first, any same-key resource defined by an imported app
			// bleeds into the host's
			// chrome and recolors host UI. The "same-key override should win for the
			// secondary app" scenario (BrewHouse-style ColorOverrideDictionary on a
			// shared-ALC brush) requires an owning-app hint plumbed through brush
			// materialization, not a guess based on parseContext alone.
			Application contextApp = null;
			if (Application.HasSecondaryApps
				&& context is XamlParseContext parseContext
				&& parseContext.AssemblyLoadContext is { } alc)
			{
				contextApp = Application.GetForAssemblyLoadContext(alc);

				if (contextApp is not null
					&& contextApp != Application.Current)
				{
					// Hot-reload guard: a parse context emitted by a previously loaded
					// secondary-ALC build still references that build's ALC even after a
					// new build has registered a fresh Application instance with the same
					// Type.FullName. Resolving via the parse context's ALC would land on
					// the stale Application whose Resources reflect the OLD build's
					// values. Bump to the most recently registered live registration of
					// the same app type so the lookup sees the live app's resources.
					var liveApp = Application.GetLatestSecondaryApplicationForType(contextApp.GetType().FullName);
					if (liveApp is not null && liveApp != contextApp)
					{
						contextApp = liveApp;
					}

					if (contextApp.Resources.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
					{
						return true;
					}

					// Lookup originates from a secondary-app context: also consult other
					// secondary apps before the host, so that a sibling secondary app's
					// override is preferred over the host's same-key default.
					foreach (var secondaryApp in Application.EnumerateSecondaryApplications())
					{
						if (secondaryApp == contextApp)
						{
							continue;
						}

						if (secondaryApp.Resources.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
				else
				{
					// contextApp resolved to the host (default ALC) — treat as a host context;
					// no secondary-app priority applies.
					contextApp = null;
				}
			}

			if (Application.Current?.Resources.TryGetValue(resourceKey, out value, shouldCheckSystem: false) ?? false)
			{
				return true;
			}

			// Last-resort fallback for resources defined ONLY in a secondary ALC's
			// Application.Resources when the lookup originated from a host / default-ALC
			// parse context. Skipped when we already iterated above.
			if (Application.HasSecondaryApps && contextApp is null)
			{
				foreach (var secondaryApp in Application.EnumerateSecondaryApplications())
				{
					if (secondaryApp.Resources.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
					{
						return true;
					}
				}
			}

			if (TryAssemblyResourceRetrieval(resourceKey, context, out value))
			{
				return true;
			}

			return TrySystemResourceRetrieval(resourceKey, out value);
		}

		/// <summary>
		/// Tries to retrieve a top-level resource selecting the Light/Dark sub-dictionary against the
		/// explicitly passed <paramref name="themeKey"/> (the resolving owner's effective theme) rather than
		/// the process-global active theme (D3, Mechanism 1 — architecture.md §6).
		/// </summary>
		internal static bool TryTopLevelRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, in SpecializedResourceDictionary.ResourceKey themeKey, object context, out object value)
		{
			value = null;
			return (Application.Current?.Resources.TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false) ?? false)
				|| TryAssemblyResourceRetrieval(resourceKey, themeKey, context, out value)
				|| TrySystemResourceRetrieval(resourceKey, themeKey, out value);
		}

		/// <summary>
		/// Try to retrieve a resource statically, also returning the providing dictionary for theme resource pinning.
		/// </summary>
		internal static bool TryStaticRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value, out ResourceDictionary providingDictionary)
		{
			foreach (var source in CurrentScope.Sources)
			{

				var dictionary = (source.Target as FrameworkElement)?.TryGetResources()
					?? source.Target as ResourceDictionary;

				if (dictionary?.TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false) == true)
				{
					return true;
				}
			}

			var topLevel = TryTopLevelRetrieval(resourceKey, context, out value, out providingDictionary);
			if (!topLevel && _log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"Couldn't statically resolve resource {resourceKey.Key}");
			}
			return topLevel;
		}

		/// <summary>
		/// Try to retrieve a resource from top-level resources, also returning the providing dictionary.
		/// </summary>
		internal static bool TryTopLevelRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, object context, out object value, out ResourceDictionary providingDictionary)
		{
			value = null;
			providingDictionary = null;

			// See the 3-parameter overload above for the rationale of this priority order.
			// In short: secondary apps are queried *before* the host only when parseContext
			// identifies a non-default (secondary) ALC. Default-ALC parse contexts (and
			// host UI lookups) resolve against the host first, with a last-resort scan of
			// secondary apps for keys defined only there. Iterating secondary apps before
			// the host on every lookup would bleed secondary-app theme values into the
			// host's chrome.
			Application contextApp = null;
			if (Application.HasSecondaryApps
				&& context is XamlParseContext parseContext
				&& parseContext.AssemblyLoadContext is { } alc)
			{
				contextApp = Application.GetForAssemblyLoadContext(alc);

				if (contextApp is not null
					&& contextApp != Application.Current)
				{
					// See the 3-parameter overload for the rationale: bump a parse-context-resolved
					// stale Application to the live registration of the same Type.FullName so we
					// don't read stale resources from a hot-reloaded-out instance.
					var liveApp = Application.GetLatestSecondaryApplicationForType(contextApp.GetType().FullName);
					if (liveApp is not null && liveApp != contextApp)
					{
						contextApp = liveApp;
					}

					if (contextApp.Resources.TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false))
					{
						return true;
					}

					foreach (var secondaryApp in Application.EnumerateSecondaryApplications())
					{
						if (secondaryApp == contextApp)
						{
							continue;
						}

						if (secondaryApp.Resources.TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
				else
				{
					contextApp = null;
				}
			}

			if (Application.Current?.Resources.TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false) ?? false)
			{
				return true;
			}

			if (Application.HasSecondaryApps && contextApp is null)
			{
				foreach (var secondaryApp in Application.EnumerateSecondaryApplications())
				{
					if (secondaryApp.Resources.TryGetValue(resourceKey, out value, out providingDictionary, shouldCheckSystem: false))
					{
						return true;
					}
				}
			}

			// Assembly and system resources don't need pinning -- RefreshValue will fall back to TryTopLevelRetrieval
			if (TryAssemblyResourceRetrieval(resourceKey, context, out value))
			{
				providingDictionary = null;
				return true;
			}

			if (TrySystemResourceRetrieval(resourceKey, out value))
			{
				providingDictionary = null;
				return true;
			}

			return false;
		}

		// Phase 4 (D3, Mechanism 1) — theme-aware providing-dictionary top-level retrieval: thread the owner's
		// effective theme through every layer (app resources, assembly, system) and the StaticResource alias chain.
		internal static bool TryTopLevelRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, in SpecializedResourceDictionary.ResourceKey themeKey, object context, out object value, out ResourceDictionary providingDictionary)
		{
			value = null;
			providingDictionary = null;

			if (Application.Current?.Resources.TryGetValue(resourceKey, themeKey, out value, out providingDictionary, shouldCheckSystem: false) ?? false)
			{
				return true;
			}

			// Assembly and system resources don't need pinning -- RefreshValue will fall back to TryTopLevelRetrieval
			if (TryAssemblyResourceRetrieval(resourceKey, themeKey, context, out value))
			{
				providingDictionary = null;
				return true;
			}

			if (TrySystemResourceRetrieval(resourceKey, themeKey, out value))
			{
				providingDictionary = null;
				return true;
			}

			return false;
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
						// `kvp.Value` is a ResourceDictionary.ResourceInitializer that materializes
						// lazily into a ResourceDictionary. If the underlying Func targets code from
						// an unloaded non-default AssemblyLoadContext, the materialization returns
						// null and the `as` cast silently yields null, leading to an NRE below if
						// left unchecked. Skip such stale entries instead of crashing the measure
						// pass (a null rd here also signals that no further resource can be
						// resolved through this initializer, so `continue` is the correct behavior).
						if (kvp.Value is not ResourceDictionary rd)
						{
							continue;
						}

						if (rd.TryGetValue(resourceKey, out value, shouldCheckSystem: false))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		// Phase 4 (D3, Mechanism 1) — theme-aware assembly-resource retrieval: select the Light/Dark
		// sub-dictionary by the resolving owner's effective theme rather than the process-global active theme.
		private static bool TryAssemblyResourceRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, in SpecializedResourceDictionary.ResourceKey themeKey, object context, out object value)
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
						if (rd.TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false))
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

		// Phase 4 (D3, Mechanism 1) — theme-aware system-resource retrieval: select the Light/Dark
		// sub-dictionary of the master dictionary by the resolving owner's effective theme.
		internal static bool TrySystemResourceRetrieval(in SpecializedResourceDictionary.ResourceKey resourceKey, in SpecializedResourceDictionary.ResourceKey themeKey, out object value) => MasterDictionary.TryGetValue(resourceKey, themeKey, out value, shouldCheckSystem: false);

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
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		public static void RegisterResourceDictionaryBySource(string uri, XamlParseContext context, Func<ResourceDictionary> dictionary, string filePath)
		{
			// Route registrations from secondary ALCs to the ALC-scoped registry rather than
			// the global one. Without this, multiple assemblies sharing the same logical name
			// (e.g. Uno.UI.HotDesign.Client.Core loaded in both Default and a per-sample ALC)
			// race for the same key in `_registeredDictionariesByUri` and last-writer-wins —
			// causing the host's Application.Resources to materialize Style/Template instances
			// from a non-default ALC. When that ALC is later torn down, the Template's emitted
			// Type literals become orphaned and Style lookups for them fail (no template
			// applied → control sized 0×0). Prefer the calling-assembly's ALC; the dictionary
			// delegate's Method.DeclaringType is the most reliable identification when the
			// caller is the SG-emitted GlobalStaticResources.RegisterResourceDictionariesBySource.
			var dictAlc = AssemblyLoadContext.GetLoadContext(dictionary.Method.DeclaringType?.Assembly ?? Assembly.GetCallingAssembly());
			if (dictAlc is not null && dictAlc != AssemblyLoadContext.Default)
			{
				lock (_alcDictionariesLock)
				{
					if (!_registeredDictionariesByUriByAlc.TryGetValue(dictAlc, out var alcDict))
					{
						alcDict = new Dictionary<string, Func<ResourceDictionary>>(StringComparer.OrdinalIgnoreCase);
						_registeredDictionariesByUriByAlc.Add(dictAlc, alcDict);
					}
					alcDict[uri] = dictionary;
				}

				// Do NOT register in the global dict for non-default ALCs — that's the bug
				// this fix exists to prevent. We also skip the by-assembly and by-filepath
				// caches since those are global and would have the same collision problem.
				return;
			}

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
		/// Removes entries from global resource registrations whose Func delegate targets
		/// belong to non-default ALCs. Called during ALC teardown.
		/// </summary>
		internal static void ClearNonDefaultAlcRegistrations()
		{
			RemoveNonDefaultAlcEntries(_registeredDictionariesByUri);
			RemoveNonDefaultAlcEntries(_registeredDictionariesByFilepath);
		}

		private static void RemoveNonDefaultAlcEntries(Dictionary<string, Func<ResourceDictionary>> dictionary)
		{
			var keysToRemove = new List<string>();
			foreach (var kvp in dictionary)
			{
				if (kvp.Value is not { } func)
				{
					continue;
				}

				// Check both the target instance and the method's declaring type.
				// Static delegates (Target == null) can still reference a non-default ALC
				// via Method.DeclaringType.
				var assembly = func.Target?.GetType().Assembly
					?? func.Method.DeclaringType?.Assembly;

				if (assembly is not null)
				{
					var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
					if (alc is not null && alc != System.Runtime.Loader.AssemblyLoadContext.Default)
					{
						keysToRemove.Add(kvp.Key);
					}
				}
			}

			foreach (var key in keysToRemove)
			{
				dictionary.Remove(key);
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
