#nullable enable

using Uno.Helpers.Theming;

namespace Uno.UI.Runtime.Skia.Wpf.WPF.Extensions.Helper.Theming
{
    internal class WpfSystemThemeHelperExtension : ISystemThemeHelperExtension
    {
		internal WpfSystemThemeHelperExtension(object owner)
		{
		}

		public SystemTheme GetSystemTheme()
		{
			const string subKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

			if (Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey) is { } key)
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
