#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Uno.Foundation.Logging;
using Uno.UI;
using Microsoft.UI.Xaml.Data;

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

		internal void ApplyTo(DependencyObject o, DependencyPropertyValuePrecedences precedence)
		{
			if (o == null)
			{
				this.Log().Warn("Style.ApplyTo - Applied to null object - Skipping");
				return;
			}

			IDisposable? localPrecedenceDisposable = null;

			EnsureSetterMap();

			try
			{
				ResourceResolver.PushNewScope(_xamlScope);
				localPrecedenceDisposable = DependencyObjectExtensions.OverrideLocalPrecedence(o, precedence);

				if (_flattenedSetters != null)
				{
					for (var i = 0; i < _flattenedSetters.Length; i++)
					{
						try
						{
							_flattenedSetters[i].ApplyTo(o);
						}
						catch (Exception)
						{
							if (this.Log().IsEnabled(LogLevel.Warning))
							{
								this.Log().LogWarning($"An exception occurred while applying style setter.");
							}

							// Ideally, this should be an empty catch, keeping parity equivalent to WinUI's IGNOREHR in https://github.com/microsoft/microsoft-ui-xaml/blob/93742a178db8f625ba9299f62c21f656e0b195ad/dxaml/xcp/core/core/elements/framework.cpp#L790
							// However, Later when default style is applied (using ImplicitStyle precedence), we don't observe that value in WinUI.
							// As a workaround, we force-set the current value to Local precedence.
							// Note: This workaround can be removed as long as When_Unbound_FullFlyout is passing.
							// Maybe we are applying default styles later than we should?
							// For now, this is very uncommon code path for already bad code that throws.
							if (_flattenedSetters[i] is Setter { Property: { } dp })
							{
								var value = o.GetValue(dp);
								this.Log().LogWarning($"Force-setting local value to '{value}'.");
								o.SetValue(dp, value);
							}
						}
					}
				}

				localPrecedenceDisposable?.Dispose();
				localPrecedenceDisposable = null;

				// Check tree for resource binding values, since some Setters may have set ThemeResource-backed values
				(o as IDependencyObjectStoreProvider)!.Store.UpdateResourceBindings(ResourceUpdateReason.ResolvedOnLoading);
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
