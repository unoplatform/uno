#nullable enable

using System;
using System.Linq;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Colors = Microsoft.UI.Colors;

namespace Uno.Diagnostics.UI;

internal class DiagnosticViewHelper
{
	private static readonly Color _white = new(255, 255, 255, 255);

	public static TextBlock CreateText(string? text)
		=> new()
		{
			Text = text ?? "--",
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 10,
			Foreground = new SolidColorBrush(_white)
		};

	public static DiagnosticViewHelper<TextBlock> CreateText<T>(Func<T> value)
		where T : struct
		=> new(() => CreateText(value().ToString()), tb => tb.Text = value().ToString());

	public static DiagnosticViewHelper<TextBlock> CreateText<T>(Func<string?> value)
		where T : struct
		=> new(() => CreateText(value()), tb => tb.Text = value() ?? "--");
}
