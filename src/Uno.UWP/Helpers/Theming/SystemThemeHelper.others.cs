#if NET461 || __NETSTD_REFERENCE__
#nullable enable

using System;
using Windows.ApplicationModel.Core;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		/// <summary>
		/// For unsupported targets we default to light theme.
		/// </summary>
		private static SystemTheme GetSystemTheme() => SystemTheme.Light;
	}
}
#endif
