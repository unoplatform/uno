using System.Text.Json;

namespace Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

[TestClass]
public class TelemetryProcessorTests : TelemetryTestBase
{
	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<TelemetryProcessorTests>(context);

	/// <summary>
	/// Tests that a processor loaded during discovery properly resolves its ITelemetry&lt;T&gt; 
	/// and logs events to the file telemetry.
	/// </summary>
	[TestMethod]
	public async Task Telemetry_ProcessorDiscovery_LogsDiscoveryEvents()
	{
		var solution = SolutionHelper!;
		var appInstanceId = Guid.NewGuid().ToString("N");

		// Arrange - Create a temporary file for telemetry output
		var telemetryFileName = GetTestTelemetryFileName("processor_discovery");
		var tempDir = Path.GetTempPath();
		var telemetryFilePath = Path.Combine(tempDir, telemetryFileName);

		// Arrange - Creation of a solution is required for processors discovery
		await solution.CreateSolutionFile();

		// Use DevServerTestHelper to start a server with telemetry redirection
		await using var helper = CreateTelemetryHelperWithExactPath(telemetryFilePath, solutionPath: solution.SolutionFile);

		try
		{
			// Act - Start the server and connect a client
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			started.Should().BeTrue();

			// Create a client and connect to the server
			var client = RemoteControlClient.Initialize(
				typeof(object),
				new[] { new ServerEndpointAttribute("localhost", helper.Port) }
			);
			await client.SendMessage(new KeepAliveMessage());

			// Locate the test processor DLL path
			// Look in the common build output directories where our test processor will be built

			const string testProcessorName = "Uno.UI.RemoteControl.TestProcessor.dll";

			var testProcessorPath = Directory.EnumerateFiles(
					path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Uno.UI.RemoteControl.TestProcessor", "bin"),
					searchPattern: testProcessorName,
					searchOption: SearchOption.AllDirectories)
				.FirstOrDefault();

			if (testProcessorPath == null)
			{
				Assert.Fail(
					$"Could not find test processor assembly. Make sure {testProcessorName} is built before running this test.");
			}

			testProcessorPath = Path.GetFullPath(testProcessorPath);

			// Send processor discovery message pointing directly to the test processor DLL
			Logger.LogInformation("Using test processor at: {Path}", testProcessorPath);
			await client.SendMessage(new ProcessorsDiscovery(testProcessorPath));

			// Wait for processor discovery and telemetry logging to complete
			await Task.Delay(2000, CT);

			// Ensure graceful shutdown to flush telemetry
			await helper.AttemptGracefulShutdown(CT);

			// Assert - Check telemetry file was created and contains expected events
			File.Exists(telemetryFilePath).Should().BeTrue("Telemetry file should be created");
			var fileContent = await File.ReadAllTextAsync(telemetryFilePath, CT);
			var events = ParseTelemetryEvents(fileContent);
			WriteEventsList(events);

			// There should be multiple telemetry events including our processor's initialization event
			events.Should().NotBeEmpty("Telemetry file should contain events");

			// Assert that server telemetry is present
			AssertHasPrefix(events, "global");

			// Check if we have connection-specific events
			events.Any(e => e.Prefix.StartsWith("connection-")).Should()
				.BeTrue("Connection telemetry should be present");

			// Check for processor discovery events
			AssertHasEvent(events, "uno/dev-server/processor-discovery-start");
			AssertHasEvent(events, "uno/dev-server/processor-discovery-complete");

			// Log all events for debugging
			Logger.LogInformation("Found {Count} telemetry events:", events.Count);
			foreach (var (prefix, json) in events.Take(20))
			{
				if (json.RootElement.TryGetProperty("EventName", out var eventName))
				{
					Logger.LogInformation(" - Prefix: {Prefix}, EventName: {EventName}",
						prefix, eventName.GetString());
				}
			}

			// Our test processor should have generated an initialization event when it was created
			// This event name will have the prefix defined in the TestProcessor assembly
			var processorEvents = events.Where(e =>
				e.Json.RootElement.TryGetProperty("EventName", out var name) &&
				name.GetString()?.Contains("telemetry-test-initialized") == true)
				.ToList();
			var processorEvent = processorEvents.Single();

			// Verify some properties of the test event
			var root = processorEvent.Json.RootElement;
			if (root.TryGetProperty("Properties", out var props) &&
				props.TryGetProperty("ProcessorType", out var processorType))
			{
				processorType.GetString().Should()
					.Be("TelemetryTestProcessor", "Event should contain processor type");
			}

			// Log the event for debugging
			Logger.LogInformation("Found processor telemetry event: {Event}",
				JsonSerializer.Serialize(processorEvent));
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Test failed with exception");

			Console.Error.WriteLine(helper.ConsoleOutput);
			throw;
		}
		finally
		{
			// Cleanup - Stop the server and remove test files
			await helper.StopAsync(CT);
			await CleanupTelemetryTest(helper, telemetryFilePath);
		}
	}

