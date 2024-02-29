using System;
using System.Collections.Concurrent;
using Uno;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Provides access to the high contrast accessibility settings.
	/// </summary>
	public partial class AccessibilitySettings
	{
		private static readonly ConcurrentDictionary<WeakReference<AccessibilitySettings>, object?> _instances = new ConcurrentDictionary<WeakReference<AccessibilitySettings>, object?>();
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
		/// In Uno Platform this returns the value of <see cref="WinRTFeatureConfiguration.Accessibility.HighContrast"/>.
		/// The default is false.
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool HighContrast => WinRTFeatureConfiguration.Accessibility.HighContrast;

		/// <summary>
		/// Gets the name of the default high contrast color scheme.
		/// </summary>
		/// <remarks>
		/// In Uno Platform this returns the value of <see cref="WinRTFeatureConfiguration.Accessibility.HighContrastScheme"/>.
		/// The default is "High Contrast Black".
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public string HighContrastScheme => WinRTFeatureConfiguration.Accessibility.HighContrastScheme;

		/// <summary>
		/// Occurs when the system high contrast feature turns on or off.
		/// </summary>
		/// <remarks>
		///	Raised when <see cref="WinRTFeatureConfiguration.Accessibility.HighContrast"/> changes.
		/// </remarks>
		[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public event TypedEventHandler<AccessibilitySettings, object?>? HighContrastChanged;

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
