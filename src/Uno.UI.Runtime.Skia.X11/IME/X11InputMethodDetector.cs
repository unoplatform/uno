using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11.DBus;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Detects the active input method framework from environment variables and the
/// running D-Bus services, and creates the appropriate D-Bus IME client.
/// </summary>
internal static class X11InputMethodDetector
{
	private const string IBusServiceName = "org.freedesktop.portal.IBus";
	private const string Fcitx5ServiceName = "org.freedesktop.portal.Fcitx";
	private const string Fcitx4ServiceName = "org.fcitx.Fcitx";

	/// <summary>
	/// The detected and fully-connected D-Bus IME, or null if none is available.
	/// Populated by <see cref="DetectAsync"/>; consumers (e.g. <see cref="X11KeyboardInputSource"/>)
	/// read this synchronously after host initialization has completed.
	/// </summary>
	public static IX11InputMethod? DetectedInputMethod { get; private set; }

	/// <summary>
	/// Runs detection and stores the result in <see cref="DetectedInputMethod"/>.
	/// Awaited from <c>X11ApplicationHost.InitializeAsync</c> so detection completes
	/// before any window is created.
	/// </summary>
	public static async Task DetectAsync()
	{
		DetectedInputMethod = await DetectAndCreateAsyncImpl();
	}

	private static async Task<IX11InputMethod?> DetectAndCreateAsyncImpl()
	{
		var imName = DetectImeName();

		if (imName == "none")
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(X11InputMethodDetector).Log().Debug("IME explicitly disabled via UNO_IM_MODULE=none.");
			}
			return null;
		}

		// Check for D-Bus session bus availability
		var sessionBus = DBusAddress.Session;
		if (sessionBus is null)
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(X11InputMethodDetector).Log().Warn("No D-Bus session bus available — IME will be disabled.");
			}
			return null;
		}

		// Build a prioritized list of candidates. Env-var-detected IME is preferred,
		// but we still try the other family if its D-Bus service is the one actually
		// running (e.g., env vars point to fcitx but only ibus-daemon is active).
		var preferIBus = imName == "ibus";
		var preferFcitx = imName is "fcitx" or "fcitx5";

		var ibusAvailable = await ServiceHasOwnerAsync(sessionBus, IBusServiceName);
		var fcitxAvailable = await ServiceHasOwnerAsync(sessionBus, Fcitx5ServiceName)
			|| await ServiceHasOwnerAsync(sessionBus, Fcitx4ServiceName);

		if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(X11InputMethodDetector).Log().Debug(
				$"IME D-Bus probe: envHint='{imName ?? "(none)"}' ibusAvailable={ibusAvailable} fcitxAvailable={fcitxAvailable}");
		}

		try
		{
			// Hint matches a running service → use it.
			if (preferIBus && ibusAvailable)
			{
				return await CreateIBusAsync(sessionBus);
			}
			if (preferFcitx && fcitxAvailable)
			{
				return await CreateFcitxAsync(sessionBus);
			}

			// Hint doesn't match anything running — fall back to whichever is actually up.
			if (ibusAvailable)
			{
				if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Information))
				{
					typeof(X11InputMethodDetector).Log().Info(
						$"Env hint '{imName ?? "(none)"}' did not match a running D-Bus IME, falling back to IBus.");
				}
				return await CreateIBusAsync(sessionBus);
			}
			if (fcitxAvailable)
			{
				if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Information))
				{
					typeof(X11InputMethodDetector).Log().Info(
						$"Env hint '{imName ?? "(none)"}' did not match a running D-Bus IME, falling back to Fcitx.");
				}
				return await CreateFcitxAsync(sessionBus);
			}

			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(X11InputMethodDetector).Log().Debug("No D-Bus IME service is running — IME will be disabled.");
			}
			return null;
		}
		catch (Exception ex)
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Error))
			{
				typeof(X11InputMethodDetector).Log().Error($"Failed to create D-Bus IME client: {ex.Message}", ex);
			}
			return null;
		}
	}

	private static string? DetectImeName()
	{
		// Check env vars in priority order (matching Avalonia's approach)
		foreach (var envVar in new[] { "UNO_IM_MODULE", "GTK_IM_MODULE", "QT_IM_MODULE" })
		{
			var value = Environment.GetEnvironmentVariable(envVar);
			if (!string.IsNullOrEmpty(value))
			{
				if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(X11InputMethodDetector).Log().Debug($"IME detected from {envVar}={value}");
				}
				return value;
			}
		}

		// Parse XMODIFIERS for @im=<name> pattern
		var xmodifiers = Environment.GetEnvironmentVariable("XMODIFIERS");
		if (xmodifiers is not null && xmodifiers.Contains("@im="))
		{
			var imStart = xmodifiers.IndexOf("@im=", StringComparison.Ordinal) + 4;
			var imEnd = xmodifiers.IndexOf('@', imStart);
			var imName = imEnd == -1 ? xmodifiers[imStart..] : xmodifiers[imStart..imEnd];

			if (!string.IsNullOrEmpty(imName))
			{
				if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(X11InputMethodDetector).Log().Debug($"IME detected from XMODIFIERS: {imName}");
				}
				return imName;
			}
		}

		return null;
	}

	private static async Task<bool> ServiceHasOwnerAsync(string sessionBus, string serviceName)
	{
		try
		{
			using var connection = new DBusConnection(sessionBus);
			await connection.ConnectAsync();

			var service = new DBusService(connection, "org.freedesktop.DBus");
			var dbus = service.CreateDBus("/org/freedesktop/DBus");
			return await dbus.NameHasOwnerAsync(serviceName);
		}
		catch (Exception ex)
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(X11InputMethodDetector).Log().Debug($"NameHasOwner probe for '{serviceName}' failed: {ex.Message}");
			}
			return false;
		}
	}

	private static async Task<IX11InputMethod?> CreateIBusAsync(string sessionBus)
	{
		if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Information))
		{
			typeof(X11InputMethodDetector).Log().Info("Creating IBus D-Bus IME client.");
		}
		var ime = new IBusInputMethod(sessionBus);
		await ime.InitTask;
		if (ime.IsEnabled)
		{
			return ime;
		}
		ime.Dispose();
		return null;
	}

	private static async Task<IX11InputMethod?> CreateFcitxAsync(string sessionBus)
	{
		if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Information))
		{
			typeof(X11InputMethodDetector).Log().Info("Creating Fcitx D-Bus IME client.");
		}
		var ime = new FcitxInputMethod(sessionBus);
		await ime.InitTask;
		if (ime.IsEnabled)
		{
			return ime;
		}
		ime.Dispose();
		return null;
	}
}
