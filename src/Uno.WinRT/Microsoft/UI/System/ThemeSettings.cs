#nullable enable

using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.System;

/// <summary>
/// Provides access to the theme settings (such as high contrast) that apply to a window.
/// </summary>
public partial class ThemeSettings
{
	private readonly AccessibilitySettings _accessibilitySettings = new();

	internal ThemeSettings(WindowId windowId)
	{
		WindowId = windowId;
		_accessibilitySettings.HighContrastChanged += OnHighContrastChanged;
	}

	internal WindowId WindowId { get; }

	/// <summary>
	/// Gets a value that indicates whether high contrast is on for the window.
	/// </summary>
	public bool HighContrast => _accessibilitySettings.HighContrast;

	/// <summary>
	/// Gets the name of the active high contrast color scheme.
	/// </summary>
	public string HighContrastScheme => _accessibilitySettings.HighContrastScheme;

	/// <summary>
	/// Occurs when any of the theme settings change.
	/// </summary>
	public event TypedEventHandler<ThemeSettings, object?>? Changed;

	/// <summary>
	/// Creates a ThemeSettings object for the given window.
	/// </summary>
	// TODO Uno: high contrast is system-wide on every Uno target, so the window id does not scope the settings.
	public static ThemeSettings CreateForWindowId(WindowId windowId) => new(windowId);

	private void OnHighContrastChanged(AccessibilitySettings sender, object args) => Changed?.Invoke(this, null);
}
