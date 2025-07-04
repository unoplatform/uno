using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Base class for telemetry tests that provides common functionality and reduces code duplication.
/// </summary>
public abstract class TelemetryTestBase
{
	protected static ILogger Logger { get; private set; } = null!;

	public TestContext? TestContext { get; set; }

	protected CancellationToken CT => TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;

	protected static void InitializeLogger<T>() where T : class
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
		});

		Logger = loggerFactory.CreateLogger<T>();
	}

	/// <summary>
	/// Creates a DevServerTestHelper with telemetry redirection to a temporary file.
	/// </summary>
	protected (DevServerTestHelper helper, string tempDir) CreateTelemetryHelper(string baseFileName)
	{
		var tempDir = Path.GetTempPath();
		var filePath = Path.Combine(tempDir, baseFileName);

		var helper = new DevServerTestHelper(
			Logger,
			environmentVariables: new Dictionary<string, string> { { "UNO_DEVSERVER_TELEMETRY_FILE", filePath } });

		return (helper, tempDir);
	}

	/// <summary>
	/// Creates a DevServerTestHelper with telemetry redirection to an exact file path.
	/// </summary>
	protected DevServerTestHelper CreateTelemetryHelperWithExactPath(string exactFilePath)
	{
		return new DevServerTestHelper(
			Logger,
			environmentVariables: new Dictionary<string, string> { { "UNO_DEVSERVER_TELEMETRY_FILE", exactFilePath } });
	}

	/// <summary>
	/// Runs a complete telemetry test cycle: start server, wait, shutdown.
	/// </summary>
	/// <returns>True if the server started successfully</returns>
	protected async Task<bool> RunTelemetryTestCycle(DevServerTestHelper helper, int waitTimeMs = 2000)
	{
		var started = await helper.StartAsync(CT);
		helper.EnsureStarted();

		await Task.Delay(waitTimeMs, CT);
		await helper.AttemptGracefulShutdown(CT);

		return started;
	}

	/// <summary>
	/// Validates that telemetry files were created and contain expected content.
	/// </summary>
	/// <returns>Array of found files</returns>
	protected async Task<string[]> ValidateTelemetryFiles(string tempDir, string filePattern, bool shouldContainDevServer = true)
	{
		var files = Directory.GetFiles(tempDir, filePattern);
		files.Should().NotBeEmpty("telemetry files should be created");

		foreach (var file in files)
		{
			var fileName = Path.GetFileName(file);
			var content = await File.ReadAllTextAsync(file, CT);
			content.Should().NotBeNullOrEmpty($"telemetry file {fileName} should not be empty");

			if (shouldContainDevServer)
			{
				content.Should().Contain("DevServer", $"file {fileName} should contain DevServer events");
			}

			Logger.LogInformation($"[DEBUG_LOG] Validated file: {fileName}, Content length: {content.Length}");
		}

		return files;
	}

	/// <summary>
	/// Validates that global telemetry files were created.
	/// </summary>
	/// <returns>Array of global files</returns>
	protected string[] ValidateGlobalFiles(string[] allFiles)
	{
		var globalFiles = allFiles.Where(f => f.Contains("_global_")).ToArray();
		globalFiles.Should().NotBeEmpty("global telemetry file should be created");
		return globalFiles;
	}

	/// <summary>
	/// Cleans up telemetry test files.
	/// </summary>
	protected async Task CleanupTelemetryTest(DevServerTestHelper helper, string tempDir, string filePattern)
	{
		await helper.StopAsync(CT);

		var testFiles = Directory.GetFiles(tempDir, filePattern);
		foreach (var file in testFiles)
		{
			try { File.Delete(file); }
			catch
			{
				/* ignore cleanup errors */
			}
		}
	}

	/// <summary>
	/// Cleans up a single telemetry test file.
	/// </summary>
	protected async Task CleanupTelemetryTest(DevServerTestHelper helper, string filePath)
	{
		await helper.StopAsync(CT);

		if (File.Exists(filePath))
		{
			try
			{
				File.Delete(filePath);
			}
			catch
			{
				/* ignore cleanup errors */
			}
		}
	}
}
