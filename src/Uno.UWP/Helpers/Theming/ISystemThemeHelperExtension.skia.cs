#nullable enable

using System;

namespace Uno.Helpers.Theming;

/// <summary>
/// System theme helper extension for Skia.
/// </summary>
internal interface ISystemThemeHelperExtension
{
	/// <summary>
	/// Provides a notification that the OS theme has changed.
	/// </summary>
	event EventHandler SystemThemeChanged;

	/// <summary>
	/// Retrieves the current system theme.
	/// </summary>
	/// <returns>System theme.</returns>
	SystemTheme GetSystemTheme();

	/// <summary>
	/// Gets whether the system High Contrast mode is currently enabled.
	/// </summary>
	bool IsHighContrastEnabled() => false;

	/// <summary>
	/// Gets the name of the active High Contrast scheme.
	/// Returns a descriptive name such as "High Contrast Black",
	/// "High Contrast White", or "High Contrast #1".
	/// </summary>
	string GetHighContrastSchemeName() => "High Contrast Black";

	/// <summary>
	/// Gets the current High Contrast system colors, if the platform provides them.
	/// </summary>
	HighContrastSystemColors? GetHighContrastSystemColors() => null;

	/// <summary>
	/// Provides a notification that the OS High Contrast setting has changed.
	/// </summary>
	event EventHandler HighContrastChanged
	{
		add { }
		remove { }
	}
}
