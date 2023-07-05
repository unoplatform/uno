#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
using System;
using Windows.ApplicationModel.Core;

namespace Uno.Helpers.Theming;

internal static partial class SystemThemeHelper
{
	/// <summary>
	/// For unsupported targets we default to light theme.
	/// </summary>
	private static SystemTheme GetSystemTheme() => SystemTheme.Light;
}
#endif
