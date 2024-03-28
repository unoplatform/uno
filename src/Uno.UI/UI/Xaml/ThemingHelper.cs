using Color = Windows.UI.Color;

namespace Windows.UI.Xaml;

internal static class ThemingHelper
{
	internal static Color GetRootVisualBackground() =>
		Application.Current.RequestedTheme == ApplicationTheme.Light ?
			Colors.White : Colors.Black;
}
