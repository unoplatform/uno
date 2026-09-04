#nullable enable

using Uno.Helpers.Theming;
using Windows.UI.ViewManagement;

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class Accessibility
	{
		private static bool? _highContrastOverride;
		private static string? _highContrastSchemeOverride;
		private static HighContrastSystemColors? _highContrastSystemColorsOverride;

		/// <summary>
		/// Allows overriding the value returned by <see cref="AccessibilitySettings.HighContrast"/>.
		/// When changed, the <see cref="AccessibilitySettings.HighContrastChanged"/> event is raised.
		/// </summary>
		public static bool HighContrast
		{
			get => _highContrastOverride ?? false;
			set => HighContrastOverride = value;
		}

		/// <summary>
		/// Allows overriding the value returned by <see cref="Windows.UI.ViewManagement.AccessibilitySettings.HighContrastScheme"/>.
		/// </summary>
		public static string HighContrastScheme
		{
			get => _highContrastSchemeOverride ?? "High Contrast Black";
			set => HighContrastSchemeOverride = value;
		}

		internal static bool? HighContrastOverride
		{
			get => _highContrastOverride;
			set
			{
				if (_highContrastOverride != value)
				{
					_highContrastOverride = value;
					SystemThemeHelper.NotifyHighContrastSettingsChanged();
				}
			}
		}

		internal static string? HighContrastSchemeOverride
		{
			get => _highContrastSchemeOverride;
			set
			{
				if (_highContrastSchemeOverride != value)
				{
					_highContrastSchemeOverride = value;
					SystemThemeHelper.NotifyHighContrastSettingsChanged();
				}
			}
		}

		internal static HighContrastSystemColors? HighContrastSystemColorsOverride
		{
			get => _highContrastSystemColorsOverride;
			set
			{
				if (_highContrastSystemColorsOverride != value)
				{
					_highContrastSystemColorsOverride = value;
					SystemThemeHelper.NotifyHighContrastSettingsChanged();
				}
			}
		}
	}
}
