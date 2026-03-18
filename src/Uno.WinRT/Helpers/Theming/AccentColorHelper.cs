#nullable enable

using System;

namespace Uno.Helpers.Theming;

/// <summary>
/// Provides centralized access to the current OS accent color palette,
/// with support for platform-specific retrieval and manual override.
/// </summary>
internal static partial class AccentColorHelper
{
	private static AccentColorPalette? _overridePalette;
	private static AccentColorPalette? _cachedPlatformPalette;
	private static EventHandler? _accentColorChanged;

	/// <summary>
	/// Gets the current accent color palette: override > platform > default.
	/// </summary>
	internal static AccentColorPalette CurrentPalette =>
		_overridePalette ?? _cachedPlatformPalette ?? GetAndCachePlatformPalette();

	/// <summary>
	/// Raised when the accent color changes (either via platform notification or override).
	/// </summary>
	internal static event EventHandler? AccentColorChanged
	{
		add
		{
			_accentColorChanged += value;
			ObserveAccentColorChanges();
		}
		remove => _accentColorChanged -= value;
	}

	/// <summary>
	/// Sets or clears the override accent color palette.
	/// </summary>
	internal static void SetOverridePalette(AccentColorPalette? palette)
	{
		_overridePalette = palette;
		RaiseAccentColorChanged();
	}

	/// <summary>
	/// Re-queries the platform accent color and raises the changed event if it differs.
	/// </summary>
	internal static void RefreshAccentColor()
	{
		if (_overridePalette is not null)
		{
			// Override is active; platform changes are ignored.
			return;
		}

		var previousPalette = _cachedPlatformPalette;
		_cachedPlatformPalette = GetPlatformAccentColorPalette();

		if (_cachedPlatformPalette is null && previousPalette is null)
		{
			return;
		}

		if (_cachedPlatformPalette is null || previousPalette is null ||
			_cachedPlatformPalette.Value.Accent != previousPalette.Value.Accent)
		{
			RaiseAccentColorChanged();
		}
	}

	private static AccentColorPalette GetAndCachePlatformPalette()
	{
		_cachedPlatformPalette ??= GetPlatformAccentColorPalette();
		return _cachedPlatformPalette ?? AccentColorPalette.Default;
	}

	/// <summary>
	/// Platform-specific implementation to retrieve the OS accent color palette.
	/// Returns null if the platform does not support accent color retrieval.
	/// </summary>
	private static partial AccentColorPalette? GetPlatformAccentColorPalette();

	static partial void ObserveAccentColorChanges();

	private static void RaiseAccentColorChanged() =>
		_accentColorChanged?.Invoke(null, EventArgs.Empty);
}
