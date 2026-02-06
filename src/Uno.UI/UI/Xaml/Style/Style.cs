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
	public partial class Style
	{
		private static Logger _logger = typeof(Style).Log();

		public delegate Style StyleProviderHandler();

		private readonly static Dictionary<Type, StyleProviderHandler> _lookup = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _defaultStyleCache = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, StyleProviderHandler> _nativeLookup = new(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _nativeDefaultStyleCache = new(Uno.Core.Comparison.FastTypeComparer.Default);

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
			if (EnsureSetterMap().TryGetValue(dp, out var setter) && setter.TryGetSetterValue(out value, @do) && value != DependencyProperty.UnsetValue)
			{
				return true;
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

			var styleCache = useUWPDefaultStyles ? _defaultStyleCache
				: _nativeDefaultStyleCache;
			var lookup = useUWPDefaultStyles ? _lookup
				: _nativeLookup;

			if (!styleCache.TryGetValue(type, out Style? style))
			{
				if (lookup.TryGetValue(type, out var styleProvider))
				{
					style = styleProvider();

					styleCache[type] = style;

					lookup.Remove(type); // The lookup won't be used again now that the style itself is cached
				}
			}

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

			// Fallback: Try to find a default style in the type's assembly's Generic.xaml
			// This handles custom controls defined in the app assembly (issue #4424)
			if (style is null)
			{
				if (ResourceResolver.TryGetStyleFromGenericXaml(type, out var genericStyle))
				{
					style = genericStyle;
					styleCache[type] = style;
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
