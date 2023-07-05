using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.Helpers.Theming;

namespace Uno.UI.Xaml.Media;

internal static class DefaultBrushes
{
	private const string DefaultTextForegroundThemeBrushKey = "DefaultTextForegroundThemeBrush";

	private static Brush? _textForegroundBrush;

	internal static Brush TextForegroundBrush => GetDefaultTextBrush();

	internal static SolidColorBrush SelectionHighlightColor { get; } = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));

	internal static void ResetDefaultThemeBrushes() => _textForegroundBrush = null;

	private static Brush GetDefaultTextBrush()
	{
		if (Application.Current is null)
		{
			// Called too early or within unit tests, fallback
			return SolidColorBrushHelper.Black;
		}

		if (_textForegroundBrush is null)
		{
			if (Application.Current.Resources.TryGetValue(DefaultTextForegroundThemeBrushKey, out var defaultBrushObject) &&
				defaultBrushObject is Brush defaultBrush)
			{
				_textForegroundBrush = defaultBrush;
			}
			else
			{
				// Fallback to black/white
				_textForegroundBrush = CoreApplication.RequestedTheme == SystemTheme.Dark ?
					SolidColorBrushHelper.White : SolidColorBrushHelper.Black;
			}
		}

		return _textForegroundBrush;
	}
}
