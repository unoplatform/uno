using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11.DBus;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.X11;

// Reads GNOME text-scaling-factor via freedesktop Settings portal
internal class X11TextScaleFactorExtension : ITextScaleFactorExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";

	public event EventHandler? TextScaleFactorChanged;

	private double _currentScale = 1.0;

	public double GetTextScaleFactor() => _currentScale;

	public static X11TextScaleFactorExtension Instance { get; } = new();

	private X11TextScaleFactorExtension()
	{
		_ = ReadInitialScale().ConfigureAwait(false);
	}

	private async Task ReadInitialScale()
	{
		try
		{
			var sessionsAddressBus = DBusAddress.Session;
			if (sessionsAddressBus is null)
			{
				return;
			}

			var connection = new DBusConnection(sessionsAddressBus);
			await connection.ConnectAsync();

			var desktopService = new DBusService(connection, Service);
			var settings = desktopService.CreateSettings(ObjectPath);

			var version = await settings.GetVersionAsync();
			if (version != 2)
			{
				return;
			}

			var result = await settings.ReadOneAsync("org.gnome.desktop.interface", "text-scaling-factor");
			_currentScale = result.GetDouble();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Unable to read text-scaling-factor via DBus.", e);
			}
		}
	}

	internal void RaiseTextScaleFactorChanged()
	{
		TextScaleFactorChanged?.Invoke(this, EventArgs.Empty);
	}
}
