using System;
using Uno.Foundation.Logging;
using Tmds.DBus.Protocol;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Detects the active input method framework from environment variables
/// and creates the appropriate D-Bus IME client.
/// </summary>
internal static class X11InputMethodDetector
{
	/// <summary>
	/// Detect the active IME and create a D-Bus client for it.
	/// Returns null if no D-Bus IME is available (XIM fallback should be used).
	/// </summary>
	public static IX11InputMethod? DetectAndCreate()
	{
		var imName = DetectImeName();
		if (imName is null)
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(X11InputMethodDetector).Log().Debug("No D-Bus IME detected from environment variables. Using XIM fallback.");
			}
			return null;
		}

		if (imName == "none")
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(X11InputMethodDetector).Log().Debug("IME explicitly disabled via UNO_IM_MODULE=none. Using XIM fallback.");
			}
			return null;
		}

		// Check for D-Bus session bus availability
		var sessionBus = DBusAddress.Session;
		if (sessionBus is null)
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(X11InputMethodDetector).Log().Warn($"D-Bus IME '{imName}' detected but no D-Bus session bus available. Using XIM fallback.");
			}
			return null;
		}

		try
		{
			return imName switch
			{
				"ibus" => CreateIBus(sessionBus),
				"fcitx" or "fcitx5" => CreateFcitx(sessionBus),
				_ => null
			};
		}
		catch (Exception ex)
		{
			if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Error))
			{
				typeof(X11InputMethodDetector).Log().Error($"Failed to create D-Bus IME client for '{imName}': {ex.Message}", ex);
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

	private static IX11InputMethod CreateIBus(string sessionBus)
	{
		if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Information))
		{
			typeof(X11InputMethodDetector).Log().Info("Creating IBus D-Bus IME client.");
		}
		return new IBusInputMethod(sessionBus);
	}

	private static IX11InputMethod CreateFcitx(string sessionBus)
	{
		if (typeof(X11InputMethodDetector).Log().IsEnabled(LogLevel.Information))
		{
			typeof(X11InputMethodDetector).Log().Info("Creating Fcitx D-Bus IME client.");
		}
		return new FcitxInputMethod(sessionBus);
	}
}
