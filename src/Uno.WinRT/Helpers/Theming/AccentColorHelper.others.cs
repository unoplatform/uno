#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
#nullable enable

namespace Uno.Helpers.Theming;

internal static partial class AccentColorHelper
{
	private static partial AccentColorPalette? GetPlatformAccentColorPalette() => null;
}
#endif
