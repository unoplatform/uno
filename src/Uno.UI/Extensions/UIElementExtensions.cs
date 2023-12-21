#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Globalization;

namespace Uno.UI.Extensions
{
	public static partial class UIElementExtensions
	{
		/// <summary>
		/// Get a display name for the element for debug purposes
		/// </summary>
		internal static string GetDebugName(this object? elt)
			=> elt switch
			{
				null => "--null--",
#if __WASM__
				FrameworkElement fwElt when !fwElt.Name.IsNullOrEmpty() => $"{fwElt.Name}-{fwElt.HtmlId}",
				UIElement uiElt => $"{elt.GetType().Name}-{uiElt.HtmlId}",
#else
				FrameworkElement fwElt when !fwElt.Name.IsNullOrEmpty() => $"{fwElt.Name}-{elt.GetHashCode():X8}",
#endif
				_ => $"{elt.GetType().Name}-{elt.GetHashCode():X8}",
			};

		internal enum IndentationFormat
		{
			/// <summary>
			/// Use tabs to indent log, e.g.
			/// ```
			/// [VisualRoot]
			/// \t[Grid]
			/// \t\t[TextBlock]
			/// ```
			/// </summary>
			Tabs,

			/// <summary>
			/// Use tabs to indent log, e.g.
			/// ```
			/// [VisualRoot]
			///   [Grid]
			///     [TextBlock]
			/// ```
			/// </summary>
			Spaces,

			/// <summary>
			/// Draw ASCII columns to indent log, e.g.
			/// ```
			/// [VisualRoot]
			/// | [Grid]
			/// | | [TextBlock]
			/// ```
			/// </summary>
			Columns,

			/// <summary>
			/// Use the depth numbers of each level to indent log, e.g.
			/// ```
			/// [VisualRoot]
			/// -1> [Grid]
			/// -1>-2> [TextBlock]
			/// ```
			/// </summary>
			Numbered,

			/// <summary>
			/// Draw ASCII columns with depth number to indent log, e.g.
			/// ```
			/// 01> [VisualRoot]
			///     | 02> [Grid]
			///     |     | 03> [TextBlock]
			/// ```
			/// </summary>
			NumberedColumns,

			/// <summary>
			/// Use spaces with only the current level number to indent the log, e.g.
			/// ```
			/// 01> [VisualRoot]
			///   02> [Grid]
			///     03> [TextBlock]
			/// ```
			/// </summary>
			NumberedSpaces,
		}

		private static readonly IndentationFormat _defaultIndentationFormat = IndentationFormat.NumberedSpaces;

		/// <summary>
		/// The name (<see cref="GetDebugName"/>) indented (<see cref="GetDebugIndent(object?, IndentationFormat, bool)"/>) for log usage.
		/// </summary>
		internal static string GetDebugIdentifier(this object? elt)
			=> GetDebugIdentifier(elt, _defaultIndentationFormat);

		/// <summary>
		/// The name (<see cref="GetDebugName"/>) indented (<see cref="GetDebugIndent(object?, IndentationFormat, bool)"/>) for log usage.
		/// </summary>
		internal static string GetDebugIdentifier(this object? elt, IndentationFormat format)
			=> $"{elt.GetDebugIndent(format)} [{elt.GetDebugName()}]";

		/// <summary>
		/// Indentation log prefix built from the depth (<see cref="GetDebugDepth"/>) of the element for tree-view like logs.
		/// </summary>
		/// <param name="subLine">
		/// Indicates that the indent is for a sub-line log of the given element,
		/// so it has to be indented one extra level but without numbering it.
		/// </param>
		internal static string GetDebugIndent(this object? elt, bool subLine = false)
			=> GetDebugIndent(elt, _defaultIndentationFormat, subLine);

