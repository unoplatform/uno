#nullable enable

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		internal static SystemTheme SystemTheme => GetSystemTheme();

#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
		private static SystemTheme GetSystemTheme() => SystemTheme.Light;
#endif

	}
}
