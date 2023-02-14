using System;
using Microsoft.Win32;
using Uno.Helpers.Theming;
using Uno.UI.Runtime.Skia.Wpf.WPF.Extensions.Helper.Theming;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf
{
	public class WpfApplicationExtension : IApplicationExtension, IDisposable
	{
		private readonly Application _owner;
		private WpfSystemThemeHelperExtension _themeHelper;
		private SystemTheme _currentTheme;

		private bool _disposedValue;

		public WpfApplicationExtension(Application owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));

			_themeHelper = new WpfSystemThemeHelperExtension(null);
			_currentTheme = _themeHelper.GetSystemTheme();

			SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
		}

		public event EventHandler SystemThemeChanged;

		private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			// This event is raised when the theme is changed,
			// but also for various other irrelevant reasons.
			// Check the registry first, before forcing the application
			// to refresh everything.
			var newTheme = _themeHelper.GetSystemTheme();

			if (newTheme != _currentTheme)
			{
				_currentTheme = newTheme;
				SystemThemeChanged?.Invoke(this, EventArgs.Empty);
			}
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

		~WpfApplicationExtension()
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
