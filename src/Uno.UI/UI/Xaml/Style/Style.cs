#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Microsoft.UI.Xaml
{
	[Markup.ContentProperty(Name = "Setters")]
	public partial class Style : IMultiParentShareableDependencyObject
	{
		private static Logger _logger = typeof(Style).Log();

		public delegate Style StyleProviderHandler();

		private readonly static Dictionary<Type, StyleProviderHandler> _lookup = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _defaultStyleCache = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, StyleProviderHandler> _nativeLookup = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _nativeDefaultStyleCache = new(Uno.Core.Comparison.FastTypeComparer.Default);

		/// <summary>
		/// Performance-optimized variants of the default styles, only used when
		/// <see cref="FeatureConfiguration.Style.UseDefaultStyleOptimizations"/> is enabled. Types
		/// without an optimized variant fall back to <see cref="_lookup"/>.
		/// </summary>
		private readonly static Dictionary<Type, StyleProviderHandler> _optimizedLookup = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _optimizedDefaultStyleCache = new(Uno.Core.Comparison.FastTypeComparer.Default);

		/// <summary>
		/// Removes entries from the style caches whose Type key belongs to a non-default ALC.
		/// These caches rebuild on demand, so the sweep may safely cover ALL non-default contexts.
		/// User configuration (<see cref="FeatureConfiguration.Style.UseUWPDefaultStylesOverride"/>)
		/// is NOT part of this group — it never rebuilds; see
		/// <see cref="RemoveAlcScopedUserStyleOverrides"/>.
		/// </summary>
		internal static void ClearCachesForNonDefaultAlc()
		{
			var removed = Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_lookup)
				+ Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_defaultStyleCache)
				+ Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_nativeLookup)
				+ Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_nativeDefaultStyleCache)
				+ Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_optimizedLookup)
				+ Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_optimizedDefaultStyleCache);

			if (removed > 0 && _logger.IsEnabled(LogLevel.Debug))
			{
				_logger.Debug($"[ALC-CLEANUP] Style caches: removed {removed} non-default-ALC entrie(s).");
			}
		}

		/// <summary>
		/// Removes <see cref="FeatureConfiguration.Style.UseUWPDefaultStylesOverride"/> entries whose
		/// control <see cref="Type"/> key is owned by the dying ALC. This dictionary is USER
		/// CONFIGURATION (written via <c>SetUWPDefaultStylesOverride</c> and never rebuilt), so unlike
		/// the rebuild-on-demand caches above it must never be swept for all non-default contexts —
		/// that would silently delete a live sibling secondary app's (or session add-in's) override.
		/// A previewed app configuring overrides for its own control types would otherwise pin those
		/// types — and its collectible context — for the process lifetime.
		/// </summary>
		internal static void RemoveAlcScopedUserStyleOverrides(global::System.Runtime.Loader.AssemblyLoadContext? dyingAlc)
		{
			var removed = Uno.UI.Helpers.AlcCacheSweep.RemoveUnloadScopedEntries(FeatureConfiguration.Style.UseUWPDefaultStylesOverride, dyingAlc);

			if (removed > 0 && _logger.IsEnabled(LogLevel.Debug))
			{
				_logger.Debug($"[ALC-CLEANUP] UseUWPDefaultStylesOverride: removed {removed} entrie(s) owned by dying ALC '{dyingAlc?.Name ?? "unload-initiated"}'.");
			}
		}

		/// <summary>
		/// Removes EVERY non-default-ALC <see cref="FeatureConfiguration.Style.UseUWPDefaultStylesOverride"/>
		/// entry. DESTRUCTIVE and never rebuilt, so this is reserved for a genuine global shutdown
		/// (<c>Application.CleanupAllSecondaryAlcCaches</c>), where every secondary app is going away and
		/// no live sibling can be harmed. It keeps the user-override sweep consistent with the other
		/// destructive global-shutdown sweeps (ResourceLoader lookup assemblies, CompositionTarget
		/// handlers), which also go all-non-default there — otherwise a scoped-only override sweep would
		/// leave keys that pin the very ALCs the shutdown exists to free. For a single dying ALC, use the
		/// scoped <see cref="RemoveAlcScopedUserStyleOverrides"/> instead.
		/// </summary>
		internal static void RemoveAllNonDefaultAlcUserStyleOverrides()
		{
			var removed = Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(FeatureConfiguration.Style.UseUWPDefaultStylesOverride);

			if (removed > 0 && _logger.IsEnabled(LogLevel.Debug))
			{
				_logger.Debug($"[ALC-CLEANUP] UseUWPDefaultStylesOverride: removed {removed} entrie(s) from all non-default ALCs (global shutdown).");
			}
		}

		/// <summary>
		/// The xaml scope in force at the time the Style was created.
		/// </summary>
		private readonly XamlScope _xamlScope;
		private Dictionary<object, SetterBase>? _settersMap;
		private SetterBase[]? _flattenedSetters;

		public Style()
		{
			_xamlScope = ResourceResolver.CurrentScope;
		}

		public Style(Type targetType) : this()
		{
			if (targetType == null)
			{
				throw new ArgumentNullException(nameof(targetType));
			}

			TargetType = targetType;
		}

		public Type? TargetType { get; set; }

		public Style? BasedOn { get; set; }

		public SetterBaseCollection Setters { get; } = new SetterBaseCollection();

		public bool IsSealed
		{
			get; private set;
		}

		public void Seal()
		{
			IsSealed = true;
			Setters.Seal();

			BasedOn?.Seal();
		}

		private bool TryGetAdjustedSetter(DependencyPropertyValuePrecedences baseValueSource, DependencyObject dependencyObject, SetterBase originalSetter, [NotNullWhen(true)] out SetterBase? adjustedSetter)
		{
			if (originalSetter is not Setter { Property: { } property })
			{
				adjustedSetter = null;
				return false;
			}

			// Replicates CDependencyObject::InvalidateProperty from WinUI
			if (property == Control.TemplateProperty)
			{
				var oldBaseValueSource = dependencyObject.GetBaseValueSource(property);
				// Uno docs: In WinUI code, this condition is `baseValueSource < oldBaseValueSource`
				// In Uno, we use >= instead because our precedence enum is opposite order of WinUI's one.
				if (baseValueSource >= oldBaseValueSource)
				{
					adjustedSetter = null;
					return false;
				}
			}

			// On WinUI, when default style is applied and there is an explicit
			// style that contains a setter for the same DP, the value from that explicit style is used.
			// Note that having two different precedences isn't sufficient to handle this case.
			// The setter application could be throwing an exception, and in this case we don't
			// want the value from the default style to take effect.
			// This bit of code isn't ported from WinUI, but is the equivalent of the following call chain:
			// OnStyleChanged -> InvalidateProperty -> UpdateEffectiveValue -> EvaluateEffectiveValue -> EvaluateBaseValue -> GetValueFromSetter
			// In DependencyObject::EvaluateBaseValue (DependencyObject.cpp file), the value is updated to that returned from GetValueFromStyle
			// Then, baseValueSource is updated from BaseValueSourceBuiltInStyle to BaseValueSourceStyle
			// The OverrideLocalPrecedence call below is the equivalent of the baseValueSource update.
			if (baseValueSource == DependencyPropertyValuePrecedences.ImplicitStyle &&
				dependencyObject is FrameworkElement fe &&
				fe.GetActiveStyle() is { } activeStyle &&
				// Make sure to only consider active style if it was explicit.
				fe.Style == activeStyle &&
				activeStyle != this &&
				activeStyle.EnsureSetterMap().TryGetValue(property, out var setter))
			{
				adjustedSetter = setter;
				return true;
			}

			adjustedSetter = null;
			return false;
		}

		/// <summary>
		/// Determines whether a setter's value can be left unmaterialized because a higher precedence already
		/// provides the base value of the target property, in which case applying the setter would be discarded
		/// by the property store.
		/// </summary>
		/// <remarks>
		/// <para>
		/// MUX Reference: <c>OptimizedStyle::AddDeferredSetterInfo</c> / <c>OptimizedStyle::EnsureValueRealized</c>.
		/// WinUI keeps a setter value unrealized until the layer it belongs to actually provides the effective value,
		/// so that a built-in style whose <c>Control.Template</c> is entirely replaced by an app style never pays for
		/// building that template. Uno's store keeps a single base value slot and re-queries the winning style through
		/// <c>DependencyObjectStore.ReevaluateBaseValue</c> whenever the winning precedence is cleared, so skipping
		/// the application here is observationally equivalent.
		/// </para>
		/// <para>
		/// Setters carrying a resource key are never skipped: applying them registers the theme (and hot reload)
		/// binding that keeps the value refreshed at that precedence, which is a subscription rather than a value.
		/// </para>
		/// </remarks>
		private static bool TryDeferSetter(DependencyObject o, DependencyPropertyValuePrecedences precedence, SetterBase setterBase)
		{
			if (!FeatureConfiguration.Style.DeferOverriddenSetterValues ||
				setterBase is not Setter { Property: { } property } setter ||
				setter.ThemeResourceKey.HasValue)
			{
				return false;
			}

			var store = ((IDependencyObjectStoreProvider)o).Store;

			if (store.GetBaseValueSourcePrecedence(property) >= precedence)
			{
				return false;
			}

			// Mirror the cleanup that applying the setter at this precedence would have performed, so a binding
			// registered by a previously applied style cannot resurface at the skipped precedence.
			store.ClearResourceBindingsForSkippedSetter(property, precedence);
			return true;
		}

		internal void ApplyTo(DependencyObject o, DependencyPropertyValuePrecedences precedence)
		{
			if (o == null)
			{
				this.Log().Warn("Style.ApplyTo - Applied to null object - Skipping");
				return;
			}

			Debug.Assert(precedence is DependencyPropertyValuePrecedences.ImplicitStyle or DependencyPropertyValuePrecedences.ExplicitStyle);

			IDisposable? localPrecedenceDisposable = null;

			EnsureSetterMap();

			try
			{
				/// <remarks>
				/// This method runs in a separate method in order to workaround for the following issue:
				/// https://github.com/dotnet/runtime/issues/111281
				/// which prevents AOT on WebAssembly when try/catch/finally are found in the same method.
				/// </remarks>
				IDisposable? InnerApplyTo(DependencyObject o, DependencyPropertyValuePrecedences precedence)
				{
					IDisposable? localPrecedenceDisposable;
					ResourceResolver.PushNewScope(_xamlScope);
					localPrecedenceDisposable = DependencyObjectExtensions.OverrideLocalPrecedence(o, precedence);

					if (_flattenedSetters != null)
					{
						for (var i = 0; i < _flattenedSetters.Length; i++)
						{
							try
							{
								if (TryDeferSetter(o, precedence, _flattenedSetters[i]))
								{
									continue;
								}

								if (TryGetAdjustedSetter(precedence, o, _flattenedSetters[i], out var adjustedSetter))
								{
									using (o.OverrideLocalPrecedence(DependencyPropertyValuePrecedences.ExplicitStyle))
									{
										adjustedSetter.ApplyTo(o);
									}
								}
								else
								{
									_flattenedSetters[i].ApplyTo(o);
								}
							}
							catch (Exception ex)
							{
								// This empty catch is to keep parity with WinUI's IGNOREHR in
								// https://github.com/microsoft/microsoft-ui-xaml/blob/93742a178db8f625ba9299f62c21f656e0b195ad/dxaml/xcp/core/core/elements/framework.cpp#L790
								if (this.Log().IsEnabled(LogLevel.Debug))
								{
									this.Log().LogDebug($"An exception occurred while applying style setter. {ex}");
								}
							}
						}
					}

					localPrecedenceDisposable?.Dispose();
					localPrecedenceDisposable = null;

					// Check tree for resource binding values, since some Setters may have set ThemeResource-backed values
					(o as IDependencyObjectStoreProvider)!.Store.UpdateResourceBindings(ResourceUpdateReason.ResolvedOnLoading);
					return localPrecedenceDisposable;
				}

				localPrecedenceDisposable = InnerApplyTo(o, precedence);
			}
			finally
			{
				localPrecedenceDisposable?.Dispose();
				ResourceResolver.PopScope();
			}
		}

		/// <summary>
		/// Clear properties from the current Style that are not set by the incoming Style. (The remaining properties will be overwritten
		/// when the incoming Style is applied.)
		/// </summary>
		internal void ClearInvalidProperties(DependencyObject dependencyObject, Style incomingStyle, DependencyPropertyValuePrecedences precedence)
		{
			var oldSetters = EnsureSetterMap();
			var newSetters = incomingStyle?.EnsureSetterMap();
			foreach (var kvp in oldSetters)
			{
				if (kvp.Key is DependencyProperty dp)
				{
					if (newSetters == null || !newSetters.ContainsKey(dp))
					{
						DependencyObjectExtensions.ClearValue(dependencyObject, dp, precedence);
					}
				}
			}
		}

		// There shouldn't be a DependencyObject parameter. This can be removed in Uno 6 once we remove `Setter<T>`
		internal bool TryGetPropertyValue(DependencyProperty dp, out object? value, DependencyObject @do)
		{
			if (EnsureSetterMap().TryGetValue(dp, out var setter))
			{
				// The setter may resolve resources, which must happen in the scope the Style was declared in,
				// exactly as it would have during ApplyTo. This matters for deferred setters, whose value is
				// only built when this method is reached through DependencyObjectStore.ReevaluateBaseValue.
				ResourceResolver.PushNewScope(_xamlScope);
				try
				{
					if (setter.TryGetSetterValue(out value, @do) && value != DependencyProperty.UnsetValue)
					{
						return true;
					}
				}
				finally
				{
					ResourceResolver.PopScope();
				}
			}

			value = null;
			return false;
		}

		/// <summary>
		/// Creates a flattened list of setter methods for the whole hierarchy of
		/// styles.
		/// </summary>
		private IDictionary<object, SetterBase> EnsureSetterMap()
		{
			if (_settersMap == null)
			{
				_settersMap = new Dictionary<object, SetterBase>();

				EnumerateSetters(this, _settersMap);

				_flattenedSetters = _settersMap.Values.ToArray();
			}

			return _settersMap;
		}

		/// <summary>
		/// Enumerates all the styles for the complete hierarchy.
		/// </summary>
		private static void EnumerateSetters(Style style, Dictionary<object, SetterBase> map)
		{
			style.Seal();

			if (style.BasedOn != null)
			{
				EnumerateSetters(style.BasedOn, map);
			}

			if (style.Setters != null)
			{
				for (var i = 0; i < style.Setters.Count; i++)
				{
					var setter = style.Setters[i];

					if (setter is Setter s)
					{
						if (s.Property == null)
						{
							throw new InvalidOperationException("Property must be set on Setter used in Style"); // TODO: We should also support Setter.Target inside Style https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter#remarks
						}
						map[s.Property] = setter;
					}
					else if (setter is ICSharpPropertySetter propertySetter)
					{
						map[propertySetter.Property] = setter;
					}
				}
			}
		}

		/// <summary>
		///  Register lazy default style provider for the nominated type.
		/// </summary>
		/// <param name="type">The type to which the style applies</param>
		/// <param name="dictionaryProvider">Provides the dictionary in which the style is defined.</param>
		/// <param name="isNative">True if it is the native default style, false if it is the UWP default style.</param>
		/// <remarks>This is an Uno-specific method, normally only called from Xaml-generated code.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterDefaultStyleForType(Type type, IXamlResourceDictionaryProvider dictionaryProvider, bool isNative)
		{
			if (isNative)
			{
				_nativeLookup[type] = ProvideStyle;
			}
			else
			{
				_lookup[type] = ProvideStyle;
			}

			Style ProvideStyle()
			{
				var styleSource = dictionaryProvider.GetResourceDictionary();
				if (styleSource.TryGetValue(type, out var style, shouldCheckSystem: false))
				{
					return (Style)style;
				}

				throw new InvalidOperationException($"{styleSource} was registered as style provider for {type} but doesn't contain matching style.");
			}
		}

		/// <summary>
		/// Register a lazy performance-optimized default style provider for the nominated type.
		/// </summary>
		/// <param name="type">The type to which the style applies</param>
		/// <param name="dictionaryProvider">Provides the dictionary in which the style is defined.</param>
		/// <remarks>
		/// This is an Uno-specific method, normally only called from Xaml-generated code for styles
		/// marked with <c>IsOptimizedStyle="True"</c>. The registered style is only used when
		/// <see cref="FeatureConfiguration.Style.UseDefaultStyleOptimizations"/> is enabled, and it never
		/// replaces the default registration made by <see cref="RegisterDefaultStyleForType"/>.
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterOptimizedDefaultStyleForType(Type type, IXamlResourceDictionaryProvider dictionaryProvider)
		{
			_optimizedLookup[type] = ProvideStyle;

			Style ProvideStyle()
			{
				var styleSource = dictionaryProvider.GetResourceDictionary();
				if (styleSource.TryGetValue(type, out var style, shouldCheckSystem: false))
				{
					return (Style)style;
				}

				throw new InvalidOperationException($"{styleSource} was registered as optimized style provider for {type} but doesn't contain matching style.");
			}
		}

		/// <summary>
		/// Returns the default Style for given type.
		/// </summary>
		internal static Style? GetDefaultStyleForType(Type type) => GetDefaultStyleForType(type, null, ShouldUseUWPDefaultStyle(type));

		internal static Style? GetDefaultStyleForInstance(FrameworkElement instance, Type type) => GetDefaultStyleForType(type, instance, ShouldUseUWPDefaultStyle(type));

		private static Style? GetDefaultStyleForType(Type type, FrameworkElement? instance, bool useUWPDefaultStyles)
		{
			if (type == null)
			{
				return null;
			}

			Style? style = null;

			if (useUWPDefaultStyles && FeatureConfiguration.Style.UseDefaultStyleOptimizations)
			{
				// Optimized styles are only defined for a subset of the controls, a miss
				// falls back to the standard style below.
				style = GetStyleFromChannel(type, _optimizedDefaultStyleCache, _optimizedLookup);
			}

			style ??= useUWPDefaultStyles
				? GetStyleFromChannel(type, _defaultStyleCache, _lookup)
				: GetStyleFromChannel(type, _nativeDefaultStyleCache, _nativeLookup);

			if (style is null && instance is Control { DefaultStyleResourceUri: { } defaultStyleResourceUri })
			{
				if (ResourceResolver.TryRetrieveDictionaryForSource(defaultStyleResourceUri, out var dictionary))
				{
					if (dictionary.TryGetValue(type, out var resolvedItem, shouldCheckSystem: false) && resolvedItem is Style defaultStyle)
					{
						style = defaultStyle;
					}
				}
			}

			if (style == null && !useUWPDefaultStyles)
			{
				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebug($"No native style found for type {type}, falling back on UWP style");
				}

				// If no native style found, fall back on UWP style
				style = GetDefaultStyleForType(type, instance, useUWPDefaultStyles: true);
			}

			if (_logger.IsEnabled(LogLevel.Debug))
			{
				if (style != null)
				{
					_logger.LogDebug($"Returning {(useUWPDefaultStyles ? "UWP" : "native")} style {style} for type {type}");
				}
				else
				{
					_logger.LogDebug($"No {(useUWPDefaultStyles ? "UWP" : "native")} style found for type {type}");
				}
			}

			return style;
		}

		private static Style? GetStyleFromChannel(Type type, Dictionary<Type, Style> styleCache, Dictionary<Type, StyleProviderHandler> lookup)
		{
			if (!styleCache.TryGetValue(type, out Style? style))
			{
				if (lookup.TryGetValue(type, out var styleProvider))
				{
					style = styleProvider();

					styleCache[type] = style;

					lookup.Remove(type); // The lookup won't be used again now that the style itself is cached
				}
			}

			return style;
		}

		internal static bool ShouldUseUWPDefaultStyle(Type type)
		{
			if (type != null && FeatureConfiguration.Style.UseUWPDefaultStylesOverride.TryGetValue(type, out var value))
			{
				return value;
			}

			return FeatureConfiguration.Style.UseUWPDefaultStyles;
		}
	}
}
