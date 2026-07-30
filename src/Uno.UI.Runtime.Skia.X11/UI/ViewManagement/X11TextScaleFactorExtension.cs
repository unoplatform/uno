using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.WinUI.Runtime.Skia.X11.DBus;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.X11;

// Reads GNOME text-scaling-factor via freedesktop Settings portal
internal class X11TextScaleFactorExtension : ITextScaleFactorExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";

	public event EventHandler? TextScaleFactorChanged;

	private DBusConnection? _connection;
	private readonly object _gate = new();
	private double _currentScale = 1.0;
	private double CurrentScale
	{
		get
		{
			lock (_gate)
			{
				return _currentScale;
			}
		}
		set
		{
			SetCurrentScale(value);
		}
	}

	public double GetTextScaleFactor() => CurrentScale;

	public static X11TextScaleFactorExtension Instance { get; } = new();

	private X11TextScaleFactorExtension()
	{
		_ = Initialize().ConfigureAwait(false);
	}

	private void SetCurrentScale(double value)
	{
		if (!NativeDispatcher.Main.HasThreadAccess)
		{
			NativeDispatcher.Main.Enqueue(() => SetCurrentScale(value));
			return;
		}

		var shouldRaise = false;
		lock (_gate)
		{
			if (!value.Equals(_currentScale))
			{
				_currentScale = value;
				shouldRaise = true;
			}
		}

		if (shouldRaise)
		{
			TextScaleFactorChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private async Task Initialize()
	{
		try
		{
			var sessionsAddressBus = DBusAddress.Session;
			if (sessionsAddressBus is null)
			{
				return;
			}

			_connection = new DBusConnection(sessionsAddressBus);
			await _connection.ConnectAsync();

			var desktopService = new DBusService(_connection, Service);
			var settings = desktopService.CreateSettings(ObjectPath);

			var version = await settings.GetVersionAsync();
			if (version != 2)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Text scale detection is only implemented for version 2 of the Settings portal, but version {version} was found");
				}
				return;
			}

			var result = await settings.ReadOneAsync("org.gnome.desktop.interface", "text-scaling-factor");
			CurrentScale = result.GetDouble();

			// Intentionally keep the connection/watch alive for the lifetime of the app.
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

				if (tuple is { Namespace: "org.gnome.desktop.interface", Key: "text-scaling-factor" })
				{
					CurrentScale = tuple.Value.GetDouble();
				}
			});
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Unable to observe text-scaling-factor via DBus.", e);
			}
		}
	}
}
