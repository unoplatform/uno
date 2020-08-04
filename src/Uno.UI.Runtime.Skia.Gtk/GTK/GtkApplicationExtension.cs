#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class GtkApplicationExtension : IApplicationExtension
	{
		private readonly Windows.UI.Xaml.Application _owner;
		private readonly Windows.UI.Xaml.IApplicationEvents _ownerEvents;

		public GtkApplicationExtension(object owner)
		{
			_owner = (Application)owner;
			_ownerEvents = (IApplicationEvents)owner;
		}

		public ApplicationTheme GetDefaultSystemTheme()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return GetWindowsTheme();
			}
			else
			{
				return Gtk.Settings.Default.ApplicationPreferDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light;
			}
		}

		private static ApplicationTheme GetWindowsTheme()
		{
			var subKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

			if (Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey) is RegistryKey key)
			{
				if (key.GetValue("AppsUseLightTheme") is int useLightTheme)
				{
					return useLightTheme != 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
				}
			}

			return ApplicationTheme.Light;
		}
	}
}
