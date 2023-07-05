using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Uno.Helpers.Theming;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.Theming
{
	internal class GtkSystemThemeHelperExtension : ISystemThemeHelperExtension, IDisposable
	{
		private bool _disposedValue;

		internal GtkSystemThemeHelperExtension(object owner)
		{
			ObserveSystemTheme();
		}

		public event EventHandler? SystemThemeChanged;

		private void ObserveSystemTheme()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
			}
			else
			{
				var settings = Gtk.Settings.Default;
				settings.AddNotification(nameof(settings.ApplicationPreferDarkTheme), ApplicationPreferDarkThemeHandler);
			}
		}

		private void UnobserveSystemTheme()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
			}
			else
			{
				var settings = Gtk.Settings.Default;
				settings.RemoveNotification(nameof(settings.ApplicationPreferDarkTheme), ApplicationPreferDarkThemeHandler);
			}
		}

		private void ApplicationPreferDarkThemeHandler(object o, GLib.NotifyArgs args)
			=> SystemThemeChanged?.Invoke(o, EventArgs.Empty);

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
				UnobserveSystemTheme();
				_disposedValue = true;
			}
		}

		~GtkSystemThemeHelperExtension()
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
