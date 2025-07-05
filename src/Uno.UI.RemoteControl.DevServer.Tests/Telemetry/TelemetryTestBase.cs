using System.Diagnostics;
using System.Text.Json;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

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
	/// Initialise la télémétrie et le logger pour la classe de test concrète (générique).
	/// </summary>
	public static void GlobalClassInitialize<T>(TestContext context) where T : class
	{
		SolutionHelper.EnsureUnoTemplatesInstalled();
		InitializeLogger<T>();
	}

	private static void InitializeLogger(Type type)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
		});
		Logger = loggerFactory.CreateLogger(type);
	}

	/// <summary>
	/// Creates a DevServerTestHelper with telemetry redirection to a temporary file.
	/// </summary>
	protected (DevServerTestHelper helper, string tempDir) CreateTelemetryHelper(string baseFileName)
	{
		var tempDir = Path.GetTempPath();
		var filePath = Path.Combine(tempDir, baseFileName);

		// Enable single file telemetry mode
		var helper = new DevServerTestHelper(
			Logger,
			environmentVariables: new Dictionary<string, string> { { "UNO_DEVSERVER_TELEMETRY_FILE", filePath }, });

		return (helper, tempDir);
	}

	/// <summary>
	/// Creates a DevServerTestHelper with telemetry redirection to an exact file path.
	/// </summary>
	protected DevServerTestHelper CreateTelemetryHelperWithExactPath(string exactFilePath, string? solutionPath = null)
	{
		return new DevServerTestHelper(
			Logger,
			solutionPath: solutionPath,
			environmentVariables: new Dictionary<string, string>
			{
				{ "UNO_DEVSERVER_TELEMETRY_FILE", exactFilePath },
			});
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
	/// Cleans up telemetry test files.
	/// </summary>
	protected async Task CleanupTelemetryTest(DevServerTestHelper helper, string tempDir, string filePattern)
	{
		await helper.StopAsync(CT);

		var testFiles = Directory.GetFiles(tempDir, filePattern);
		foreach (var file in testFiles)
		{
			try { File.Delete(file); }
			catch { /* ignore cleanup errors */ }
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
			try { File.Delete(filePath); }
			catch { /* ignore cleanup errors */ }
		}
	}

	protected async Task<T> WaitFor<T>(Func<CancellationToken, Task<T>> test, CancellationToken ct, int interations = 5,
		int timeBetweenIterationsInMs = 250)
	{
		for (var i = 0; i < interations; i++)
		{
			try { return await test(ct); }
			catch { await Task.Delay(timeBetweenIterationsInMs, ct); if (i == interations - 1) throw; }
		}
		throw new InvalidOperationException();
	}

	/// <summary>
	/// Helper method to wait for a client to connect to the server with improved diagnostics.
	/// </summary>
	protected static async Task WaitForClientConnectionAsync(
		RemoteControlClient client,
		TimeSpan timeout)
	{
		var startTime = Stopwatch.GetTimestamp();
		var checkInterval = TimeSpan.FromMilliseconds(200);
		var attemptsCount = 0;

		while (Stopwatch.GetElapsedTime(startTime) < timeout)
		{
			attemptsCount++;
			if (client.Status.State == RemoteControlStatus.ConnectionState.Connected)
			{
				Console.WriteLine($"Client connected successfully after {attemptsCount} attempts ({Stopwatch.GetElapsedTime(startTime).TotalSeconds:F1}s)");
				return;
			}
			if (attemptsCount % 10 == 0)
			{
				Console.WriteLine($"Waiting for client connection... Current state: {client.Status.State}, Elapsed: {Stopwatch.GetElapsedTime(startTime).TotalSeconds:F1}s");
			}
			await Task.Delay(checkInterval);
		}
		throw new TimeoutException($"Client failed to connect within {timeout.TotalSeconds} seconds. Final state: {client.Status.State}");
	}

	/// <summary>
	/// Logs client status information for diagnostic purposes.
	/// </summary>
	protected static void LogClientStatus(RemoteControlClient client, string context)
	{
		Console.WriteLine($"[{context}] Client Status: State={client.Status.State}, Error={client.Status.Error}, KeepAlive={client.Status.KeepAlive}");
	}

	/// <summary>
	/// Parse telemetry file content into a list of (Prefix, JsonDocument) objects.
	/// </summary>
	protected static List<(string Prefix, JsonDocument Json)> ParseTelemetryEvents(string fileContent)
	{
		return fileContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Split(':', 2, StringSplitOptions.RemoveEmptyEntries))
			.Where(x => x.Length == 2)
			.Select(x => (x[0].Trim(), JsonDocument.Parse(x[1])))
			.ToList();
	}

	/// <summary>
	/// Assert that at least one event with the given prefix exists.
	/// </summary>
	protected static void AssertHasPrefix(List<(string Prefix, JsonDocument Json)> events, string prefix)
	{
		events.Any(e => e.Prefix == prefix).Should().BeTrue($"Should contain at least one event with prefix '{prefix}'");
	}

	/// <summary>
	/// Assert that at least one event with the given event name exists (optionally for a given prefix).
	/// </summary>
	protected static void AssertHasEvent(List<(string Prefix, JsonDocument Json)> events, string eventName, string? prefix = null)
	{
		var filtered = prefix == null ? events : events.Where(e => e.Prefix == prefix);
		filtered.Any(e => e.Json.RootElement.TryGetProperty("EventName", out var n) && n.GetString() == eventName)
			.Should().BeTrue($"Should contain event '{eventName}'{(prefix != null ? $" with prefix '{prefix}'" : "")}");
	}

	/// <summary>
	/// Génère un nom de fichier de test telemetry unique et court.
	/// </summary>
	protected static string GetTestTelemetryFileName(string testKey) => $"telemetry_{testKey}_{Guid.NewGuid():N}.log";
}
