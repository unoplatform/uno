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
}
