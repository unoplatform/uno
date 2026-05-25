using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.Host;

/// <seealso href="ambient-registry.md"/>
public partial class AmbientRegistry(ILogger logger)
{
	private static readonly string RegistryDirectory =
		Path.Combine(ResolveLocalApplicationDataPath(), "Uno Platform", "DevServers");

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true
	};

	private string? _registryFilePath;
	private readonly ILogger _logger = logger;

	/// <summary>
	/// Returns an absolute path to the local application data directory.
	/// On Linux, <see cref="Environment.SpecialFolder.LocalApplicationData"/>
	/// returns an empty string when <c>XDG_DATA_HOME</c> points outside
	/// <c>$HOME/.local/share</c>. This method falls back to
	/// <c>XDG_DATA_HOME</c>, then <c>$HOME/.local/share</c>, to ensure a
	/// stable absolute path regardless of the environment configuration.
	/// </summary>
	internal static string ResolveLocalApplicationDataPath()
	{
		var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		// .NET returns empty when XDG_DATA_HOME is set to a non-standard path.
		path = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
		if (!string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path))
		{
			return path;
		}

		// Last resort: use the conventional Linux default.
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.Combine(home, ".local", "share");
	}

	/// <summary>
	/// Registers the current development server process in the local registry for discovery and management purposes.
	/// </summary>
	/// <remarks>This method creates or updates a registration entry for the current process, allowing external
	/// tools or processes to discover and interact with the running development server instance. The registration is
	/// stored as a JSON file in a predefined registry directory. If registration fails, a warning is logged to the
	/// console.</remarks>
	/// <param name="solution">The full path to the solution file associated with the development server, or null if not applicable.</param>
	/// <param name="ppid">The process ID of the parent process that launched the development server.</param>
	/// <param name="httpPort">The HTTP port number on which the development server is listening.</param>
	internal void Register(string? solution, int ppid, int httpPort, string? ideChannelId = null)
	{
		try
		{
			// Ensure the registry directory exists
			Directory.CreateDirectory(RegistryDirectory);

			// Create a unique filename based on process ID
			var currentProcessId = Environment.ProcessId;
			var fileName = $"devserver-{currentProcessId}.json";

			_registryFilePath = Path.Combine(RegistryDirectory, fileName);

			// Create registration data
			var registrationData = new DevServerRegistration
			{
				ProcessId = currentProcessId,
				ParentProcessId = ppid,
				SolutionPath = solution,
				StartTime = DateTime.UtcNow,
				Port = httpPort,
				MachineName = Environment.MachineName,
				UserName = Environment.UserName,
				IdeChannelId = ideChannelId
			};

			// Serialize and write to file
			var json = JsonSerializer.Serialize(registrationData, JsonOptions);

			File.WriteAllText(_registryFilePath, json);

			_logger.LogDebug($"DevServer registered: {_registryFilePath}");
		}
		catch (Exception ex)
		{
			_logger.LogWarning($"Failed to register DevServer: {ex.Message}");
		}
	}

	/// <summary>
	/// Removes the DevServer registration by deleting the associated registry file, if it exists.
	/// </summary>
	/// <remarks>If the registry file does not exist, this method performs no action. Any exceptions encountered
	/// during the operation are caught and logged as warnings to the console.</remarks>
	internal void Unregister()
	{
		try
		{
			if (_registryFilePath != null && File.Exists(_registryFilePath))
			{
				File.Delete(_registryFilePath);
				_logger.LogDebug($"DevServer unregistered: {_registryFilePath}");
			}

			CleanupStaleRegistrations();
		}
		catch (Exception ex)
		{
			_logger.LogWarning($"Failed to unregister DevServer: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets all currently registered dev servers
	/// </summary>
	public IEnumerable<DevServerRegistration> GetActiveDevServers()
	{
		var activeServers = new List<DevServerRegistration>();

		try
		{
			if (!Directory.Exists(RegistryDirectory))
			{
				return activeServers;
			}

			// Primary registrations: written by the DevServer host process at startup.
			// Sidecars (`devserver-{pid}.aux.json`) are written by the CLI when it spawns a host
			// to backfill metadata that the host itself didn't record (notably ideChannelId on
			// Uno.WinUI.DevServer versions older than the commit that introduced IdeChannelId
			// in the registration record). The `.aux` suffix excludes them from the primary glob.
			var registryFiles = Directory.GetFiles(RegistryDirectory, "devserver-*.json")
				.Where(f => !f.EndsWith(".aux.json", StringComparison.OrdinalIgnoreCase))
				.ToArray();

			foreach (var filePath in registryFiles)
			{
				try
				{
					var json = File.ReadAllText(filePath);
					var registration = JsonSerializer.Deserialize<DevServerRegistration>(json);

					if (registration != null)
					{
						// Check if the process is still running
						if (IsProcessRunning(registration.ProcessId))
						{
							OverlayAuxiliaryRegistration(registration);
							activeServers.Add(registration);
						}
						else
						{
							// Clean up stale registration file
							File.Delete(filePath);
							TryDeleteSidecar(registration.ProcessId);
							_logger.LogDebug($"Cleaned up stale registration: {filePath}");
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogDebug($"Warning: Failed to read registration file {filePath}: {ex.Message}");

					// Try to clean up corrupted file
					try
					{
						File.Delete(filePath);
					}
					catch
					{
						// Ignore cleanup errors
					}
				}
			}

			// Also clean up orphan sidecars whose target process is gone — this handles the
			// case where the CLI wrote a sidecar but the host crashed before registering, or
			// the primary file was already cleaned up in a previous pass.
			CleanupOrphanSidecars();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"Warning: Failed to enumerate dev server registrations: {ex.Message}");
		}

		return activeServers;
	}

	/// <summary>
	/// Returns the active DevServerRegistration for the provided solution path if any; otherwise null.
	/// </summary>
	public DevServerRegistration? GetActiveDevServerForPath(string? solution)
	{
		if (string.IsNullOrWhiteSpace(solution))
		{
			return null;
		}

		try
		{
			var solutionFull = Path.GetFullPath(solution);
			return GetActiveDevServers().FirstOrDefault(s =>
				!string.IsNullOrWhiteSpace(s.SolutionPath) &&
				Path.GetFullPath(s.SolutionPath).Equals(solutionFull, StringComparison.OrdinalIgnoreCase) &&
				s.MachineName == Environment.MachineName &&
				s.UserName == Environment.UserName);
		}
		catch (Exception e)
		{
			_logger.LogDebug($"Failed to read active servers. {e}");

			return null;
		}
	}

	/// <summary>
	/// Returns the active DevServerRegistration for the provided port if any; otherwise null.
	/// </summary>
	public DevServerRegistration? GetActiveDevServerForPort(int port)
	{
		if (port <= 0)
		{
			return null;
		}

		return GetActiveDevServers().FirstOrDefault(s => s.Port == port && s.MachineName == Environment.MachineName && s.UserName == Environment.UserName);
	}

	/// <summary>
	/// Cleans up stale registration files for processes that are no longer running
	/// </summary>
	public void CleanupStaleRegistrations()
	{
		// Reuse the active-servers enumeration so stale files are pruned by the
		// same logic that normal discovery already uses.
		_ = GetActiveDevServers();
	}

	private bool IsProcessRunning(int processId)
	{
		try
		{
			var process = Process.GetProcessById(processId);
			return !process.HasExited;
		}
		catch
		{
			// Process doesn't exist or can't be accessed
			return false;
		}
	}

	public class DevServerRegistration
	{
		public int ProcessId { get; set; }
		public int ParentProcessId { get; set; }
		public string? SolutionPath { get; set; }
		public DateTime StartTime { get; set; }
		public int Port { get; set; }
		public string MachineName { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public string? IdeChannelId { get; set; }
	}

	/// <summary>
	/// Sidecar payload written by the CLI to backfill metadata the host itself didn't record.
	/// See <see cref="WriteAuxiliaryRegistration"/> for context. Kept minimal — only fields the
	/// CLI knows that the host might not (currently just <see cref="IdeChannelId"/>).
	/// </summary>
	internal sealed class AuxiliaryRegistration
	{
		public int ProcessId { get; set; }
		public string? IdeChannelId { get; set; }
	}

	/// <summary>
	/// Writes a sidecar registration record at <c>devserver-{targetProcessId}.aux.json</c>
	/// alongside the primary host registration. Used by the CLI when it spawns a host to
	/// expose metadata that older host versions don't register themselves — primarily
	/// <paramref name="ideChannelId"/>, which uno.studio's DevServerLauncher needs to
	/// connect directly to a running host without re-spawning a duplicate.
	/// </summary>
	/// <remarks>
	/// Calling with a null/empty <paramref name="ideChannelId"/> is a no-op. The sidecar is
	/// removed by <see cref="GetActiveDevServers"/> when its target process is no longer alive.
	/// </remarks>
	public void WriteAuxiliaryRegistration(int targetProcessId, string? ideChannelId)
	{
		if (string.IsNullOrWhiteSpace(ideChannelId))
		{
			return;
		}

		try
		{
			Directory.CreateDirectory(RegistryDirectory);
			var sidecarPath = GetSidecarPath(targetProcessId);
			var payload = new AuxiliaryRegistration
			{
				ProcessId = targetProcessId,
				IdeChannelId = ideChannelId,
			};
			File.WriteAllText(sidecarPath, JsonSerializer.Serialize(payload, JsonOptions));
			_logger.LogDebug("Wrote auxiliary DevServer registration for PID {Pid}: {Path}", targetProcessId, sidecarPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to write auxiliary DevServer registration for PID {Pid}.", targetProcessId);
		}
	}

	private static string GetSidecarPath(int processId)
		=> Path.Combine(RegistryDirectory, $"devserver-{processId}.aux.json");

	private void OverlayAuxiliaryRegistration(DevServerRegistration registration)
	{
		// The host's own values are authoritative — the sidecar is only a backfill for fields
		// the host left null. Only IdeChannelId today; if other fields end up needing the same
		// treatment, prefer adding them here over duplicating the file/path/parse plumbing.
		if (!string.IsNullOrWhiteSpace(registration.IdeChannelId))
		{
			return;
		}

		var sidecarPath = GetSidecarPath(registration.ProcessId);
		if (!File.Exists(sidecarPath))
		{
			return;
		}

		try
		{
			var json = File.ReadAllText(sidecarPath);
			var aux = JsonSerializer.Deserialize<AuxiliaryRegistration>(json);
			if (!string.IsNullOrWhiteSpace(aux?.IdeChannelId))
			{
				registration.IdeChannelId = aux!.IdeChannelId;
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug("Failed to overlay auxiliary registration {Path}: {Message}", sidecarPath, ex.Message);
		}
	}

	private void CleanupOrphanSidecars()
	{
		try
		{
			var sidecarFiles = Directory.GetFiles(RegistryDirectory, "devserver-*.aux.json");
			foreach (var sidecarPath in sidecarFiles)
			{
				var fileName = Path.GetFileNameWithoutExtension(sidecarPath); // "devserver-{pid}.aux"
				var pidPart = fileName.Length > "devserver-".Length + ".aux".Length
					? fileName.Substring("devserver-".Length, fileName.Length - "devserver-".Length - ".aux".Length)
					: null;

				if (!int.TryParse(pidPart, out var pid) || !IsProcessRunning(pid))
				{
					try { File.Delete(sidecarPath); } catch { /* best effort */ }
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug("Failed to enumerate auxiliary registrations: {Message}", ex.Message);
		}
	}

	private void TryDeleteSidecar(int processId)
	{
		try
		{
			var sidecarPath = GetSidecarPath(processId);
			if (File.Exists(sidecarPath))
			{
				File.Delete(sidecarPath);
			}
		}
		catch
		{
			// Best effort
		}
	}
}
