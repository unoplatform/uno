using System;
using System.Collections.Generic;
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

		public Type TargetType { get; set; }

		public Style BasedOn { get; set; }

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
						map[s.Property] = setter.ApplyTo;
					}
					else if (setter is ICSharpPropertySetter propertySetter)
					{
						map[propertySetter.Property] = setter.ApplyTo;
					}
				}
			}
		}

		public static void RegisterDefaultStyleForType(Type type, StyleProviderHandler styleProvider) => RegisterDefaultStyleForType(type, styleProvider, isNative: false);

		/// <summary>
		/// Register lazy default style provider for the nominated type.
		/// </summary>
		/// <param name="type">The type to which the style applies</param>
		/// <param name="styleProvider">Function which generates the style. This will be called once when first used, then cached.</param>
		/// <param name="isNative">True if is is the native default style, false if it is the UWP default style.</param>
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
		/// Returns the default Style for given type. 
		/// </summary>
		internal static Style GetDefaultStyleForType(Type type) => GetDefaultStyleForType(type, ShouldUseUWPDefaultStyle(type));

		private static Style GetDefaultStyleForType(Type type, bool useUWPDefaultStyles)
		{
			if (type == null)
			{
				return null;
			}

			var styleCache = useUWPDefaultStyles ? _defaultStyleCache
				: _nativeDefaultStyleCache;
			var lookup = useUWPDefaultStyles ? _lookup
				: _nativeLookup;

			if (!styleCache.TryGetValue(type, out var style))
			{
				if (lookup.TryGetValue(type, out var styleProvider))
				{
					style = styleProvider();

					styleCache[type] = style;
				}
			}

			if (style == null && !useUWPDefaultStyles)
			{

				if (typeof(Style).Log().IsEnabled(LogLevel.Debug))
				{
						typeof(Style).Log().LogDebug($"No native style found for type {type}, falling back on UWP style");
				}

				// If no native style found, fall back on UWP style
				style = GetDefaultStyleForType(type, useUWPDefaultStyles: true);
			}

			if (typeof(Style).Log().IsEnabled(LogLevel.Debug))
			{
				if (style != null)
				{
					typeof(Style).Log().LogDebug($"Returning {(useUWPDefaultStyles ? "UWP" : "native")} style {style} for type {type}");
				}
				else
				{
					typeof(Style).Log().LogDebug($"No {(useUWPDefaultStyles ? "UWP" : "native")} style found for type {type}");
				}
			}

			return style;
		}

		private static bool ShouldUseUWPDefaultStyle(Type type)
		{
			if (type != null && FeatureConfiguration.Style.UseUWPDefaultStylesOverride.TryGetValue(type, out var value))
			{
				return value;
			}

			return FeatureConfiguration.Style.UseUWPDefaultStyles;
		}
	}
}
