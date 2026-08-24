using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml;

internal static class ThemingHelper
{
	internal static Color GetRootVisualBackground() =>
		Application.Current.RequestedTheme == ApplicationTheme.Light ?
			Colors.White : Colors.Black;

	internal static Color FromArgb(uint argb) =>
		Color.FromArgb(
			(byte)(argb >> 24),
			(byte)(argb >> 16),
			(byte)(argb >> 8),
			(byte)argb);
}
