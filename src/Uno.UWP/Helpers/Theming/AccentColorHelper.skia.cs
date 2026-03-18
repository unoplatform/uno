#nullable enable

using Uno.Foundation.Extensibility;

namespace Uno.Helpers.Theming;

internal static partial class AccentColorHelper
{
	private static IAccentColorExtension? _accentColorExtension;

	private static partial AccentColorPalette? GetPlatformAccentColorPalette() =>
		GetExtension()?.GetAccentColorPalette();

	static partial void ObserveAccentColorChanges()
	{
		if (GetExtension() is { } extension)
		{
			extension.AccentColorChanged += OnPlatformAccentColorChanged;
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
