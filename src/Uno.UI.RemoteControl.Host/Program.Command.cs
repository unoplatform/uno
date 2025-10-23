using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.Host;

partial class Program
{
	private static async Task StartCommandAsync(int httpPort, int parentPID, string? solution, string? workingDir, int timeoutMs)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(workingDir))
			{
				workingDir = Directory.GetCurrentDirectory();
			}

			if (string.IsNullOrWhiteSpace(solution))
			{
				var solutionFiles = Directory.EnumerateFiles(workingDir, "*.sln").Concat(Directory.EnumerateFiles(workingDir, "*.slnx")).ToArray();
				solution = solutionFiles.FirstOrDefault();
			}

			var ambientLogger = NullLogger.Instance;
			var ambient = new AmbientRegistry(ambientLogger);

			// If a solution is provided (or discovered), check if an active DevServer already serves it.
			if (!string.IsNullOrWhiteSpace(solution))
			{
				var existingBySolution = ambient.GetActiveDevServerForPath(solution);
				if (existingBySolution is not null)
				{
					await Console.Out.WriteLineAsync($"A DevServer is already running for solution {Path.GetFullPath(solution)} (PID {existingBySolution.ProcessId}, Port {existingBySolution.Port}). Not starting a new one.");
					Environment.ExitCode = 0;
					return;
				}
			}

			// If a port was explicitly requested, check for an active DevServer on that port.
			if (httpPort > 0)
			{
				var existingByPort = ambient.GetActiveDevServerForPort(httpPort);
				if (existingByPort is not null)
				{
					await Console.Out.WriteLineAsync($"A DevServer is already running on port {httpPort} (PID {existingByPort.ProcessId}). Not starting a new one.");
					Environment.ExitCode = 0;
					return;
				}
			}

			// If no http port was specified, allocate one now.
			if (httpPort == 0)
			{
				httpPort = EnsureTcpPort();
			}

			var selfPath = Assembly.GetExecutingAssembly().Location;
			var psi = new ProcessStartInfo
			{
				FileName = selfPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? "dotnet" : selfPath,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WorkingDirectory = workingDir,
			};

			if (selfPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
			{
				psi.ArgumentList.Add(selfPath);
			}

			psi.ArgumentList.Add("--httpPort");
			psi.ArgumentList.Add(httpPort.ToString(CultureInfo.InvariantCulture));

			if (parentPID > 0)
			{
				psi.ArgumentList.Add("--ppid");
				psi.ArgumentList.Add(parentPID.ToString(CultureInfo.InvariantCulture));
			}

			if (string.IsNullOrWhiteSpace(solution))
			{
				await Console.Error.WriteLineAsync("A solution file is required");
				Environment.ExitCode = 1;
				return;
			}

			psi.ArgumentList.Add("--solution");
			psi.ArgumentList.Add(solution);

			await CsprojUserGenerator.SetCsprojUserPort(solution, httpPort);

			await Console.Out.WriteLineAsync($"Starting DevServer: {psi.FileName} {string.Join(' ', psi.ArgumentList)}");

			using var process = Process.Start(psi);
			if (process is null)
			{
				await Console.Error.WriteLineAsync("Failed to start DevServer process");
				Environment.ExitCode = 1;
				return;
			}

			var outputTask = process.StandardOutput.ReadToEndAsync();
			var errorTask = process.StandardError.ReadToEndAsync();

			var ready = await WaitForDevServerReadyAsync(httpPort, timeoutMs);
			if (!ready)
			{
				await Console.Error.WriteLineAsync($"DevServer did not become ready within {timeoutMs}ms");

				var output = await outputTask;
				var error = await errorTask;

				if (!string.IsNullOrWhiteSpace(output))
				{
					await Console.Error.WriteLineAsync("DevServer stdout:\n" + output);
				}
				if (!string.IsNullOrWhiteSpace(error))
				{
					await Console.Error.WriteLineAsync("DevServer stderr:\n" + error);
				}

				try
				{
					if (!process.HasExited)
					{
						process.Kill();
					}
				}
				catch { }

				Environment.ExitCode = 1;
				return;
			}

			await Console.Out.WriteLineAsync($"DevServer is ready on port {httpPort}");
			Environment.ExitCode = 0;
		}
		catch (Exception ex)
		{
			await Console.Error.WriteLineAsync($"Controller error: {ex.Message}");
			Environment.ExitCode = 1;
		}
	}

	private static async Task<int> StopCommandAsync()
	{
		var logger = NullLogger.Instance;
		var ambient = new AmbientRegistry(logger);
		var servers = ambient.GetActiveDevServers().ToList();
		if (servers.Count == 0)
		{
			await Console.Out.WriteLineAsync("No active DevServers found to stop.");
			return 0;
		}

		int stopped = 0, failed = 0;
		foreach (var s in servers)
		{
			await Console.Out.WriteLineAsync($"Stopping DevServer with PID {s.ProcessId} on port {s.Port}...");
			try
			{
				try
				{
					var p = Process.GetProcessById(s.ProcessId);
					if (!p.HasExited)
					{
						p.Kill();
						await p.WaitForExitAsync();
					}
					stopped++;
				}
				catch (ArgumentException)
				{
					// Process not found; treat as already stopped
					stopped++;
				}
			}
			catch (Exception ex)
			{
				failed++;
				await Console.Error.WriteLineAsync($"Error stopping DevServer with PID {s.ProcessId}: {ex.Message}");
			}
		}

		ambient.CleanupStaleRegistrations();

		await Console.Out.WriteLineAsync($"Successfully stopped {stopped} DevServer(s)");
		if (failed > 0)
		{
			await Console.Error.WriteLineAsync($"Failed to stop {failed} DevServer(s)");
			return 1;
		}

		return 0;
	}

	private static async Task<int> ListCommandAsync()
	{
		var logger = NullLogger.Instance;
		var ambient = new AmbientRegistry(logger);
		var servers = ambient.GetActiveDevServers().ToList();

		await Console.Out.WriteLineAsync("Active Uno DevServers:");
		if (servers.Count == 0)
		{
			await Console.Out.WriteLineAsync("No active DevServers found.");
			return 0;
		}

		foreach (var s in servers)
		{
			await Console.Out.WriteLineAsync($"Process ID: {s.ProcessId}");
			await Console.Out.WriteLineAsync($"  Parent PID: {s.ParentProcessId}");
			await Console.Out.WriteLineAsync($"  Port: {s.Port}");
			await Console.Out.WriteLineAsync($"  Solution: {s.SolutionPath ?? "N/A"}");
			await Console.Out.WriteLineAsync($"  Machine: {s.MachineName}");
			await Console.Out.WriteLineAsync($"  User: {s.UserName}");
			await Console.Out.WriteLineAsync($"  Started: {s.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
		}

		await Console.Out.WriteLineAsync($"Total active DevServers: {servers.Count}");
		return 0;
	}

	private static async Task<int> CleanupCommandAsync()
	{
		var logger = NullLogger.Instance;
		var ambient = new AmbientRegistry(logger);
		await Console.Out.WriteLineAsync("Cleaning up stale DevServer registrations...");
		ambient.CleanupStaleRegistrations();
		await Console.Out.WriteLineAsync("Cleanup completed.");
		return 0;
	}

	private static async Task<bool> WaitForDevServerReadyAsync(int port, int timeoutMs)
	{
		var sw = Stopwatch.StartNew();
		var endpoint = new IPEndPoint(IPAddress.Loopback, port);

		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			try
			{
				using var tcp = new TcpClient();
				var connectTask = tcp.ConnectAsync(endpoint.Address, endpoint.Port);
				var timeoutTask = Task.Delay(1000);
				var winner = await Task.WhenAny(connectTask, timeoutTask);
				if (winner == connectTask && !connectTask.IsFaulted)
				{
					return true;
				}
			}
			catch { }

			await Task.Delay(500);
		}

		return false;
	}

	private static int EnsureTcpPort()
	{
		var tcp = new TcpListener(IPAddress.Any, 0) { ExclusiveAddressUse = true };
		tcp.Start();
		var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();
		return port;
	}
}
