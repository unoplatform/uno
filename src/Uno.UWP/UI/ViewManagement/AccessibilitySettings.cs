using Uno;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Provides access to the high contrast accessibility settings.
	/// </summary>
	public partial class AccessibilitySettings
	{
		/// <summary>
		/// Initializes a new AccessibilitySettings object.
		/// </summary>
		public AccessibilitySettings()
		{
		}

		/// <summary>
		/// Gets a value that indicates whether the system high contrast feature is on or off.
		/// </summary>
		/// <remarks>
		/// In Uno Platform this returns the value of <see cref="WinRTFeatureConfiguration.Accessibility.HighContrast"/>.
		/// The default is false.
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool HighContrast => WinRTFeatureConfiguration.Accessibility.HighContrast;

		/// <summary>
		/// Gets the name of the default high contrast color scheme.
		/// </summary>
		/// <remarks>
		/// In Uno Platform this returns the value of <see cref="WinRTFeatureConfiguration.Accessibility.HighContrastScheme"/>.
		/// The default is "High Contrast Black".
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public string HighContrastScheme => WinRTFeatureConfiguration.Accessibility.HighContrastScheme;
	}
}
