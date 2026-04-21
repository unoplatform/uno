#nullable enable
using Uno.Helpers.Theming;
using Windows.UI.ViewManagement;

namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class Accessibility
	{
		private static bool _highContrast;
		private static bool? _highContrastOverride;
		private static string? _highContrastSchemeOverride;
		private static HighContrastSystemColors? _highContrastSystemColorsOverride;

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
					_highContrastOverride = value;
					NotifyHighContrastChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets an explicit override for High Contrast state on Skia targets.
		/// When null, the value is read from the OS via platform detection.
		/// When set to true/false, it overrides OS detection.
		/// </summary>
		internal static bool? HighContrastOverride
		{
			get => _highContrastOverride;
			set
			{
				if (_highContrastOverride != value)
				{
					_highContrastOverride = value;
					NotifyHighContrastChanged();
				}
			}
		}

		/// <summary>
		/// Allows overriding the value returned by <see cref="Windows.UI.ViewManagement.AccessibilitySettings.HighContrastScheme"/>.
		/// </summary>
		public static string HighContrastScheme
		{
			get => _highContrastSchemeOverride ?? "High Contrast Black";
			set
			{
				if (_highContrastSchemeOverride != value)
				{
					_highContrastSchemeOverride = value;
					NotifyHighContrastChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets an explicit override for the High Contrast scheme name on Skia targets.
		/// When null, the value is read from the OS via platform detection.
		/// </summary>
		internal static string? HighContrastSchemeOverride
		{
			get => _highContrastSchemeOverride;
			set
			{
				if (_highContrastSchemeOverride != value)
				{
					_highContrastSchemeOverride = value;
					NotifyHighContrastChanged();
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
					NotifyHighContrastChanged();
				}
			}
		}

		private static void NotifyHighContrastChanged()
		{
			AccessibilitySettings.OnHighContrastChanged();
			UISettings.OnColorValuesChanged();
		}
	}
}

