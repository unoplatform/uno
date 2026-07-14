#nullable enable

namespace Uno.Helpers.Theming;

internal static partial class AccentColorHelper
{
	// CSS AccentColor support is not yet implemented for WASM; returns null to use the default palette.
	private static partial AccentColorPalette? GetPlatformAccentColorPalette() => null;
}
