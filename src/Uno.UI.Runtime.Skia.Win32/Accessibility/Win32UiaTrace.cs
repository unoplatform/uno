#nullable enable

using System;
using System.IO;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Self-contained file-based trace for the Win32 UIA provider layer. Enabled when
/// the environment variable <c>UNO_UIA_TRACE</c> is set to a writable file path —
/// works without any <see cref="Microsoft.Extensions.Logging"/> wiring so it can be
/// turned on in customer apps that don't configure a logger factory.
///
/// Usage:
///   1. Close the app.
///   2. set UNO_UIA_TRACE=C:\temp\uno-uia-trace.log   (cmd.exe)
///      $env:UNO_UIA_TRACE = 'C:\temp\uno-uia-trace.log'  (PowerShell)
///   3. Launch the app, reproduce the issue.
///   4. Close the app, send the trace file.
/// </summary>
internal static class Win32UiaTrace
{
	private static readonly object _gate = new();
	private static readonly string? _path;
	private static readonly bool _enabled;

	static Win32UiaTrace()
	{
		var path = Environment.GetEnvironmentVariable("UNO_UIA_TRACE");
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		try
		{
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			_path = path;
			_enabled = true;
			Append($"=== Win32UiaTrace started {DateTime.Now:o} pid={Environment.ProcessId} ===");
		}
		catch
		{
			_enabled = false;
		}
	}

	internal static bool IsEnabled => _enabled;

	internal static void Write(string message)
	{
		if (!_enabled)
		{
			return;
		}
		Append(message);
	}

	private static void Append(string line)
	{
		if (_path is null)
		{
			return;
		}
		try
		{
			lock (_gate)
			{
				File.AppendAllText(_path, $"{DateTime.Now:HH:mm:ss.fff} t{Environment.CurrentManagedThreadId,-3} {line}{Environment.NewLine}");
			}
		}
		catch
		{
			// Tracing must never throw.
		}
	}
}
