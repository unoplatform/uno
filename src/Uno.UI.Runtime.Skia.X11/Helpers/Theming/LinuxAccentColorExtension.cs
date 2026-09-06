using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;
using Uno.WinUI.Runtime.Skia.X11.DBus;
using Windows.UI;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Settings.html

internal class LinuxAccentColorExtension : IAccentColorExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";
	private const string AppearanceNamespace = "org.freedesktop.appearance";
	private const string AccentColorKey = "accent-color";
	private const string NotFoundErrorName = "org.freedesktop.portal.Error.NotFound";

	public static LinuxAccentColorExtension Instance { get; } = new();

	public event EventHandler? AccentColorChanged;

	private AccentColorPalette? _currentPalette;

	private LinuxAccentColorExtension()
	{
		_ = Init();
	}

	// Null (default palette) until the asynchronous portal read completes; the OS value then arrives through
	// AccentColorChanged. Init continues off the UI thread and the setter marshals to the dispatcher, so the
	// first write always lands after the startup frame that created this instance has subscribed.
	public AccentColorPalette? GetAccentColorPalette() => _currentPalette;

	private AccentColorPalette? CurrentPalette
	{
		get => _currentPalette;
		set
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				// The shades are derived from the accent, so comparing Accent is enough to detect a change.
				if (_currentPalette?.Accent != value?.Accent)
				{
					_currentPalette = value;
					AccentColorChanged?.Invoke(this, EventArgs.Empty);
				}
			}
			else
			{
				NativeDispatcher.Main.Enqueue(() => CurrentPalette = value);
			}
		}
	}

	private async Task Init()
	{
		try
		{
			var sessionsAddressBus = DBusAddress.Session;
			if (sessionsAddressBus is null)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Unable to observe the system accent color, the default accent will be used. (Unable to determine the DBus session bus address)");
				}
				return;
			}

			var connection = new DBusConnection(sessionsAddressBus);
			await connection.ConnectAsync().ConfigureAwait(false);

			var desktopService = new DBusService(connection, Service);
			var settings = desktopService.CreateSettings(ObjectPath);

			// ReadOne and the accent-color key are version 2 additions; later versions are additive.
			var version = await settings.GetVersionAsync().ConfigureAwait(false);
			if (version < 2)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"System accent color detection requires version 2 of the Settings portal, but version {version} was found. The default accent will be used.");
				}
				return;
			}

			// Watch before the initial read so that neither a change racing the read nor a failed read can leave
			// the accent stale for the lifetime of the app. The IDisposable is ignored: the watch lives as long as the app.
			await settings.WatchSettingChangedAsync((exception, tuple) =>
			{
				try
				{
					if (exception is not null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error("Failed to observe desktop portal setting changes; the accent color will no longer follow the OS.", exception);
						}
						return;
					}

					if (tuple is { Namespace: AppearanceNamespace, Key: AccentColorKey })
					{
						CurrentPalette = ToPalette(tuple.Value);
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("Unable to process a desktop portal accent color change.", e);
					}
				}
			}).ConfigureAwait(false);

			try
			{
				var result = await settings.ReadOneAsync(AppearanceNamespace, AccentColorKey).ConfigureAwait(false);
				CurrentPalette = ToPalette(result);
			}
			catch (DBusErrorReplyException e) when (e.ErrorName == NotFoundErrorName)
			{
				// Portal backends may implement version 2 without the optional accent-color key.
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("The desktop portal does not expose the optional accent-color setting.");
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Unable to read the initial accent color from the desktop portal; the default accent will be used until it changes.", e);
				}
			}
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Unable to observe the system accent color. (DBus Settings error, see https://aka.platform.uno/x11-dbus-troubleshoot for troubleshooting information)", e);
			}
		}
	}

	// accent-color is a (ddd) struct of sRGB components in [0,1]; per the portal spec, out-of-range
	// components mean that no accent color is set.
	private AccentColorPalette? ToPalette(VariantValue value)
	{
		if (value.Type != VariantValueType.Struct || value.Count != 3 ||
			value.GetItem(0).Type != VariantValueType.Double ||
			value.GetItem(1).Type != VariantValueType.Double ||
			value.GetItem(2).Type != VariantValueType.Double)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Unexpected accent-color value from the desktop portal ({value.Type} with {value.Count} item(s)); expected a (ddd) struct.");
			}
			return null;
		}

		var r = value.GetItem(0).GetDouble();
		var g = value.GetItem(1).GetDouble();
		var b = value.GetItem(2).GetDouble();

		if (!IsInRange(r) || !IsInRange(g) || !IsInRange(b))
		{
			return null;
		}

		return AccentColorPalette.FromAccentColor(Color.FromArgb(0xFF, ToByte(r), ToByte(g), ToByte(b)));

		static bool IsInRange(double component) => component is >= 0 and <= 1;

		static byte ToByte(double component) => (byte)Math.Round(component * 255);
	}
}
