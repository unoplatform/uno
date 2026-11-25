#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

using static System.FormattableString;

namespace Uno.UI.Extensions;

/// <summary>
/// Formats values as short, culture-invariant strings, for visual tree dumps such as <see cref="VisualTreeExtensions.TreeGraph(DependencyObject)"/>.
/// </summary>
public static class PrettyPrint
{
	private static readonly Lazy<Dictionary<Color, string>> _knownColors = new(() => typeof(Colors)
		.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
		.Where(x => x.PropertyType == typeof(Color))
		// Aqua/Cyan and Fuchsia/Magenta are the same color: keep the first name of each pair, as ToDictionary rejects duplicate keys.
		.Where(x => x.Name is not ("Cyan" or "Magenta"))
		.ToDictionary(x => (Color)x.GetValue(null)!, x => x.Name)
	);

	/// <summary>
	/// Formats the type of <paramref name="o"/>, with its x:Name when it is a named <see cref="FrameworkElement"/>: <c>Border</c>, <c>Border#Root</c>.
	/// </summary>
	public static string FormatType(object? o)
	{
		if (o is null) return $"null";
		if (o is FrameworkElement { Name: { Length: > 0 } xName }) return $"{o.GetType().Name}#{xName}";

		return o.GetType().Name;
	}

	/// <summary>
	/// Formats <paramref name="o"/> as its value, or as its type name for a <see cref="DependencyObject"/>.
	/// </summary>
	public static string FormatValue(object? o)
	{
		if (o is null) return $"null";
		if (o is DependencyObject) return o.GetType().Name;

		return (o is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : o.ToString()) ?? "";
	}

	/// <summary>
	/// Formats a corner radius: <c>4</c> when uniform, otherwise <c>[top-left,top-right,bottom-right,bottom-left]</c>.
	/// </summary>
	public static string FormatCornerRadius(CornerRadius x)
	{
		// format: uniform, [top-left,top-right,bottom-right,bottom-left]
		if (x.TopLeft == x.TopRight && x.TopRight == x.BottomRight && x.BottomRight == x.BottomLeft) return Invariant($"{x.TopLeft:0.#}");
		return Invariant($"[{x.TopLeft:0.#},{x.TopRight:0.#},{x.BottomRight:0.#},{x.BottomLeft:0.#}]");
	}

	/// <summary>
	/// Formats a thickness: <c>1</c> when uniform, <c>[1,2]</c> when left-right and top-bottom match, otherwise <c>[1,2,3,4]</c>.
	/// </summary>
	public static string FormatThickness(Thickness x)
	{
		// format: uniform, [same-left-right,same-top-bottom], [left,top,right,bottom]
		if (x.Left == x.Top && x.Top == x.Right && x.Right == x.Bottom) return Invariant($"{x.Left:0.#}");
		if (x.Left == x.Right && x.Top == x.Bottom) return Invariant($"[{x.Left:0.#},{x.Top:0.#}]");
		return Invariant($"[{x.Left:0.#},{x.Top:0.#},{x.Right:0.#},{x.Bottom:0.#}]");
	}

	/// <summary>
	/// Formats a rectangle as <c>[{width}x{height}@{left},{top}]</c>, e.g. <c>[100x50@10,20]</c>.
	/// </summary>
	public static string FormatRect(Rect x)
	{
		return Invariant($"[{x.Width:0.#}x{x.Height:0.#}@{x.Left:0.#},{x.Top:0.#}]");
	}

	/// <summary>
	/// Formats a size as <c>{width}x{height}</c>, e.g. <c>100x50</c>.
	/// </summary>
	public static string FormatSize(Size size) => FormatSize(size.Width, size.Height);

	/// <summary>
	/// Formats a size as <c>{width}x{height}</c>, e.g. <c>100x50</c>.
	/// </summary>
	public static string FormatSize(double width, double height) => Invariant($"{width:0.#}x{height:0.#}");

