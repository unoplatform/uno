using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;
using Uno.WinUI.Runtime.Skia.X11.DBus;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Settings.html

internal class LinuxSystemThemeHelper : ISystemThemeHelperExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";

	public event EventHandler? SystemThemeChanged;
	public event EventHandler? HighContrastChanged;

	private SystemTheme _currentTheme = SystemTheme.Light;
	private bool _currentHighContrast;

	private SystemTheme CurrentTheme
	{
		get => _currentTheme;
		set
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				if (_currentTheme != value)
				{
					_currentTheme = value;
					SystemThemeChanged?.Invoke(this, EventArgs.Empty);
				}
			}
			else
			{
				NativeDispatcher.Main.Enqueue(() => CurrentTheme = value);
			}
		}
	}

	private bool CurrentHighContrast
	{
		get => _currentHighContrast;
		set
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				if (_currentHighContrast != value)
				{
					_currentHighContrast = value;
					HighContrastChanged?.Invoke(this, EventArgs.Empty);
				}
			}
			else
			{
				NativeDispatcher.Main.Enqueue(() => CurrentHighContrast = value);
			}
		}
	}

	public SystemTheme GetSystemTheme() => CurrentTheme;
	public bool IsHighContrastEnabled() => CurrentHighContrast;
	public string GetHighContrastSchemeName() => "High Contrast Black";

	public static LinuxSystemThemeHelper Instance { get; } = new();

	private LinuxSystemThemeHelper()
	{
		_ = Init().ConfigureAwait(false);
	}

	public async Task Init()
	{
		try
		{
			var sessionsAddressBus = DBusAddress.Session;
			if (sessionsAddressBus is null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Unable to observe the system theme. (Unable to determine the DBus session bus address)");
				}
				return;
			}

			var connection = new DBusConnection(sessionsAddressBus);
			await connection.ConnectAsync();

			var desktopService = new DBusService(connection, Service);
			var settings = desktopService.CreateSettings(ObjectPath);

			var version = await settings.GetVersionAsync();
			if (version != 2)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"System theme detection is only implemented for version 2 of the Settings portal, but version {version} was found");
				}
				return;
			}

			var result = await settings.ReadOneAsync("org.freedesktop.appearance", "color-scheme");
			CurrentTheme = result.GetUInt32() == 1 ? SystemTheme.Dark : SystemTheme.Light;

			// Try to read contrast setting (freedesktop portal v2)
			try
			{
				var contrastResult = await settings.ReadOneAsync("org.freedesktop.appearance", "contrast");
				_currentHighContrast = contrastResult.GetUInt32() == 1;
			}
			catch
			{
				// Not all desktops support the contrast setting.
				// Fall back to checking gtk-theme-name for "HighContrast" pattern.
				try
				{
					var themeResult = await settings.ReadOneAsync("org.gnome.desktop.interface", "gtk-theme");
					var themeName = themeResult.GetString();
					_currentHighContrast = themeName?.Contains("HighContrast", StringComparison.OrdinalIgnoreCase) == true ||
											themeName?.Contains("high-contrast", StringComparison.OrdinalIgnoreCase) == true;
				}
				catch
				{
					// Neither approach worked — HC not available
					_currentHighContrast = false;
				}
			}

			// ignoring IDisposable return value here since we're watching for the lifetime of the app
			await settings.WatchSettingChangedAsync((exception, tuple) =>
			{
				if (exception is not null)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(settings.WatchSettingChangedAsync)} threw an exception", exception);
					}
					return;
				}

				if (tuple is { Namespace: "org.freedesktop.appearance", Key: "color-scheme" })
				{
					CurrentTheme = tuple.Value.GetUInt32() == 1 ? SystemTheme.Dark : SystemTheme.Light;
				}
				else if (tuple is { Namespace: "org.freedesktop.appearance", Key: "contrast" })
				{
					CurrentHighContrast = tuple.Value.GetUInt32() == 1;
				}
			});
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unable to observe the system theme. (DBus Settings error, see https://aka.platform.uno/x11-dbus-troubleshoot for troubleshooting information)", e);
			}
		}
	}
}

