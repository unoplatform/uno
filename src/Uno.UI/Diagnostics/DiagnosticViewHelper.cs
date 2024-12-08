#nullable enable

using System;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.Diagnostics.UI;

internal class DiagnosticViewHelper
{
	private static readonly Color _white = new Color { A = 255, R = 255, G = 255, B = 255 };

	public static TextBlock CreateText(string? text)
		=> new()
		{
			Text = text ?? "--",
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 10,
			Foreground = new SolidColorBrush(_white)
		};

	public static DiagnosticViewManager<TextBlock> CreateText<T>(Func<T> value)
		where T : struct
		=> new(() => CreateText(value().ToString()), tb => tb.Text = value().ToString());

	public static DiagnosticViewManager<TextBlock> CreateText(Func<string?> value)
		=> new(() => CreateText(value()), tb => tb.Text = value() ?? "--");
}
