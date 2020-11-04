#nullable enable

using System;
using AppKit;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		/// <summary>
		/// Based on <see cref="https://forums.developer.apple.com/thread/118974" />
		/// </summary>
		/// <returns>System theme</returns>
		private static SystemTheme GetSystemTheme()
		{
			var version = DeviceHelper.OperatingSystemVersion;
			if (version >= new Version(10, 14))
			{
				var app = NSAppearance.CurrentAppearance?.FindBestMatch(new string[]
				{
					NSAppearance.NameAqua,
					NSAppearance.NameDarkAqua
				});

				if (app == NSAppearance.NameDarkAqua)
				{
					return SystemTheme.Dark;
				}
			}
			return SystemTheme.Light;
		}
	}
}
