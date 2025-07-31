using System.Collections.Concurrent;
using System.Diagnostics;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

/// <summary>
/// Helper class for starting and managing the dev server during tests.
/// </summary>
/// <remarks>
/// This class addresses several potential issues:
/// - Thread-safe output capture using ConcurrentQueue instead of StringBuilder
/// - Robust host DLL discovery with multiple fallback strategies
/// - Proper exception handling around process startup
/// - Process cleanup on timeout or failure to prevent resource leaks
/// - Monotonic timing using Stopwatch instead of DateTime.Now
/// - More robust server startup detection with multiple indicators
/// - Proper cancellation token handling with OperationCanceledException
/// - Race condition prevention using SemaphoreSlim
/// - Safe environment variable merging
/// - Comprehensive logging of output on failures
/// 
/// Note: BeginOutputReadLine/BeginErrorReadLine should prevent stdout/stderr blocking
/// in most cases, but be aware that very heavy logging from the child process could
/// potentially cause issues if the handlers are slow.
/// </remarks>
public sealed class DevServerTestHelper : IAsyncDisposable
{

	private readonly ILogger _logger;
	private Process? _devServerProcess;
	private readonly ConcurrentQueue<string> _consoleOutput = new();
	private readonly ConcurrentQueue<string> _errorOutput = new();
	private readonly int _httpPort;
	private readonly string? _solutionPath;
	private readonly IReadOnlyDictionary<string, string>? _environmentVariables;
	private readonly SemaphoreSlim _startStopSemaphore = new(1, 1);
	private bool _isDisposed;

	/// <summary>
	/// Gets the captured console output from the dev server.
	/// </summary>
	public string ConsoleOutput => string.Join(Environment.NewLine, _consoleOutput);

	/// <summary>
	/// Gets the captured error output from the dev server.
	/// </summary>
	public string ErrorOutput => string.Join(Environment.NewLine, _errorOutput);

	/// <summary>
	/// Gets a value indicating whether the dev server is running.
	/// </summary>
	public bool IsRunning => _devServerProcess != null && !_devServerProcess.HasExited;

	/// <summary>
	/// Gets the HTTP port that the dev server is using.
	/// </summary>
	public int Port => _httpPort;

	/// <summary>
	/// Initializes a new instance of the <see cref="DevServerTestHelper"/> class.
	/// </summary>
	/// <param name="logger">The logger to use for logging.</param>
	/// <param name="httpPort">The HTTP port to use for the dev server. If 0, a random port will be used.</param>
	/// <param name="solutionPath">Optional path to the solution file.</param>
	/// <param name="environmentVariables">Optional environment variables for the process.</param>
	public DevServerTestHelper(
		ILogger logger,
		int httpPort = 0,
		string? solutionPath = null,
		IReadOnlyDictionary<string, string>? environmentVariables = null)
	{
		_logger = logger;
		_httpPort = httpPort == 0 ? GetRandomPort() : httpPort;
		_solutionPath = solutionPath;
		_environmentVariables = environmentVariables;
	}

