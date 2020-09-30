#nullable enable

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
    {
		internal static SystemTheme SystemTheme => GetSystemTheme();

#if NET461 || __NETSTD_REFERENCE__
		private static SystemTheme GetSystemTheme() => SystemTheme.Light;
#endif

	}
}
