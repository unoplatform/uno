using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Extensions;

internal static class PrettyPrint
{
	private static readonly Lazy<Dictionary<Color, string>> _knownColors = new(() => typeof(Colors)
		.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
		.Where(x => x.PropertyType == typeof(Color))
		.Where(x => !"Aqua=Cyan,Fuchsia=Magenta".Split(',').SelectMany(y => y.Split('=').Skip(1)).Contains(x.Name)) // skip duplicated colors
		.ToDictionary(x => (Color)x.GetValue(null)!, x => x.Name)
	);

	internal static string FormatCornerRadius(CornerRadius x)
	{
		// format: uniform, [left,top,right,bottom]
		if (x.TopLeft == x.TopRight && x.TopRight == x.BottomRight && x.BottomRight == x.BottomLeft) return $"{x.TopLeft:0.#}";
		return $"[{x.TopLeft:0.#},{x.TopRight:0.#},{x.BottomRight:0.#},{x.BottomLeft:0.#}]";
	}
	internal static string FormatThickness(Thickness x)
	{
		// format: uniform, [same-left-right,same-top-bottom], [left,top,right,bottom]
		if (x.Left == x.Top && x.Top == x.Right && x.Right == x.Bottom) return $"{x.Left:0.#}";
		if (x.Left == x.Right && x.Top == x.Bottom) return $"[{x.Left:0.#},{x.Top:0.#}]";
		return $"[{x.Left:0.#},{x.Top:0.#},{x.Right:0.#},{x.Bottom:0.#}]";
	}
	internal static string FormatRect(Rect x)
	{
		return $"[{x.Width:0.#}x{x.Height:0.#}@{x.Left:0.#},{x.Top:0.#}]";
	}
#if __ANDROID__
	internal static string FormatViewRect(Android.Views.View x)
	{
		return $"[{x.Width:0.#}x{x.Height:0.#}@{x.Left:0.#},{x.Top:0.#}]";
	}
#endif
	internal static string FormatSize(Size size) => $"{size.Width:0.#}x{size.Height:0.#}";
	internal static string FormatBrush(Brush b)
	{
		if (b is SolidColorBrush scb) return
			// ColorWithOpacity compounds Brush::Opacity into Color::Alpha, but we want to keep the context of both.
			(_knownColors.Value.TryGetValue(scb.Color, out var name) ? name : $"#{scb.Color.A:X2}{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}") +
			(scb.Opacity != 1 ? $"*{scb.Opacity:#.###}" : "");

		return b.GetType().Name;
	}
}
