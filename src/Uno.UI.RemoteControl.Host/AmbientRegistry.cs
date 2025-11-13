using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.Host;

public class AmbientRegistry(ILogger logger)
{
	private static readonly string RegistryDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Uno Platform",
		"DevServers"
	);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true
	};

	private string? _registryFilePath;
	private readonly ILogger _logger = logger;

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
	internal void Register(string? solution, int ppid, int httpPort)
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
				UserName = Environment.UserName
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

			var registryFiles = Directory.GetFiles(RegistryDirectory, "devserver-*.json");

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
							activeServers.Add(registration);
						}
						else
						{
							// Clean up stale registration file
							File.Delete(filePath);
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
		_ = GetActiveDevServers(); // This will automatically clean up stale registrations
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
	}
}
