using Windows.UI.ViewManagement;

namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class Accessibility
		{
			private static bool _highContrast;

			/// <summary>
			/// Allows overriding the value returned by <see cref="AccessibilitySettings.HighContrast"/>.
			/// When changed, the <see cref="AccessibilitySettings.HighContrastChanged"/> event is raised.
			/// </summary>
			public static bool HighContrast
			{
				get => _highContrast;
				set
				{
					if (_highContrast != value)
					{
						_highContrast = value;
						AccessibilitySettings.OnHighContrastChanged();
					}
				}
			}

			/// <summary>
			/// Allows overriding the value returned by <see cref="Windows.UI.ViewManagement.AccessibilitySettings.HighContrastScheme"/>.
			/// </summary>
			public static string HighContrastScheme { get; set; } = "High Contrast Black";
		}
	}
}
