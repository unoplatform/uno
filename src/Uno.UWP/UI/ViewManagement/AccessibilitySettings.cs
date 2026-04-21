using System;
using System.Collections.Concurrent;
using Uno;
using Uno.Helpers.Theming;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Provides access to the high contrast accessibility settings.
	/// </summary>
	public partial class AccessibilitySettings
	{
		private static readonly ConcurrentDictionary<WeakReference<AccessibilitySettings>, object> _instances = new ConcurrentDictionary<WeakReference<AccessibilitySettings>, object>();
		private readonly WeakReference<AccessibilitySettings> _weakReference;

		/// <summary>
		/// Initializes a new AccessibilitySettings object.
		/// </summary>
		public AccessibilitySettings()
		{
			_weakReference = new WeakReference<AccessibilitySettings>(this);
			_instances.TryAdd(_weakReference, null);
		}

		~AccessibilitySettings()
		{
			_instances.TryRemove(_weakReference, out var _);
		}

		/// <summary>
		/// Gets a value that indicates whether the system high contrast feature is on or off.
		/// </summary>
		/// <remarks>
		/// On Skia and WebAssembly targets, this reads from the platform via <see cref="SystemThemeHelper"/>.
		/// Use <see cref="WinRTFeatureConfiguration.Accessibility.HighContrast"/> to override.
		/// On other platforms, returns the value of <see cref="WinRTFeatureConfiguration.Accessibility.HighContrast"/>.
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
		public bool HighContrast =>
#if __SKIA__ || __WASM__
			WinRTFeatureConfiguration.Accessibility.HighContrastOverride ?? SystemThemeHelper.IsHighContrastEnabled;
#else
			WinRTFeatureConfiguration.Accessibility.HighContrast;
#endif

		/// <summary>
		/// Gets the name of the default high contrast color scheme.
		/// </summary>
		/// <remarks>
		/// On Skia and WebAssembly targets, this reads from the platform via <see cref="SystemThemeHelper"/>.
		/// Use <see cref="WinRTFeatureConfiguration.Accessibility.HighContrastScheme"/> to override.
		/// On other platforms, returns the value of <see cref="WinRTFeatureConfiguration.Accessibility.HighContrastScheme"/>.
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
		public string HighContrastScheme =>
#if __SKIA__ || __WASM__
			WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride ?? SystemThemeHelper.HighContrastSchemeName;
#else
			WinRTFeatureConfiguration.Accessibility.HighContrastScheme;
#endif

		/// <summary>
		/// Occurs when the system high contrast feature turns on or off.
		/// </summary>
		[NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
		public event TypedEventHandler<AccessibilitySettings, object> HighContrastChanged;

		/// <summary>
		/// Gets whether High Contrast is currently active (from any source).
		/// This is used internally for theme selection.
		/// </summary>
		internal static bool IsHighContrastActive =>
#if __SKIA__ || __WASM__
			WinRTFeatureConfiguration.Accessibility.HighContrastOverride ?? SystemThemeHelper.IsHighContrastEnabled;
#else
			WinRTFeatureConfiguration.Accessibility.HighContrast;
#endif

		internal static void OnHighContrastChanged()
		{
			foreach (var instance in _instances)
			{
				var weakReference = instance.Key;
				if (weakReference.TryGetTarget(out var accessibilitySettings))
				{
					accessibilitySettings.HighContrastChanged?.Invoke(accessibilitySettings, null);
				}
			}
		}
	}
}

