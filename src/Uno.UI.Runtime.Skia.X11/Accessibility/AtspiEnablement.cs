#nullable enable

using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Tracks the desktop's <c>org.a11y.Status</c> (IsEnabled / ScreenReaderEnabled), the
/// switch toolkits use to decide whether to run their AT-SPI bridge. Starting the bridge
/// only while it is set keeps apps from mirroring their UI onto the bus when no assistive
/// technology is running, and lets it come up when a screen reader starts later.
/// </summary>
internal static class AtspiEnablement
{
	private const string BusService = "org.a11y.Bus";
	private const string BusPath = "/org/a11y/bus";
	private const string StatusInterface = "org.a11y.Status";

	private static readonly object _gate = new();
	private static Task? _initTask;
	private static volatile bool _isEnabled;

	/// <summary>Raised on a D-Bus thread whenever <see cref="IsEnabled"/> may have changed.</summary>
	public static event EventHandler? Changed;

	public static bool IsEnabled => _isEnabled;

	public static Task EnsureInitializedAsync()
	{
		lock (_gate)
		{
			return _initTask ??= InitializeAsync();
		}
	}

	private static async Task InitializeAsync()
	{
		// GTK's opt-out; honoured so the bridge can be disabled without touching the desktop setting.
		if (Environment.GetEnvironmentVariable("NO_AT_BRIDGE") == "1")
		{
			return;
		}

		if (DBusAddress.Session is not { } sessionBusAddress)
		{
			return;
		}

		try
		{
			// Kept open for the lifetime of the process to receive status changes.
			var connection = new DBusConnection(sessionBusAddress);
			await connection.ConnectAsync();

			await connection.WatchPropertiesChangedAsync(
				BusService,
				BusPath,
				StatusInterface,
				static (_, _) => true,
				(exception, changed) =>
				{
					if (exception is null)
					{
						_ = RefreshAsync(connection);
					}
				},
				readerState: null,
				emitOnCapturedContext: false,
				flags: ObserverFlags.None);

			await RefreshAsync(connection);
		}
		catch (Exception ex)
		{
			if (typeof(AtspiEnablement).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AtspiEnablement).Log().Debug($"Unable to read org.a11y.Status; the AT-SPI bridge stays disabled: {ex.Message}");
			}
		}
	}

	private static async Task RefreshAsync(DBusConnection connection)
	{
		try
		{
			var enabled = await GetBoolAsync(connection, "IsEnabled") || await GetBoolAsync(connection, "ScreenReaderEnabled");
			if (enabled != _isEnabled)
			{
				_isEnabled = enabled;

				if (typeof(AtspiEnablement).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(AtspiEnablement).Log().Debug($"AT-SPI accessibility is now {(enabled ? "enabled" : "disabled")}.");
				}

				Changed?.Invoke(null, EventArgs.Empty);
			}
		}
		catch (Exception ex)
		{
			if (typeof(AtspiEnablement).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AtspiEnablement).Log().Debug($"Unable to refresh org.a11y.Status: {ex.Message}");
			}
		}
	}

	private static Task<bool> GetBoolAsync(DBusConnection connection, string property)
	{
		var writer = connection.GetMessageWriter();
		writer.WriteMethodCallHeader(BusService, BusPath, "org.freedesktop.DBus.Properties", "Get", "ss", MessageFlags.None);
		writer.WriteString(StatusInterface);
		writer.WriteString(property);
		return connection.CallMethodAsync(
			writer.CreateMessage(),
			static (reply, _) => reply.GetBodyReader().ReadVariantValue().GetBool(),
			null);
	}
}
