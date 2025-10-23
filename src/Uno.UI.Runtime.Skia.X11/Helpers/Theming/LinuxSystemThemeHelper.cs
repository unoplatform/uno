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

	private SystemTheme _currentTheme = SystemTheme.Light;
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

	public SystemTheme GetSystemTheme() => CurrentTheme;

	public static LinuxSystemThemeHelper Instance { get; } = new();

	private LinuxSystemThemeHelper()
	{
		_ = Init().ConfigureAwait(false);
	}

	public async Task Init()
	{
		try
		{
			var sessionsAddressBus = Address.Session;
			if (sessionsAddressBus is null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Unable to observe the system theme. (Unable to determine the DBus session bus address)");
				}
				return;
			}

			var connection = new Connection(sessionsAddressBus);
			await connection.ConnectAsync();

			var desktopService = new DesktopService(connection, Service);
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
