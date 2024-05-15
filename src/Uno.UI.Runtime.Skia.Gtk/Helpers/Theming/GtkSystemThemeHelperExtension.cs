#nullable enable

using System;
using System.Runtime.InteropServices;
using Gtk;
using Microsoft.Win32;
using Uno.Helpers.Theming;

namespace Uno.UI.Runtime.Skia.Gtk.Extensions.Helpers.Theming
{
	internal class GtkSystemThemeHelperExtension : ISystemThemeHelperExtension, IDisposable
	{
		private bool _disposedValue;
		private string _previousThemeName;

		internal GtkSystemThemeHelperExtension(object owner)
		{
			_previousThemeName = Settings.Default.ThemeName;
			ObserveSystemTheme();
		}

		public event EventHandler? SystemThemeChanged;

		private void ObserveSystemTheme()
		{
			if (OperatingSystem.IsWindows())
			{
				SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
			}
			else
			{
				var settings = Settings.Default;
				settings.AddNotification(nameof(settings.ApplicationPreferDarkTheme), ApplicationPreferDarkThemeHandler);
				settings.AddNotification(nameof(settings.ThemeName), ApplicationPreferDarkThemeHandler);
			}
		}

		private void UnobserveSystemTheme()
		{
			if (OperatingSystem.IsWindows())
			{
				SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
			}
			else
			{
				var settings = Settings.Default;
				settings.RemoveNotification(nameof(settings.ApplicationPreferDarkTheme), ApplicationPreferDarkThemeHandler);
				settings.RemoveNotification(nameof(settings.ThemeName), ApplicationPreferDarkThemeHandler);
			}
		}

		private void ApplicationPreferDarkThemeHandler(object o, GLib.NotifyArgs args)
		{
			var settings = Settings.Default;
			if (args.Property == nameof(settings.ApplicationPreferDarkTheme) || IsGtkThemeDark(_previousThemeName) != IsGtkThemeDark(settings.ThemeName))
			{
				SystemThemeChanged?.Invoke(o, EventArgs.Empty);
			}
			_previousThemeName = settings.ThemeName;
		}

		private bool IsGtkThemeDark(string themeName)
		{
			return themeName.Contains("-dark", StringComparison.OrdinalIgnoreCase);
		}

		public SystemTheme GetSystemTheme()
		{
			if (OperatingSystem.IsWindows())
			{
				return GetWindowsTheme();
			}
			else
			{
				return (Settings.Default.ApplicationPreferDarkTheme || IsGtkThemeDark(Settings.Default.ThemeName)) ? SystemTheme.Dark : SystemTheme.Light;
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
