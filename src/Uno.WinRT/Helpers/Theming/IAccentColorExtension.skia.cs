#nullable enable

using System;

namespace Uno.Helpers.Theming;

/// <summary>
/// Extension interface for Skia platforms to provide OS accent color support.
/// </summary>
internal interface IAccentColorExtension
{
	/// <summary>
	/// Provides a notification that the OS accent color has changed.
	/// </summary>
	event EventHandler AccentColorChanged;

	/// <summary>
	/// Retrieves the current OS accent color palette, or null if not available.
	/// </summary>
	AccentColorPalette? GetAccentColorPalette();
}
