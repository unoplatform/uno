namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class Accessibility
		{
			/// <summary>
			/// Allows overriding the value returned by <see cref="Windows.UI.ViewManagement.AccessibilitySettings.HighContrast"/>.
			/// </summary>
			public static bool HighContrast { get; set; } = false;

			/// <summary>
			/// Allows overriding the value returned by <see cref="Windows.UI.ViewManagement.AccessibilitySettings.HighContrastScheme"/>.
			/// </summary>
			public static string HighContrastScheme { get; set; } = "High Contrast Black";
		}
	}
}