		/// <summary>
		/// Indentation log prefix built from the depth (<see cref="GetDebugDepth"/>) of the element for tree-view like logs.
		/// </summary>
		/// <param name="subLine">
		/// Indicates that the indent is for a sub-line log of the given element,
		/// so it has to be indented one extra level but without numbering it.
		/// </param>
		internal static string GetDebugIndent(this object? elt, IndentationFormat format, bool subLine = false)
		{
			var depth = elt.GetDebugDepth();
			return format switch
			{
				IndentationFormat.Numbered when depth < 0 => "-?>",
				IndentationFormat.NumberedColumns when depth < 0 => "?>",
				_ when depth < 0 => "",

				IndentationFormat.Columns => GetColumnsIndentation(depth),
				IndentationFormat.Numbered => GetNumberedIndentation(depth, subLine),
				IndentationFormat.NumberedColumns => GetNumberedColumnIndentation(depth, subLine),
				IndentationFormat.Tabs when subLine => new string('\t', depth + 1),
				IndentationFormat.Tabs => new string('\t', depth),
				IndentationFormat.Spaces when subLine => new string(' ', (depth + 1) * 2),
				IndentationFormat.Spaces => new string(' ', depth * 2),

				// default to NumberedSpaces
				_ when subLine => new string(' ', (depth + 1) * 2 + 1),
				_ => $"{new string(' ', depth * 2)} {depth:D2}>",
			};

			static string GetColumnsIndentation(int depth)
			{
				var sb = new StringBuilder(depth * 2);
				for (var i = 0; i < depth; i++)
				{
					sb.Append('|');
					sb.Append(' ');
				}

				return sb.ToString();
			}

			static string GetNumberedIndentation(int depth, bool subLine)
			{
				var sb = new StringBuilder(depth * 4);
				for (var i = 0; i < depth; i++)
				{
					sb.Append('-');
					sb.Append(i);
					sb.Append('>');
				}

				if (subLine)
				{
					sb.Append(' ', depth.ToString(CultureInfo.InvariantCulture).Length + 2);
				}

				return sb.ToString();
			}

			static string GetNumberedColumnIndentation(int depth, bool subLine)
			{
				var sb = new StringBuilder(depth * 4);
				var count = subLine ? depth : depth - 1;
				for (var i = 0; i < count; i++)
				{
					sb.Append(' ', i.ToString(CultureInfo.InvariantCulture).Length);
					sb.Append('|');
				}

				if (!subLine)
				{
					sb.Append(depth);
					sb.Append('>');
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets the depth of an element in the visual tree.
		/// </summary>
		internal static int GetDebugDepth(this object? elt) =>
			elt switch
			{
				null => 0,
#if __CROSSRUNTIME__
				UIElement fwElt => fwElt.Depth,
#endif
				_ => elt.GetParent()?.GetDebugDepth() + 1 ?? 0,
			};

		internal static CornerRadius GetCornerRadius(this UIElement uiElement)
		{
			if (uiElement is FrameworkElement fe && fe.TryGetCornerRadius(out var cornerRadius))
			{
				return cornerRadius;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("CornerRadius");
			return property != null && uiElement.GetValue(property) is CornerRadius t ? t : default;
		}

		internal static Thickness GetPadding(this UIElement uiElement)
		{
			if (uiElement is FrameworkElement fe && fe.TryGetPadding(out var padding))
			{
				return padding;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
		}

		internal static Thickness GetBorderThickness(this UIElement uiElement)
		{
			if (uiElement is FrameworkElement fe && fe.TryGetBorderThickness(out var borderThickness))
			{
				return borderThickness;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("BorderThicknessProperty");
			return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
		}

		internal static bool SetPadding(this UIElement uiElement, Thickness padding)
		{
			if (uiElement is FrameworkElement fe && fe.TrySetPadding(padding))
			{
				return true;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			if (property != null)
			{
				uiElement.SetValue(property, padding);
				return true;
			}

			return false;
		}

		internal static bool SetBorderThickness(this UIElement uiElement, Thickness borderThickness)
		{
			if (uiElement is FrameworkElement fe && fe.TrySetBorderThickness(borderThickness))
			{
				return true;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("BorderThicknessProperty");
			if (property != null)
			{
				uiElement.SetValue(property, borderThickness);
				return true;
			}

			return false;
		}

		private static Dictionary<(Type type, string property), DependencyProperty?>? _dependencyPropertyReflectionCache;

		internal static DependencyProperty? FindDependencyPropertyUsingReflection<TProperty>(this UIElement uiElement, string propertyName)
		{
			var type = uiElement.GetType();
			var propertyType = typeof(TProperty);
			var key = (ownerType: type, propertyName);

			_dependencyPropertyReflectionCache ??= new Dictionary<(Type, string), DependencyProperty?>(2);

			if (_dependencyPropertyReflectionCache.TryGetValue(key, out var property))
			{
				return property;
			}

			property =
				type
					.GetTypeInfo()
					.GetDeclaredProperty(propertyName)
					?.GetValue(null) as DependencyProperty
				?? type
					.GetTypeInfo()
					.GetDeclaredField(propertyName)
					?.GetValue(null) as DependencyProperty;

			if (property == null)
			{
				uiElement.Log().Warn($"The {propertyName} dependency property does not exist on {type}");
			}
			else if (property.Type != propertyType)
			{
				uiElement.Log().Warn($"The {propertyName} dependency property {type} is not of the {propertyType} Type.");
				property = null;
			}

			_dependencyPropertyReflectionCache[key] = property;

			return property;
		}
	}
}
