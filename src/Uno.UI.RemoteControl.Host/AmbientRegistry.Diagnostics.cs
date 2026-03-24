using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.Host;

public partial class AmbientRegistry
{
	/// <summary>
	/// Updates the currently registered IDE channel without changing the rest
	/// of the registration payload.
	/// </summary>
	internal void UpdateIdeChannel(string? ideChannelId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_registryFilePath) || !File.Exists(_registryFilePath))
			{
				return;
			}

			var json = File.ReadAllText(_registryFilePath);
			var registration = JsonSerializer.Deserialize<DevServerRegistration>(json);
			if (registration is null)
			{
				return;
			}

			registration.IdeChannelId = ideChannelId;

			// Atomic write: unique temp file + rename to prevent partial reads
			// and avoid races when multiple rebinds target the same registration.
			var tempPath = _registryFilePath + $".{Guid.NewGuid():N}.tmp";
			try
			{
				File.WriteAllText(tempPath, JsonSerializer.Serialize(registration, JsonOptions));
				File.Move(tempPath, _registryFilePath, overwrite: true);
			}
			finally
			{
				// Clean up the temp file if rename failed.
				try { File.Delete(tempPath); } catch { /* best effort */ }
			}

			_logger.LogDebug("Updated DevServer IDE channel registration: {IdeChannelId}", ideChannelId ?? "<null>");
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to update DevServer IDE channel registration.");
		}
	}

	/// <summary>
	/// Builds a bounded ancestry chain for diagnostics.
	/// </summary>
	public IReadOnlyList<ProcessChainNode> GetProcessChain(DevServerRegistration registration, int maxDepth = 8)
	{
		var chain = new List<ProcessChainNode>();
		var visited = new HashSet<int>();
		var currentProcessId = registration.ProcessId;
		int? nextParentProcessId = registration.ParentProcessId > 0 ? registration.ParentProcessId : null;

		for (var depth = 0; depth < maxDepth && currentProcessId > 0 && visited.Add(currentProcessId); depth++)
		{
			chain.Add(new ProcessChainNode
			{
				ProcessId = currentProcessId,
				ProcessName = TryGetProcessName(currentProcessId),
			});

			currentProcessId = nextParentProcessId ?? 0;
			nextParentProcessId = currentProcessId > 0 ? TryGetParentProcessId(currentProcessId) : null;
		}

		return chain;
	}

	private static string? TryGetProcessName(int processId)
	{
		try
		{
			using var process = Process.GetProcessById(processId);
			return process.HasExited ? null : process.ProcessName;
		}
		catch
		{
			return null;
		}
	}

	private static int? TryGetParentProcessId(int processId)
	{
		try
		{
			if (OperatingSystem.IsWindows())
			{
				return TryGetWindowsParentProcessId(processId);
			}

			if (OperatingSystem.IsLinux())
			{
				return TryGetLinuxParentProcessId(processId);
			}

			if (OperatingSystem.IsMacOS())
			{
				return TryGetUnixParentProcessId(processId);
			}
		}
		catch
		{
			// Best-effort diagnostics only.
		}

		return null;
	}

	private static int? TryGetLinuxParentProcessId(int processId)
	{
		var statPath = $"/proc/{processId}/stat";
		if (!File.Exists(statPath))
		{
			return null;
		}

		var content = File.ReadAllText(statPath);
		var closeParen = content.LastIndexOf(')');
		if (closeParen < 0 || closeParen + 4 >= content.Length)
		{
			return null;
		}

		var remainder = content[(closeParen + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return remainder.Length < 2 || !int.TryParse(remainder[1], out var parentProcessId)
			? null
			: parentProcessId;
	}

	private static int? TryGetUnixParentProcessId(int processId)
	{
		var psi = new ProcessStartInfo
		{
			FileName = "ps",
			Arguments = $"-o ppid= -p {processId}",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		using var process = Process.Start(psi);
		if (process is null)
		{
			return null;
		}

		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		return process.ExitCode == 0 && int.TryParse(output.Trim(), out var parentProcessId)
			? parentProcessId
			: null;
	}

	private static int? TryGetWindowsParentProcessId(int processId)
	{
		var processHandle = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId);
		if (processHandle == IntPtr.Zero)
		{
			return null;
		}

		try
		{
			var basicInformation = new ProcessBasicInformation();
			var result = NtQueryInformationProcess(
				processHandle,
				processInformationClass: 0,
				ref basicInformation,
				Marshal.SizeOf<ProcessBasicInformation>(),
				out _);

			return result == 0
				? basicInformation.InheritedFromUniqueProcessId.ToInt32()
				: null;
		}
		finally
		{
			_ = CloseHandle(processHandle);
		}
	}

	public class ProcessChainNode
	{
		public int ProcessId { get; set; }

		public string? ProcessName { get; set; }
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct ProcessBasicInformation
	{
		public IntPtr Reserved1;
		public IntPtr PebBaseAddress;
		public IntPtr Reserved2_0;
		public IntPtr Reserved2_1;
		public IntPtr UniqueProcessId;
		public IntPtr InheritedFromUniqueProcessId;
	}

	[Flags]
	private enum ProcessAccessFlags : uint
	{
		QueryLimitedInformation = 0x1000,
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("ntdll.dll")]
	private static extern int NtQueryInformationProcess(
		IntPtr processHandle,
		int processInformationClass,
		ref ProcessBasicInformation processInformation,
		int processInformationLength,
		out int returnLength);
}
