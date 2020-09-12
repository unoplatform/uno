using System;
using Windows.UI.Xaml;

namespace Uno.UI.Skia.Platform
{
	public class WpfApplicationExtension : IApplicationExtension
	{
		private readonly Windows.UI.Xaml.Application _owner;
		private readonly Windows.UI.Xaml.IApplicationEvents _ownerEvents;

		public WpfApplicationExtension(object owner)
		{
			_owner = (Application)owner;
		}

		public ApplicationTheme GetDefaultSystemTheme()
		{
			const string subKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

			if (Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey) is { } key)
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
