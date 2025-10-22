using System.Diagnostics;
using System.Text.Json;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;
using System.Threading;

namespace Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

public abstract class TelemetryTestBase
{
	protected static ILogger Logger { get; private set; } = null!;

	public TestContext? TestContext { get; set; }

	private CancellationToken GetTimeoutToken()
	{
		var baseToken = TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;
		if (!Debugger.IsAttached)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource(baseToken);
#if DEBUG
			// 45 seconds when running locally (DEBUG) without debugger
			cts.CancelAfter(TimeSpan.FromMinutes(.75));
#else
			// 2 minutes on CI
			cts.CancelAfter(TimeSpan.FromMinutes(2));
#endif
			return cts.Token;
		}
		return baseToken;
	}

	protected CancellationToken CT => GetTimeoutToken();

	protected SolutionHelper? SolutionHelper { get; private set; }

	[TestInitialize]
	public void TestInitialize()
	{
		SolutionHelper = new SolutionHelper(TestContext!);
		SolutionHelper.EnsureUnoTemplatesInstalled();
	}

	[TestCleanup]
	public void TestCleanup()
	{
		SolutionHelper?.Dispose();
		SolutionHelper = null;
	}

	private static void InitializeLogger<T>() where T : class
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
		});

		Logger = loggerFactory.CreateLogger<T>();
	}

	protected static void GlobalClassInitialize<T>(TestContext context) where T : class
	{
		InitializeLogger<T>();
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
			environmentVariables: new Dictionary<string, string> { { "UNO_PLATFORM_TELEMETRY_FILE", filePath }, });

		return (helper, tempDir);
	}

	/// <summary>
	/// Creates a DevServerTestHelper with telemetry redirection to an exact file path.
	/// </summary>
	protected DevServerTestHelper CreateTelemetryHelperWithExactPath(string exactFilePath, string? solutionPath = null, bool enableIdeChannel = false)
	{
		var envVars = new Dictionary<string, string>
		{
			{ "UNO_PLATFORM_TELEMETRY_FILE", exactFilePath }
		};

		if (enableIdeChannel)
		{
			// Create an IDE channel GUID so the dev-server will initialize the named-pipe IDE channel
			envVars["UNO_PLATFORM_DEVSERVER_ideChannel"] = Guid.NewGuid().ToString();
		}

		return new DevServerTestHelper(
			Logger,
			solutionPath: solutionPath,
			environmentVariables: envVars);
	}

	/// <summary>
	/// Runs a complete telemetry test cycle: start server, wait, shutdown.
	/// </summary>
	/// <returns>True if the server started successfully</returns>
	protected async Task<bool> RunTelemetryTestCycleAsync(DevServerTestHelper helper, int waitTimeMs = 2000)
	{
		var started = await helper.StartAsync(CT);
		helper.EnsureStarted();

		await Task.Delay(waitTimeMs, CT);
		await helper.AttemptGracefulShutdownAsync(CT);

		return started;
	}

	protected async Task CleanupTelemetryTestAsync(DevServerTestHelper helper, string tempDir, string filePattern)
	{
		await helper.StopAsync(CT);

		var testFiles = Directory.GetFiles(tempDir, filePattern);
		foreach (var file in testFiles)
		{
			try { File.Delete(file); }
			catch { /* ignore cleanup errors */ }
		}
	}

	protected async Task CleanupTelemetryTestAsync(DevServerTestHelper helper, string filePath)
	{
		await helper.StopAsync(CT);

		if (File.Exists(filePath))
		{
			try { File.Delete(filePath); }
			catch { /* ignore cleanup errors */ }
		}
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

	protected void WriteEventsList(List<(string Prefix, JsonDocument Json)> events)
	{
		TestContext!.WriteLine($"Found {events.Count} telemetry events:");
		var index = 1;
		foreach (var (prefix, json) in events)
		{
			if (json.RootElement.TryGetProperty("EventName", out var eventName))
			{
				TestContext!.WriteLine($"[{index++}] Prefix: {prefix}, EventName: {eventName.GetString()}");
			}
		}
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
	/// Assert that at least one event with the given event name exists and has the specified property with the expected value.
	/// </summary>
	protected static void AssertEventHasProperty(
		List<(string Prefix, JsonDocument Json)> events,
		string eventName,
		string propertyName,
		string expectedValue,
		string? prefix = null)
	{
		var filtered = prefix == null ? events : events.Where(e => e.Prefix == prefix);
		var matchingEvents = filtered
			.Where(e => e.Json.RootElement.TryGetProperty("EventName", out var n) && n.GetString() == eventName)
			.ToList();

		matchingEvents.Should().NotBeEmpty($"Should contain at least one event '{eventName}'{(prefix != null ? $" with prefix '{prefix}'" : "")}");

		var hasProperty = matchingEvents.Any(e =>
			e.Json.RootElement.TryGetProperty("Properties", out var props) &&
			props.TryGetProperty(propertyName, out var prop) &&
			prop.GetString() == expectedValue);

		hasProperty.Should().BeTrue(
			$"Event '{eventName}' should have property '{propertyName}' with value '{expectedValue}'{(prefix != null ? $" (prefix: '{prefix}')" : "")}");
	}

	protected static string GetTestTelemetryFileName(string testKey) => $"telemetry_{testKey}_{Guid.NewGuid():N}.log";
}
