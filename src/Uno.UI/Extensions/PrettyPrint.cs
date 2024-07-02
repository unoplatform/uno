using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

#if HAS_UNO_WINUI || WINAPPSDK || WINUI
using Colors = Windows.UI.Colors;
#else
using Colors = Windows.UI.Colors;
#endif

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

	public static string FormatObject(object o)
	{
		if (o is null) return $"null";
		if (o is FrameworkElement or DependencyObject) return o.GetType().Name;

		return o.ToString();
	}
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
	public static string FormatSize(Size size) => $"{size.Width:0.#}x{size.Height:0.#}";
	public static string FormatBrush(Brush b)
	{
		if (b is SolidColorBrush scb) return
			// ColorWithOpacity compounds Brush::Opacity into Color::Alpha, but we want to keep the context of both.
			(_knownColors.Value.TryGetValue(scb.Color, out var name) ? name : $"#{scb.Color.A:X2}{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}") +
			(scb.Opacity != 1 ? $"*{scb.Opacity:#.###}" : "");

		return b.GetType().Name;
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
