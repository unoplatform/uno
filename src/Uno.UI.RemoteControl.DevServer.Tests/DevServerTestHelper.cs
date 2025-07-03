using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Helper class for starting and managing the dev server during tests.
/// </summary>
public sealed class DevServerTestHelper : IAsyncDisposable
{
	private readonly ILogger _logger;
	private Process? _devServerProcess;
	private readonly StringBuilder _consoleOutput = new();
	private readonly StringBuilder _errorOutput = new();
	private readonly int _httpPort;
	private readonly string? _solutionPath;
	private readonly IReadOnlyDictionary<string, string>? _environmentVariables;
	private bool _isDisposed;

	/// <summary>
	/// Gets the captured console output from the dev server.
	/// </summary>
	public string ConsoleOutput => _consoleOutput.ToString();

	/// <summary>
	/// Gets the captured error output from the dev server.
	/// </summary>
	public string ErrorOutput => _errorOutput.ToString();

	/// <summary>
	/// Gets a value indicating whether the dev server is running.
	/// </summary>
	public bool IsRunning => _devServerProcess != null && !_devServerProcess.HasExited;

	/// <summary>
	/// Initializes a new instance of the <see cref="DevServerTestHelper"/> class.
	/// </summary>
	/// <param name="logger">The logger to use for logging.</param>
	/// <param name="httpPort">The HTTP port to use for the dev server. If 0, a random port will be used.</param>
	/// <param name="solutionPath">Optional path to the solution file.</param>
	/// <param name="environmentVariables">Optional environment variables for the process.</param>
	public DevServerTestHelper(ILogger logger, int httpPort = 0, string? solutionPath = null, IReadOnlyDictionary<string, string>? environmentVariables = null)
	{
		_logger = logger;
		_httpPort = httpPort == 0 ? GetRandomPort() : httpPort;
		_solutionPath = solutionPath;
		_environmentVariables = environmentVariables;
	}

	/// <summary>
	/// Starts the dev server.
	/// </summary>
	/// <param name="timeout">The timeout in milliseconds to wait for the server to start.</param>
	/// <returns>True if the server started successfully, false otherwise.</returns>
	public async Task<bool> StartAsync(CancellationToken ct, int timeout = 10000)
	{
		if (IsRunning)
		{
			_logger.LogWarning("Dev server is already running");
			return true;
		}

		_logger.LogInformation("Starting dev server on port {Port}", _httpPort);

		// Get the path to the Host dll in the original project's bin folder
		var sourceProjectBinPath = Path.GetFullPath(Path.Combine(
			Path.GetDirectoryName(typeof(DevServerTestHelper).Assembly.Location)!,
			"..", "..", "..", "..",
			"Uno.UI.RemoteControl.Host", "bin", "Debug", "net8.0"));

		var hostDllPath = Path.Combine(sourceProjectBinPath, "Uno.UI.RemoteControl.Host.dll");

		// Ensure the path exists
		if (!File.Exists(hostDllPath))
		{
			_logger.LogError("Host dll not found at {Path}", hostDllPath);
			return false;
		}

		_logger.LogInformation("Using host dll at {Path}", hostDllPath);

		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"\"{hostDllPath}\" --httpPort {_httpPort}" +
				(_solutionPath != null ? $" --solution \"{_solutionPath}\"" : ""),
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = sourceProjectBinPath // Set working directory to ensure all dependencies are found
		};

		if (_environmentVariables != null)
		{
			foreach (var variable in _environmentVariables)
			{
				startInfo.EnvironmentVariables[variable.Key] = variable.Value;
			}
		}

		_devServerProcess = new Process { StartInfo = startInfo };
		_devServerProcess.OutputDataReceived += (sender, args) =>
		{
			if (args.Data != null)
			{
				_consoleOutput.AppendLine(args.Data);
				_logger.LogDebug("DEV SERVER: {Output}", args.Data);
			}
		};
		_devServerProcess.ErrorDataReceived += (sender, args) =>
		{
			if (args.Data != null)
			{
				_errorOutput.AppendLine(args.Data);
				_logger.LogError("DEV SERVER ERROR: {Error}", args.Data);
			}
		};

		_devServerProcess.Start();
		_devServerProcess.BeginOutputReadLine();
		_devServerProcess.BeginErrorReadLine();

		// Wait for the server to start
		var startTime = DateTime.Now;
		while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(timeout) && ct.IsCancellationRequested == false)
		{
			if (ConsoleOutput.Contains("Application started"))
			{
				_logger.LogInformation("Dev server started successfully");
				return true;
			}

			if (_devServerProcess.HasExited)
			{
				// Wait a little delay to let all console output accumulate
				await Task.Delay(250, ct);

				_logger.LogError("Dev server process exited unexpectedly with code {ExitCode}", _devServerProcess.ExitCode);
				return false;
			}

			await Task.Delay(100, ct);
		}

		_logger.LogError("Timeout waiting for dev server to start");
		return false;
	}

	/// <summary>
	/// Stops the dev server.
	/// </summary>
	public async Task StopAsync(CancellationToken ct)
	{
		if (!IsRunning)
		{
			return;
		}

		_logger.LogInformation("Stopping dev server");

		try
		{
			_devServerProcess!.Kill(entireProcessTree: true);
			await _devServerProcess.WaitForExitAsync(ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error stopping dev server");
		}
		finally
		{
			_devServerProcess = null;
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
}
