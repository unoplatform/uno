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
		/// This reflects the effective platform high-contrast state.
		/// <see cref="WinRTFeatureConfiguration.Accessibility.HighContrast"/> can override the platform value.
		/// </remarks>
		public bool HighContrast => SystemThemeHelper.IsHighContrast;

		/// <summary>
		/// Gets the name of the default high contrast color scheme.
		/// </summary>
		/// <remarks>
		/// This reflects the effective platform high-contrast scheme.
		/// <see cref="WinRTFeatureConfiguration.Accessibility.HighContrastScheme"/> can override the platform value.
		/// </remarks>
		public string HighContrastScheme => SystemThemeHelper.HighContrastSchemeName;

		/// <summary>
		/// Occurs when the system high contrast feature turns on or off.
		/// </summary>
		/// <remarks>
		/// Raised when the effective platform or overridden high-contrast state changes.
		/// </remarks>
		public event TypedEventHandler<AccessibilitySettings, object> HighContrastChanged;

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
