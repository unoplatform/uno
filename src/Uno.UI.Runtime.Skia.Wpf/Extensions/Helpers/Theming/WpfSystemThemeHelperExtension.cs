#nullable enable

using Microsoft.Win32;
using System;
using Uno.Helpers.Theming;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.Helpers.Theming
{
	internal class WpfSystemThemeHelperExtension : ISystemThemeHelperExtension, IDisposable
	{
		private bool _disposedValue;

		internal WpfSystemThemeHelperExtension(object owner)
		{
			SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
		}

		public event EventHandler? SystemThemeChanged;

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

		private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			// This event is raised when the theme is changed,
			// but also for various other irrelevant reasons.
			// SystemThemeHelper retrieves the current theme to ensure
			// it actually changed, before changing the app theme.
			SystemThemeChanged?.Invoke(this, EventArgs.Empty);
		}

		#region IDisposable
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				// According to Microsoft, https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.systemevents.userpreferencechanged,
				// this event must be manully unhooked when the application is disposed.
				SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
				_disposedValue = true;
			}
		}

		~WpfSystemThemeHelperExtension()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
