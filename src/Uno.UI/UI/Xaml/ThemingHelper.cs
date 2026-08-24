using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml;

internal static class ThemingHelper
{
<<<<<<< HEAD
	internal static Color GetRootVisualBackground() =>
		Application.Current.RequestedTheme == ApplicationTheme.Light ?
			Colors.White : Colors.Black;
=======
	internal static bool IsHighContrastActive
	{
		get
		{
#if __SKIA__ && UNO_HAS_ENHANCED_LIFECYCLE
			return Uno.UI.Xaml.Core.CoreServices.Instance.Theming.HasHighContrastTheme();
#elif __SKIA__
			return Uno.Helpers.Theming.SystemThemeHelper.IsHighContrast;
#else
			return false;
#endif
		}
	}

	internal static Color FromArgb(uint argb) =>
		Color.FromArgb(
			(byte)(argb >> 24),
			(byte)(argb >> 16),
			(byte)(argb >> 8),
			(byte)argb);
>>>>>>> b71dc16 (fix(skia): Seed the root background from one source)
}