	/// <summary>
	/// Starts the dev server.
	/// </summary>
	/// <param name="ct">Cancellation token to cancel the operation.</param>
	/// <param name="timeout">The timeout in milliseconds to wait for the server to start. Default is 30 seconds.</param>
	/// <param name="extraArgs"></param>
	/// <returns>True if the server started successfully, false otherwise.</returns>
	/// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
	public async Task<bool> StartAsync(CancellationToken ct, int timeout = 30000, string? extraArgs = null)
	{
		// Use semaphore to prevent race conditions with concurrent start/stop calls
		await _startStopSemaphore.WaitAsync(ct);
		try
		{
			if (IsRunning)
			{
				_logger.LogWarning("Dev server is already running");
				return true;
			}

			_logger.LogInformation("Starting dev server on port {Port}", _httpPort);

			// Discover host DLL path more robustly
			var hostDllPath = DiscoverHostDllPath();
			if (hostDllPath == null)
			{
				_logger.LogError("Could not locate Host DLL. Tried multiple discovery methods.");
				return false;
			}

			_logger.LogInformation("Using host dll at {Path}", hostDllPath);

			var startInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"\"{hostDllPath}\" --httpPort {_httpPort}" + (_solutionPath != null ? $" --solution \"{_solutionPath}\"" : "") + (extraArgs != null ? $" {extraArgs}" : ""),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true, // Enable stdin redirection for CTRL-C sending
				WorkingDirectory = Path.GetDirectoryName(hostDllPath) // Set working directory to ensure all dependencies are found
			};

			// Merge environment variables more safely
			if (_environmentVariables != null)
			{
				foreach (var variable in _environmentVariables)
				{
					// For testing purposes, allow overriding environment variables
					// This is especially important for telemetry redirection in tests
					if (startInfo.Environment.ContainsKey(variable.Key))
					{
						_logger.LogWarning("Environment variable {Key} already exists, overriding for test",
							variable.Key);
					}
					startInfo.Environment[variable.Key] = variable.Value;
				}
			}

			Process? process = null;
			try
			{
				process = new Process { StartInfo = startInfo };
				process.OutputDataReceived += (sender, args) =>
				{
					if (args.Data != null)
					{
						_consoleOutput.Enqueue(args.Data);
						_logger.LogDebug("DEV SERVER: {Output}", args.Data);
					}
				};
				process.ErrorDataReceived += (sender, args) =>
				{
					if (args.Data != null)
					{
						_errorOutput.Enqueue(args.Data);
						_logger.LogError("DEV SERVER ERROR: {Error}", args.Data);
					}
				};

				// Exception handling around Process.Start()
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				_devServerProcess = process;
				process = null; // Prevent disposal in finally block
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to start dev server process");
				process?.Dispose();
				return false;
			}

			// Wait for the server to start using Stopwatch for monotonic timing
			var stopwatch = Stopwatch.StartNew();
			var timeoutSpan = TimeSpan.FromMilliseconds(timeout);

			while (stopwatch.Elapsed < timeoutSpan)
			{
				// Throw OperationCanceledException if cancelled
				ct.ThrowIfCancellationRequested();

				// More robust startup detection - look for multiple possible indicators
				var consoleOutput = ConsoleOutput;
				if (IsServerStarted(consoleOutput))
				{
					_logger.LogInformation("Dev server started successfully in {ElapsedMs}ms",
						stopwatch.ElapsedMilliseconds);
					return true;
				}

				if (_devServerProcess.HasExited)
				{
					// Wait a little delay to let all console output accumulate
					await Task.Delay(250, ct);

					var finalConsoleOutput = ConsoleOutput;
					var finalErrorOutput = ErrorOutput;

					_logger.LogError(
						"Dev server process exited unexpectedly with code {ExitCode}. Console output: {ConsoleOutput}. Error output: {ErrorOutput}",
						_devServerProcess.ExitCode, finalConsoleOutput, finalErrorOutput);

					// Clean up the failed process
					CleanupProcess();
					return false;
				}

				await Task.Delay(100, ct);
			}

			// Timeout occurred - log final output and clean up
			var timeoutConsoleOutput = ConsoleOutput;
			var timeoutErrorOutput = ErrorOutput;
			_logger.LogError(
				"Timeout waiting for dev server to start after {TimeoutMs}ms. Console output: {ConsoleOutput}. Error output: {ErrorOutput}",
				timeout, timeoutConsoleOutput, timeoutErrorOutput);

			// Clean up the process that didn't start in time
			CleanupProcess();
			return false;
		}
		finally
		{
			_startStopSemaphore.Release();
		}
	}

	/// <summary>
	/// Stops the dev server using graceful shutdown if possible.
	/// </summary>
	public async Task StopAsync(CancellationToken ct)
	{
		if (!IsRunning)
		{
			return;
		}

		_logger.LogInformation("Stopping dev server gracefully");

		// Attempt graceful shutdown first - this method handles semaphore and cleanup internally
		var gracefulShutdownSucceeded = await AttemptGracefulShutdown(ct);

		if (!gracefulShutdownSucceeded)
		{
			_logger.LogWarning("Graceful shutdown failed, forcing process termination");
			// Use semaphore for the cleanup operation
			await _startStopSemaphore.WaitAsync(ct);
			try
			{
				CleanupProcess();
			}
			finally
			{
				_startStopSemaphore.Release();
			}
		}
	}

	/// <summary>
	/// Asserts that the dev server is running.
	/// </summary>
	public void AssertRunning()
	{
		IsRunning.Should().BeTrue("dev server should be running");
	}

	/// <summary>
	/// Asserts that the console output contains the specified text.
	/// </summary>
	/// <param name="text">The text to look for in the console output.</param>
	public void AssertConsoleOutputContains(string text)
	{
		ConsoleOutput.Should().Contain(text, $"console output should contain '{text}'");
	}

	/// <summary>
	/// Asserts that the error output contains the specified text.
	/// </summary>
	/// <param name="text">The text to look for in the error output.</param>
	public void AssertErrorOutputContains(string text)
	{
		ErrorOutput.Should().Contain(text, $"error output should contain '{text}'");
	}

	/// <summary>
	/// Discovers the path to the Host DLL using multiple strategies.
	/// </summary>
	/// <returns>The path to the Host DLL if found, null otherwise.</returns>
	private string? DiscoverHostDllPath()
	{
		// Strategy 1: Check environment variable for custom path
		var customPath = Environment.GetEnvironmentVariable("UNO_DEVSERVER_HOST_DLL_PATH");
		if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
		{
			_logger.LogInformation("Using custom Host DLL path from environment variable: {Path}", customPath);
			return customPath;
		}

		// Strategy 2: Try different build configurations and frameworks
		var assemblyLocation = Path.GetDirectoryName(typeof(DevServerTestHelper).Assembly.Location)!;
		var configurations = new[] { "Debug", "Release" };
		var frameworks = new[] { "net10.0", "net9.0", };

		foreach (var config in configurations)
		{
			foreach (var framework in frameworks)
			{
				var hostDllPath = Path.GetFullPath(Path.Combine(
					assemblyLocation,
					"..", "..", "..", "..",
					"Uno.UI.RemoteControl.Host", "bin", config, framework,
					"Uno.UI.RemoteControl.Host.dll"));

				if (File.Exists(hostDllPath))
				{
					_logger.LogInformation("Found Host DLL using discovery: {Path}", hostDllPath);
					return hostDllPath;
				}
			}
		}

		// Strategy 3: Try MSBuild output directory patterns
		var possiblePaths = new[]
		{
			Path.Combine(assemblyLocation, "Uno.UI.RemoteControl.Host.dll"),
			Path.Combine(assemblyLocation, "..", "Uno.UI.RemoteControl.Host", "Uno.UI.RemoteControl.Host.dll"),
		};

		foreach (var path in possiblePaths)
		{
			var fullPath = Path.GetFullPath(path);
			if (File.Exists(fullPath))
			{
				_logger.LogInformation("Found Host DLL in output directory: {Path}", fullPath);
				return fullPath;
			}
		}

		_logger.LogError(
			"Could not discover Host DLL path. Tried configurations: {Configurations}, frameworks: {Frameworks}",
			string.Join(", ", configurations), string.Join(", ", frameworks));
		return null;
	}

	/// <summary>
	/// Determines if the server has started based on console output.
	/// Uses multiple indicators to be more robust than just "Application started".
	/// </summary>
	/// <param name="consoleOutput">The console output to check.</param>
	/// <returns>True if the server appears to have started.</returns>
	private static bool IsServerStarted(string consoleOutput)
	{
		// Multiple possible startup indicators to make detection more robust
		var startupIndicators = new[]
		{
			"Application started", "Now listening on",
			"Application is shutting down", // This might seem counterintuitive, but it indicates the app at least started
			"Hosting environment:", "Content root path:", "info: Microsoft.Hosting.Lifetime"
		};

		return startupIndicators.Any(indicator =>
			consoleOutput.Contains(indicator, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Attempts to gracefully shutdown the dev server by sending CTRL-C signal.
	/// When graceful shutdown succeeds, this method will properly clean up the internal state.
	/// </summary>
	/// <param name="ct">Cancellation token for the operation.</param>
	/// <returns>True if graceful shutdown succeeded, false otherwise.</returns>
	public async Task<bool> AttemptGracefulShutdown(CancellationToken ct)
	{
		// Use semaphore to prevent race conditions with concurrent start/stop calls
		await _startStopSemaphore.WaitAsync(ct);
		try
		{
			if (_devServerProcess == null || _devServerProcess.HasExited)
			{
				return true;
			}

			try
			{
				// Attempt platform-specific graceful shutdown
				var signalSent = await SendGracefulShutdownSignal(_devServerProcess);
				_logger.LogDebug("Graceful shutdown signal sent: {SignalSent}", signalSent);

				// Wait for the process to exit gracefully (up to 5 seconds)
				var gracefulShutdownTimeout = TimeSpan.FromSeconds(5);
				var stopwatch = Stopwatch.StartNew();

				while (stopwatch.Elapsed < gracefulShutdownTimeout && !_devServerProcess.HasExited)
				{
					ct.ThrowIfCancellationRequested();
					await Task.Delay(100, ct);
				}

				var succeeded = _devServerProcess.HasExited;
				_logger.LogDebug("Graceful shutdown {Result} after {ElapsedMs}ms",
					succeeded ? "succeeded" : "timed out", stopwatch.ElapsedMilliseconds);

				// If graceful shutdown succeeded, clean up the internal state
				if (succeeded)
				{
					_logger.LogInformation("Dev server stopped gracefully");
					// Dispose the process object and reset the reference
					_devServerProcess?.Dispose();
					_devServerProcess = null;
				}

				return succeeded;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error during graceful shutdown attempt");
				return false;
			}
		}
		finally
		{
			_startStopSemaphore.Release();
		}
	}

	/// <summary>
	/// Sends a graceful shutdown signal to the process by writing CTRL-C to stdin.
	/// </summary>
	/// <param name="process">The process to send the signal to.</param>
	/// <returns>True if the signal was sent successfully, false otherwise.</returns>
	private async Task<bool> SendGracefulShutdownSignal(Process process)
	{
		try
		{
			_logger.LogDebug("Sending CTRL-C via stdin to process {ProcessId}", process.Id);

			// Send CTRL-C character (ASCII 3) via stdin
			if (process.StartInfo.RedirectStandardInput)
			{
				await process.StandardInput.WriteAsync('\x03'); // CTRL-C character
				await process.StandardInput.FlushAsync();
				_logger.LogDebug("CTRL-C sent via stdin");
				return true;
			}
			else
			{
				_logger.LogWarning("Cannot send CTRL-C via stdin - stdin is not redirected");
				return false;
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error sending CTRL-C via stdin");
			return false;
		}
	}


	/// <summary>
	/// Cleans up the current process by killing it and disposing resources.
	/// </summary>
	private void CleanupProcess()
	{
		if (_devServerProcess != null)
		{
			try
			{
				if (!_devServerProcess.HasExited)
				{
					_devServerProcess.Kill(entireProcessTree: true);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error killing dev server process during cleanup");
			}
			finally
			{
				try
				{
					_devServerProcess.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error disposing dev server process during cleanup");
				}

				_devServerProcess = null;
			}
		}
	}

	/// <summary>
	/// Gets a random port number between 10000 and 65535.
	/// </summary>
	/// <remarks>
	/// That's lazy approach that should work often enough for CI.
	/// </remarks>
	/// <returns>A random port number.</returns>
	private static int GetRandomPort() => new Random().Next(10000, 65536);

	public async ValueTask DisposeAsync()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		await StopAsync(CancellationToken.None);
		_startStopSemaphore.Dispose();
	}

	public void EnsureStarted()
	{
		if (!IsRunning)
		{
			// Output both error and console and throw an exception
			Console.Error.Write(ErrorOutput);
			Console.Error.Write(ConsoleOutput);

			throw new ApplicationException("Dev server not started : " + _devServerProcess?.ExitCode);
		}
	}

	public int? GetProcessId() => _devServerProcess?.Id;
}
