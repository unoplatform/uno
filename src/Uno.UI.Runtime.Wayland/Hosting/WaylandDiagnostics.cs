#nullable enable

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Wayland;

// Window lifecycle and protocol trace, enabled through the Debug log level of this category.
internal static class WaylandDiagnostics
{
	internal static bool IsEnabled => typeof(WaylandDiagnostics).Log().IsEnabled(LogLevel.Debug);

	// Input contents (key codes) are keystrokes, so they only appear at Trace.
	internal static bool IsVerbose => typeof(WaylandDiagnostics).Log().IsEnabled(LogLevel.Trace);

	internal static void Trace(string message)
	{
		if (IsEnabled)
		{
			typeof(WaylandDiagnostics).Log().Debug(message);
		}
	}

	internal static void TraceVerbose(string message)
	{
		if (IsVerbose)
		{
			typeof(WaylandDiagnostics).Log().Trace(message);
		}
	}
}
