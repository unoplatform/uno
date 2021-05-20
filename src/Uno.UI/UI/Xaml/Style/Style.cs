#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;

namespace Windows.UI.Xaml
{
	[Markup.ContentProperty(Name = "Setters")]
	public partial class Style
	{
		private static ILogger _logger = typeof(Style).Log();

		private delegate void ApplyToHandler(DependencyObject instance);

		public delegate Style StyleProviderHandler();

		private readonly static Dictionary<Type, StyleProviderHandler> _lookup = new Dictionary<Type, StyleProviderHandler>(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _defaultStyleCache = new Dictionary<Type, Style>(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, StyleProviderHandler> _nativeLookup = new Dictionary<Type, StyleProviderHandler>(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _nativeDefaultStyleCache = new Dictionary<Type, Style>(Uno.Core.Comparison.FastTypeComparer.Default);

		/// <summary>
		/// The xaml scope in force at the time the Style was created.
		/// </summary>
		private readonly XamlScope _xamlScope;

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

		internal void ApplyTo(DependencyObject o, DependencyPropertyValuePrecedences precedence)
		{
			if (o == null)
			{
				this.Log().Warn("Style.ApplyTo - Applied to null object - Skipping");
				return;
			}

			using (DependencyObjectExtensions.OverrideLocalPrecedence(o, precedence))
			{
				var flattenedSetters = CreateSetterMap();
#if !HAS_EXPENSIVE_TRYFINALLY
				try
#endif
				{
					ResourceResolver.PushNewScope(_xamlScope);
					foreach (var pair in flattenedSetters)
					{
						pair.Value(o);
					}

					// Check tree for resource binding values, since some Setters may have set ThemeResource-backed values
					(o as IDependencyObjectStoreProvider)!.Store.UpdateResourceBindings(isThemeChangedUpdate: false);
				}
#if !HAS_EXPENSIVE_TRYFINALLY
				finally
#endif
				{
					ResourceResolver.PopScope();
				}
			}
		}

		/// <summary>
		/// Clear properties from the current Style that are not set by the incoming Style. (The remaining properties will be overwritten
		/// when the incoming Style is applied.)
		/// </summary>
		internal void ClearInvalidProperties(DependencyObject dependencyObject, Style incomingStyle, DependencyPropertyValuePrecedences precedence)
		{
			var oldSetters = CreateSetterMap();
			var newSetters = incomingStyle?.CreateSetterMap();
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

		/// <summary>
		/// Creates a flattened list of setter methods for the whole hierarchy of
		/// styles.
		/// </summary>
		private IDictionary<object, ApplyToHandler> CreateSetterMap()
		{
			var map = new Dictionary<object, ApplyToHandler>();

			EnumerateSetters(this, map);

			return map;
		}

		/// <summary>
		/// Enumerates all the styles for the complete hierarchy.
		/// </summary>
		private void EnumerateSetters(Style style, Dictionary<object, ApplyToHandler> map)
		{
			if (style.BasedOn != null)
			{
				EnumerateSetters(style.BasedOn, map);
			}

			if (style.Setters != null)
			{
				foreach (var setter in style.Setters)
				{
					if (setter is Setter s)
					{
						if (s.Property == null)
						{
							throw new InvalidOperationException("Property must be set on Setter used in Style"); // TODO: We should also support Setter.Target inside Style https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter#remarks
						}
						map[s.Property] = setter.ApplyTo;
					}
					else if (setter is ICSharpPropertySetter propertySetter)
					{
						map[propertySetter.Property] = setter.ApplyTo;
					}
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterDefaultStyleForType(Type type, StyleProviderHandler styleProvider) => RegisterDefaultStyleForType(type, styleProvider, isNative: false);

		/// <summary>
		/// Register lazy default style provider for the nominated type.
		/// </summary>
		/// <param name="type">The type to which the style applies</param>
		/// <param name="styleProvider">Function which generates the style. This will be called once when first used, then cached.</param>
		/// <param name="isNative">True if it is the native default style, false if it is the UWP default style.</param>
		/// <remarks>
		/// This is public for backward compatibility, but isn't called from Xaml-generated code any longer. 
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void RegisterDefaultStyleForType(Type type, StyleProviderHandler styleProvider, bool isNative)
		{
			if (isNative)
			{
				_nativeLookup[type] = styleProvider;
			}
			else
			{
				_lookup[type] = styleProvider;
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
			RegisterDefaultStyleForType(type, ProvideStyle, isNative);

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
		internal static Style? GetDefaultStyleForType(Type type) => GetDefaultStyleForType(type, ShouldUseUWPDefaultStyle(type));

		private static Style? GetDefaultStyleForType(Type type, bool useUWPDefaultStyles)
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

			if (style == null && !useUWPDefaultStyles)
			{

				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebug($"No native style found for type {type}, falling back on UWP style");
				}

				// If no native style found, fall back on UWP style
				style = GetDefaultStyleForType(type, useUWPDefaultStyles: true);
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