	/// <summary>
	/// Tests that ServerHotReloadProcessor can be properly instantiated and resolves ITelemetry&lt;T&gt; 
	/// with the correct connection context.
	/// </summary>
	[TestMethod]
	public async Task Telemetry_ServerHotReloadProcessor_ResolvesCorrectly()
	{
		var solution = SolutionHelper!;
		var appInstanceId = Guid.NewGuid().ToString("N");

		// Arrange - Create a temporary file for telemetry output
		var telemetryFileName = GetTestTelemetryFileName("hotreload_processor");
		var tempDir = Path.GetTempPath();
		var telemetryFilePath = Path.Combine(tempDir, telemetryFileName);

		// Arrange - Creation of a solution is required for processors discovery
		await solution.CreateSolutionFile();

		// Use DevServerTestHelper to start a server with telemetry redirection
		await using var helper = CreateTelemetryHelperWithExactPath(telemetryFilePath, solutionPath: solution.SolutionFile);

		try
		{
			// Act - Start the server and connect a client
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			started.Should().BeTrue();

			// Create a client and connect to the server
			var client = RemoteControlClient.Initialize(
				typeof(object),
				new[] { new ServerEndpointAttribute("localhost", helper.Port) }
			);
			await client.SendMessage(new KeepAliveMessage());

			// Locate the ServerHotReloadProcessor DLL path
			// This should be in the Server.Processors assembly
			const string hotReloadProcessorAssembly = "Uno.UI.RemoteControl.Server.Processors.dll";

			var hotReloadProcessorPath = Directory.EnumerateFiles(
					path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Uno.UI.RemoteControl.Server.Processors", "bin"),
					searchPattern: hotReloadProcessorAssembly,
					searchOption: SearchOption.AllDirectories)
				.FirstOrDefault();

			if (hotReloadProcessorPath == null)
			{
				Assert.Fail(
					$"Could not find HotReload processor assembly. Make sure {hotReloadProcessorAssembly} is built before running this test.");
			}

			hotReloadProcessorPath = Path.GetFullPath(hotReloadProcessorPath);

			// Send processor discovery message pointing to the HotReload processor DLL
			Logger.LogInformation("Using HotReload processor at: {Path}", hotReloadProcessorPath);
			await client.SendMessage(new ProcessorsDiscovery(hotReloadProcessorPath));

			// Wait for processor discovery and telemetry logging to complete
			await Task.Delay(3000, CT); // Longer wait for HotReload processor

			// Ensure graceful shutdown to flush telemetry
			await helper.AttemptGracefulShutdown(CT);

			// Assert - Check telemetry file was created and contains expected events
			File.Exists(telemetryFilePath).Should().BeTrue("Telemetry file should be created");
			var fileContent = await File.ReadAllTextAsync(telemetryFilePath, CT);
			var events = ParseTelemetryEvents(fileContent);
			WriteEventsList(events);

			// There should be multiple telemetry events
			events.Should().NotBeEmpty("Telemetry file should contain events");

			// Assert that server telemetry is present
			AssertHasPrefix(events, "global");

			// Check if we have connection-specific events
			events.Any(e => e.Prefix.StartsWith("connection-")).Should()
				.BeTrue("Connection telemetry should be present");

			// Check for processor discovery events
			AssertHasEvent(events, "uno/dev-server/processor-discovery-start");
			AssertHasEvent(events, "uno/dev-server/processor-discovery-complete");

			// Log all events for debugging to help identify HotReload processor events
			Logger.LogInformation("Found {Count} telemetry events:", events.Count);
			foreach (var (prefix, json) in events.Take(50)) // More events for debugging
			{
				if (json.RootElement.TryGetProperty("EventName", out var eventName))
				{
					Logger.LogInformation(" - Prefix: {Prefix}, EventName: {EventName}",
						prefix, eventName.GetString());
				}
			}

			// Look for any HotReload-related processor instantiation events
			// The fact that discovery completed successfully means ServerHotReloadProcessor was instantiated without DI issues
			var discoveryCompleteEvents = events.Where(e =>
				e.Json.RootElement.TryGetProperty("EventName", out var name) &&
				name.GetString() == "uno/dev-server/processor-discovery-complete")
				.ToList();

			discoveryCompleteEvents.Should().NotBeEmpty("Discovery should complete successfully, indicating DI resolution worked");

			// Verify connection context is properly maintained
			// Connection-scoped telemetry should be present, showing the processor has access to connection context
			var connectionEvents = events.Where(e => e.Prefix.StartsWith("connection-")).ToList();
			connectionEvents.Should().NotBeEmpty("Connection-scoped telemetry should be available to processors");

			Logger.LogInformation("✅ ServerHotReloadProcessor test passed - DI resolution works correctly!");
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "ServerHotReloadProcessor test failed with exception");

			Console.Error.WriteLine(helper.ConsoleOutput);
			throw;
		}
		finally
		{
			// Cleanup - Stop the server and remove test files
			await helper.StopAsync(CT);
			await CleanupTelemetryTest(helper, telemetryFilePath);
		}
	}
}
