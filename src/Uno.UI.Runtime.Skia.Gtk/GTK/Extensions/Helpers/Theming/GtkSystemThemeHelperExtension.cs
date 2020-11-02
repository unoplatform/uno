#nullable enable

using System.Runtime.InteropServices;
using Microsoft.Win32;
using Uno.Helpers.Theming;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.Theming
{
	internal class GtkSystemThemeHelperExtension : ISystemThemeHelperExtension
	{
		internal GtkSystemThemeHelperExtension(object owner)
		{
		}

		public SystemTheme GetSystemTheme()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return GetWindowsTheme();
			}
			else
			{
				return Gtk.Settings.Default.ApplicationPreferDarkTheme ? SystemTheme.Dark : SystemTheme.Light;
			}
		}

		private static SystemTheme GetWindowsTheme()
		{
			var subKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

			if (Registry.CurrentUser.OpenSubKey(subKey) is RegistryKey key)
			{
				if (key.GetValue("AppsUseLightTheme") is int useLightTheme)
				{
					return useLightTheme != 0 ? SystemTheme.Light : SystemTheme.Dark;
				}
			}

			return SystemTheme.Light;
		}
	}
}
