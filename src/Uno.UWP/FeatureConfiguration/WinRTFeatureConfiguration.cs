using System.Globalization;

namespace Uno;

public static partial class WinRTFeatureConfiguration
{
	/// <summary>
	/// Used by tests cleanup to restore the default configuration for other tests!
	/// </summary>
	internal static void RestoreDefaults()
	{
		GestureRecognizer.RestoreDefaults();
	}

	public static class ApplicationLanguages
	{
		/// <summary>
		/// <see cref="Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride"/> takes effect (by changing current culture) during its setter execution.
		/// On Windows, changing this property *may* take effect only after restarting the application.
		/// Set this property to false to require an app restart for the change to take effect. The default of the property is true.
		/// </summary>
		public static bool UseLegacyPrimaryLanguageOverride { get; set; } = true;
	}

	public static class ResourceLoader
	{
		/// <summary>
		/// Use <see cref="Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride"/> as the override for resource resolution.
		/// Alternatively if set to false, use <see cref="CultureInfo.CurrentUICulture"/> as the override.
		/// </summary>
		public static bool UsePrimaryLanguageOverride { get; set; } = true;

		/// <summary>
		/// Determines if parsed string resources are preserved between languages change.
		/// </summary>
		public static bool PreserveParsedResources { get; set; }
	}

#if __ANDROID__
	public static class StoreContext
	{
		/// <summary>
		/// Set True to test Store Context APIs on Android. False by default.
		/// </summary>
		public static bool TestMode { get; set; }
	}
#endif

	internal static class DebugOptions
	{
#if DEBUG
		/// <summary>
		/// Adjusts all PointerPoint instances as if they were of type Touch.
		/// </summary>
		public static bool SimulateTouch { get; set; }
#endif

		internal static bool ForceEnableBackButtonIntegration { get; set; }
	}
}
