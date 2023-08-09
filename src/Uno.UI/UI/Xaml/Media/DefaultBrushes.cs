#nullable enable

using System.Diagnostics;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Media;

internal static class DefaultBrushes
{
	private const string DefaultTextForegroundThemeBrushKey = "DefaultTextForegroundThemeBrush";
	private const string HyperlinkForegroundKey = "HyperlinkForeground";

	private static Brush? _textForegroundBrush;
	private static Brush? _hyperlinkForegroundBrush;

	internal static Brush TextForegroundBrush => GetDefaultTextBrush(DefaultTextForegroundThemeBrushKey, ref _textForegroundBrush);

	// HyperlinkForeground is AccentTextFillColorPrimaryBrush
	// On light theme that is SystemAccentColorDark2 (#004275)
	// On dark theme, that is SystemAccentColorLight3 (#A6D8FF)
	internal static Brush HyperlinkForegroundBrush => GetDefaultTextBrush(HyperlinkForegroundKey, ref _hyperlinkForegroundBrush, Color.FromArgb(255, 0, 66, 117), Color.FromArgb(255, 166, 216, 255));

	internal static SolidColorBrush SelectionHighlightColor { get; } = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));

	internal static void ResetDefaultThemeBrushes()
	{
		_textForegroundBrush = null;
		_hyperlinkForegroundBrush = null;
	}

	private static Brush GetDefaultTextBrush(string key, ref Brush? brush, Color? lightFallback = null, Color? darkFallback = null)
	{
		// We expect both to be set, or none.
		Debug.Assert(!(darkFallback is null ^ lightFallback is null));

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
			else if (darkFallback.HasValue)
			{
				brush = new SolidColorBrush(CoreApplication.RequestedTheme == SystemTheme.Dark ? darkFallback.Value : lightFallback!.Value);
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
