using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	[Markup.ContentProperty(Name = "Setters")] 
	public partial class Style
	{
        private delegate void ApplyToHandler(DependencyObject instance);

        public delegate Style StyleProviderHandler();

		private readonly static Dictionary<Type, StyleProviderHandler> _lookup = new Dictionary<Type, StyleProviderHandler>(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<Type, Style> _defaultStyleCache = new Dictionary<Type, Style>(Uno.Core.Comparison.FastTypeComparer.Default);

		/// <summary>
		/// Default style precedence to apply when using the Style property.
		/// </summary>
		internal static DependencyPropertyValuePrecedences DefaultStylePrecedence
			 => DependencyPropertyValuePrecedences.ImplicitStyle;

		// Note: These conditionals are placed for legacy reasons, when Uno.UI xaml-style code
		// was considered. This is approach has been abandoned since then but style is still referenced 
		// by windows platforms. Once it's removed we can safely remove these conditionals.
		internal DependencyPropertyValuePrecedences Precedence { get; }

		public Style()
		{
			Precedence = DependencyPropertyValuePrecedences.ExplicitStyle;
		}

		public Style(Type targetType)
		{
			if (targetType == null)
			{
				throw new ArgumentNullException(nameof(targetType));
			}

			TargetType = targetType;

			Precedence = DependencyPropertyValuePrecedences.ExplicitStyle;
		}

		public Style(Type targetType, Style basedOn)
			: this(targetType, basedOn, false)
		{
			if (targetType == null)
			{
				throw new ArgumentNullException(nameof(targetType));
			}

			if (basedOn == null)
			{
				throw new ArgumentNullException(nameof(basedOn));
			}

			TargetType = targetType;
			BasedOn = basedOn;

			Precedence = DependencyPropertyValuePrecedences.ExplicitStyle;
		}

		internal Style(Type targetType, Style basedOn, bool defaultStyle)
		{
			if (targetType == null)
			{
				throw new ArgumentNullException(nameof(targetType));
			}

			if (basedOn == null)
			{
				throw new ArgumentNullException(nameof(basedOn));
			}

			TargetType = targetType;
			BasedOn = basedOn;

			// The style is specified as Implicit until locally defined styles are correctly supported.
			Precedence = defaultStyle ? DefaultStylePrecedence : DependencyPropertyValuePrecedences.ExplicitStyle;
		}

		public Type TargetType { get; set; }

		public Style BasedOn { get; set; }

		public SetterBaseCollection Setters { get; } = new SetterBaseCollection();

		public void ApplyTo(DependencyObject o)
		{
			if (o == null)
			{
				this.Log().Warn("Style.ApplyTo - Applied to null object - Skipping");
				return;
			}

			using (DependencyObjectExtensions.OverrideLocalPrecedence(o, Precedence))
			{
				var flattenedSetters = CreateSetterMap();

				foreach (var pair in flattenedSetters)
				{
					pair.Value(o);
				}
			}
		}

		/// <summary>
		/// Clears the current style from the specified dependency object.
		/// </summary>
		/// <param name="dependencyObject">The target dependency object</param>
		/// <remarks>
		/// This method should be included in the calls to update of the Style
		/// property. Automatic style removal is not implemented for now because
		/// of breaking changes.
		/// </remarks>
		internal void ClearStyle(DependencyObject dependencyObject)
		{
			var type = dependencyObject.GetType();

			foreach (var setter in CreateSetterMap())
			{
				if (setter.Key is DependencyProperty dp)
				{
					DependencyObjectExtensions.ClearValue(dependencyObject, dp, Precedence);
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
		/// Lazily enumerates all the styles for the complete hierarchy.
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
					if(setter is Setter s)
					{
						map[s.Property] = setter.ApplyTo;
					}
					else if(setter is ICSharpPropertySetter propertySetter)
					{
						map[propertySetter.Property] = setter.ApplyTo;
					}
				}
			}
		}

        public static void RegisterDefaultStyleForType(Type type, Style style)
        {
            _lookup[type] = () => style;
        }

        public static void RegisterDefaultStyleForType(Type type, StyleProviderHandler styleProvider)
        {
            _lookup[type] = styleProvider;
        }

		public static Style DefaultStyleForType(Type type)
		{
			if (!_defaultStyleCache.TryGetValue(type, out var style))
			{
				if (_lookup.TryGetValue(type, out var styleProvider))
				{
					style = new Style(targetType: type, basedOn: styleProvider(), defaultStyle: true);
				}
				else
				{
					style = new Style(targetType: type);
				}

				_defaultStyleCache[type] = style;
			}

			return style;
		}
	}

	public class Style<T> : Style
	{
		public Style()
			: base(typeof(T))
		{

		}
	}
}
