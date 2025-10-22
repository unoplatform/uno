#nullable enable

using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Xaml.Media;

internal static class DefaultBrushes
{
	private const string DefaultTextForegroundThemeBrushKey = "DefaultTextForegroundThemeBrush";

	private static Brush? _textForegroundBrush;

	internal static Brush TextForegroundBrush => GetDefaultTextBrush(DefaultTextForegroundThemeBrushKey, ref _textForegroundBrush);

	internal static SolidColorBrush SelectionHighlightColor { get; } = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));

	internal static SolidColorBrush SelectedTextHighlightColor { get; } = new SolidColorBrush(Colors.White);

	internal static void ResetDefaultThemeBrushes() => _textForegroundBrush = null;

	private static Brush GetDefaultTextBrush(string key, ref Brush? brush)
	{
		if (Application.Current is null)
		{
			// Called too early or within unit tests, fallback
			return SolidColorBrushHelper.Black;
		}

		if (brush is null)
		{
			if (Application.Current.Resources.TryGetValue(key, out var defaultBrushObject) &&
				defaultBrushObject is Brush defaultBrush)
			{
				brush = defaultBrush;
			}
			else
			{
				// Fallback to black/white
				brush = CoreApplication.RequestedTheme == SystemTheme.Dark ?
					SolidColorBrushHelper.White : SolidColorBrushHelper.Black;
			}
		}

		return brush;
	}
}
