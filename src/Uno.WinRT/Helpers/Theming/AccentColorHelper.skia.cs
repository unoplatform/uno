#nullable enable

using Uno.Foundation.Extensibility;

namespace Uno.Helpers.Theming;

internal static partial class AccentColorHelper
{
	private static IAccentColorExtension? _accentColorExtension;
	private static bool _isObservingAccentColorChanges;

	private static partial AccentColorPalette? GetPlatformAccentColorPalette() =>
		GetExtension()?.GetAccentColorPalette();

	static partial void ObserveAccentColorChanges()
	{
		// Subscribe to the platform extension exactly once, even if AccentColorChanged gains
		// multiple managed subscribers, to avoid duplicate RefreshAccentColor() calls per OS change.
		if (_isObservingAccentColorChanges)
		{
			return;
		}

		if (GetExtension() is { } extension)
		{
			extension.AccentColorChanged += OnPlatformAccentColorChanged;
			_isObservingAccentColorChanges = true;
		}
	}

	private static void OnPlatformAccentColorChanged(object? sender, System.EventArgs e) =>
		RefreshAccentColor();

	private static IAccentColorExtension? GetExtension()
	{
		if (_accentColorExtension is null)
		{
			ApiExtensibility.CreateInstance(typeof(AccentColorHelper), out _accentColorExtension);
		}

		return _accentColorExtension;
	}
}