	/// <summary>
	/// Formats a brush: a solid color brush as its known color name or <c>#AARRGGBB</c>, followed by <c>*opacity</c> when it isn't 1; any other brush as its type name.
	/// </summary>
	public static string? FormatBrush(Brush? b)
	{
		if (b is SolidColorBrush scb) return
			// ColorWithOpacity compounds Brush::Opacity into Color::Alpha, but we want to keep the context of both.
			(_knownColors.Value.TryGetValue(scb.Color, out var name) ? name : $"#{scb.Color.A:X2}{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}") +
			(scb.Opacity != 1 ? Invariant($"*{scb.Opacity:0.###}") : "");

		return b?.GetType().Name;
	}

	/// <summary>
	/// Formats a column definition as its width, or <c>[min~width~max]</c> when it has a min or max width.
	/// </summary>
	public static string FormatGridDefinition(ColumnDefinition def) => FormatGridDefinition(def.MinWidth, def.Width, def.MaxWidth);

	/// <summary>
	/// Formats a row definition as its height, or <c>[min~height~max]</c> when it has a min or max height.
	/// </summary>
	public static string FormatGridDefinition(RowDefinition def) => FormatGridDefinition(def.MinHeight, def.Height, def.MaxHeight);

	private static string FormatGridDefinition(double min, GridLength value, double max)
	{
		return min != 0d || max != double.PositiveInfinity
			? Invariant($"[{min:0.##}~{FormatGridLength(value)}~{max:0.##}]") // [min~value~max]
			: FormatGridLength(value); // value
	}

	/// <summary>
	/// Formats a grid length: <c>A</c> for auto, <c>100</c> for pixels, <c>*</c> or <c>2*</c> for stars.
	/// </summary>
	public static string FormatGridLength(GridLength x) => FormatGridLength(x.Value, x.GridUnitType);

	/// <summary>
	/// Formats a grid length: <c>A</c> for auto, <c>100</c> for pixels, <c>*</c> or <c>2*</c> for stars.
	/// </summary>
	public static string FormatGridLength(double value, GridUnitType type) => type switch
	{
		GridUnitType.Auto => "A",
		GridUnitType.Pixel => Invariant($"{value:0.##}"),
		GridUnitType.Star => value switch
		{
			0d => "0*",
			1d => "*",

			_ => Invariant($"{value:0.##}*")
		},
		_ => /* CS8524: (GridUnitType)123 */ Invariant($"{value:0.##}{type}")
	};

	/// <summary>
	/// Formats a point as <c>{x},{y}</c>, e.g. <c>10,20</c>.
	/// </summary>
	public static string FormatPoint(Point p) => FormatPoint(p.X, p.Y);

	/// <summary>
	/// Formats a point as <c>{x},{y}</c>, e.g. <c>10,20</c>.
	/// </summary>
	public static string FormatPoint(double x, double y) => Invariant($"{x:0.#},{y:0.#}");

	/// <summary>
	/// Formats a binding expression as its path, followed by its mode when it isn't one-way and its relative source: <c>[Path=Title, TwoWay]</c>.
	/// </summary>
	public static string FormatBinding(BindingExpression? be)
	{
		if (be == null) return "null";

		var descriptions = new List<string>();
		descriptions.Add($"Path={be.ParentBinding.Path?.Path}");
		if (be.ParentBinding.Mode != BindingMode.OneWay) descriptions.Add(be.ParentBinding.Mode.ToString());
		if (be.ParentBinding.RelativeSource is { Mode: not RelativeSourceMode.None } rs) descriptions.Add(rs.Mode.ToString());

		return $"[{string.Join(", ", descriptions)}]";
	}

	/// <summary>
	/// Escapes line breaks (and optionally tabs) so <paramref name="s"/> fits on one line.
	/// </summary>
	public static string EscapeMultiline(string s, bool escapeTabs = false)
	{
		s = s
			.Replace("\r", "\\r")
			.Replace("\n", "\\n");

		if (escapeTabs)
		{
			s = s.Replace("\t", "\\t");
		}

		return s;
	}
}
