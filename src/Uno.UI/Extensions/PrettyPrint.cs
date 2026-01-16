using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

using Colors = Microsoft.UI.Colors;

namespace Uno.UI.Extensions;


#if WINAPPSDK || WINDOWS_UWP
internal static class PrettyPrint
#else
public static class PrettyPrint
#endif
{
	private static readonly Lazy<Dictionary<Color, string>> _knownColors = new(() => typeof(Colors)
		.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
		.Where(x => x.PropertyType == typeof(Color))
		.Where(x => !"Aqua=Cyan,Fuchsia=Magenta".Split(',').SelectMany(y => y.Split('=').Skip(1)).Contains(x.Name)) // skip duplicated colors
		.ToDictionary(x => (Color)x.GetValue(null)!, x => x.Name)
	);

	public static string FormatType(object o)
	{
		if (o is null) return $"null";
		if (o is FrameworkElement { Name: { Length: > 0 } xName }) return $"{o.GetType().Name}#{xName}";

		return o.GetType().Name;
	}
	public static string FormatObject(object o)
	{
		if (o is null) return $"null";
		if (o is FrameworkElement or DependencyObject) return o.GetType().Name;

		return o.ToString();
	}
	public static string FormatData(object x) => x?.GetType().Name ?? "<null>";
	public static string FormatCornerRadius(CornerRadius x)
	{
		// format: uniform, [left,top,right,bottom]
		if (x.TopLeft == x.TopRight && x.TopRight == x.BottomRight && x.BottomRight == x.BottomLeft) return $"{x.TopLeft:0.#}";
		return $"[{x.TopLeft:0.#},{x.TopRight:0.#},{x.BottomRight:0.#},{x.BottomLeft:0.#}]";
	}
	public static string FormatThickness(Thickness x)
	{
		// format: uniform, [same-left-right,same-top-bottom], [left,top,right,bottom]
		if (x.Left == x.Top && x.Top == x.Right && x.Right == x.Bottom) return $"{x.Left:0.#}";
		if (x.Left == x.Right && x.Top == x.Bottom) return $"[{x.Left:0.#},{x.Top:0.#}]";
		return $"[{x.Left:0.#},{x.Top:0.#},{x.Right:0.#},{x.Bottom:0.#}]";
	}
	public static string FormatRect(Rect x)
	{
		return $"[{x.Width:0.#}x{x.Height:0.#}@{x.Left:0.#},{x.Top:0.#}]";
	}
#if __ANDROID__
	public static string FormatViewRect(Android.Views.View x)
	{
		return $"[{x.Width:0.#}x{x.Height:0.#}@{x.Left:0.#},{x.Top:0.#}]";
	}
#endif
	public static string FormatSize(Size size) => FormatSize(size.Width, size.Height);
	public static string FormatSize(double width, double height) => $"{width:0.#}x{height:0.#}";
	public static string FormatBrush(Brush b)
	{
		if (b is SolidColorBrush scb) return
			// ColorWithOpacity compounds Brush::Opacity into Color::Alpha, but we want to keep the context of both.
			(_knownColors.Value.TryGetValue(scb.Color, out var name) ? name : $"#{scb.Color.A:X2}{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}") +
			(scb.Opacity != 1 ? $"*{scb.Opacity:#.###}" : "");

		return b?.GetType().Name;
	}
	public static string FormatGridDefinition(ColumnDefinition def) => FormatGridDefinition(def.MinWidth, def.Width, def.MaxWidth);
	public static string FormatGridDefinition(RowDefinition def) => FormatGridDefinition(def.MinHeight, def.Height, def.MaxHeight);
	private static string FormatGridDefinition(double min, GridLength value, double max)
	{
		return min != 0d || max != double.PositiveInfinity
			? $"[{min:#.##}~{FormatGridLength(value)}~{max:#.##}]" // [min~value~max]
			: FormatGridLength(value); // value
	}
	public static string FormatGridLength(GridLength x) => FormatGridLength(x.Value, x.GridUnitType);
	public static string FormatGridLength(double value, GridUnitType type) => type switch
	{
		GridUnitType.Auto => "A",
		GridUnitType.Pixel => $"{value:#.##}",
		GridUnitType.Star => value switch
		{
			0d => "0*",
			1d => "*",

			_ => $"{value:#.##}*"
		},
		_ => /* CS8524: (GridUnitType)123 */ $"{value:#.##}{type}"
	};
	public static string FormatPoint(Point p) => FormatPoint(p.X, p.Y);
	public static string FormatPoint(double x, double y) => $"{x:0.#},{y:0.#}";
	public static string FormatBinding(BindingExpression be)
	{
		if (be == null) return "null";

		var descriptions = new List<string>();
		descriptions.Add($"Path={be.ParentBinding.Path?.Path}");
		if (be.ParentBinding.Mode != BindingMode.OneWay) descriptions.Add(be.ParentBinding.Mode.ToString());
		if (be.ParentBinding.RelativeSource is { Mode: not RelativeSourceMode.None } rs) descriptions.Add(rs.Mode.ToString());

		return $"[{string.Join(", ", descriptions)}]";
	}
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
