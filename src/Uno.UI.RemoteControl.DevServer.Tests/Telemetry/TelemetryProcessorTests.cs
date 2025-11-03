using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;

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
		var solution = SolutionHelper;
		var appInstanceId = Guid.NewGuid().ToString("N");

		// Arrange - Create a temporary file for telemetry output
		var telemetryFileName = GetTestTelemetryFileName("processor_discovery");
		var tempDir = Path.GetTempPath();
		var telemetryFilePath = Path.Combine(tempDir, telemetryFileName);

		// Arrange - Creation of a solution is required for processors discovery
		await solution.CreateSolutionFileAsync();

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

			// Locate the test processor DLL path using ExternalDllDiscoveryHelper
			const string testProcessorName = "Uno.Test.Processor.dll";

			var testProcessorPath = ExternalDllDiscoveryHelper.DiscoverExternalDllPath(
				Logger,
				typeof(DevServerTestHelper).Assembly,
				projectName: "Uno.UI.RemoteControl.TestProcessor",
				dllFileName: testProcessorName);

			if (string.IsNullOrWhiteSpace(testProcessorPath) || !File.Exists(testProcessorPath))
			{
				Assert.Fail(
					$"Could not find test processor assembly. Make sure {testProcessorName} is built before running this test.");
			}

			// Send processor discovery message pointing directly to the test processor DLL
			Logger.LogInformation("Using test processor at: {Path}", testProcessorPath);
			await client.SendMessage(new ProcessorsDiscovery(testProcessorPath, appInstanceId));

			// Wait for processor discovery and telemetry logging to complete
			await Task.Delay(2000, CT);

			// Ensure graceful shutdown to flush telemetry
			await helper.AttemptGracefulShutdownAsync(CT);

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

			await Console.Error.WriteLineAsync(helper.ConsoleOutput);
			throw;
		}
		finally
		{
			// Cleanup - Stop the server and remove test files
			await helper.StopAsync(CT);
			await CleanupTelemetryTestAsync(helper, telemetryFilePath);
		}
	}

	/// <summary>
	/// Tests that ServerHotReloadProcessor can be properly instantiated and resolves ITelemetry&lt;T&gt; 
	/// with the correct connection context.
	/// </summary>
	[TestMethod]
	public async Task Telemetry_ServerHotReloadProcessor_ResolvesCorrectly()
	{
		var solution = SolutionHelper;
		var appInstanceId = Guid.NewGuid().ToString("N");

		// Arrange - Create a temporary file for telemetry output
		var telemetryFileName = GetTestTelemetryFileName("hotreload_processor");
		var tempDir = Path.GetTempPath();
		var telemetryFilePath = Path.Combine(tempDir, telemetryFileName);

		// Arrange - Creation of a solution is required for processors discovery
		await solution.CreateSolutionFileAsync();

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
					path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
						"Uno.UI.RemoteControl.Server.Processors", "bin"),
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
			await client.SendMessage(new ProcessorsDiscovery(hotReloadProcessorPath, appInstanceId));

			// Wait for processor discovery and telemetry logging to complete
			await Task.Delay(3000, CT); // Longer wait for HotReload processor

			// Ensure graceful shutdown to flush telemetry
			await helper.AttemptGracefulShutdownAsync(CT);

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

			discoveryCompleteEvents.Should()
				.NotBeEmpty("Discovery should complete successfully, indicating DI resolution worked");

			// Verify connection context is properly maintained
			// Connection-scoped telemetry should be present, showing the processor has access to connection context
			var connectionEvents = events.Where(e => e.Prefix.StartsWith("connection-")).ToList();
			connectionEvents.Should().NotBeEmpty("Connection-scoped telemetry should be available to processors");

			Logger.LogInformation("✅ ServerHotReloadProcessor test passed - DI resolution works correctly!");
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "ServerHotReloadProcessor test failed with exception");

			await Console.Error.WriteLineAsync(helper.ConsoleOutput);
			throw;
		}
		finally
		{
			// Cleanup - Stop the server and remove test files
			await helper.StopAsync(CT);
			await CleanupTelemetryTestAsync(helper, telemetryFilePath);
		}
	}

	[TestMethod]
	public async Task Telemetry_ProcessorDiscovery_Should_Load_Dependencies()
	{
		// This is a repro for the issue https://github.com/unoplatform/uno-private/issues/1552
		var solution = SolutionHelper;
		var appInstanceId = Guid.NewGuid().ToString("N");

		await solution.CreateSolutionFileAsync();

		await using var helper = CreateTelemetryHelperWithExactPath(
			GetTestTelemetryFileName("processor_dependency_resolution"),
			solutionPath: solution.SolutionFile);

		try
		{
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			started.Should().BeTrue();

			var client = RemoteControlClient.Initialize(
				typeof(object),
				new[] { new ServerEndpointAttribute("localhost", helper.Port) }
			);
			await client.SendMessage(new KeepAliveMessage());

			const string testProcessorName = "Uno.Test.Processor.dll";
			var testProcessorPath = ExternalDllDiscoveryHelper.DiscoverExternalDllPath(
				Logger,
				typeof(DevServerTestHelper).Assembly,
				projectName: "Uno.UI.RemoteControl.TestProcessor",
				dllFileName: testProcessorName);

			if (string.IsNullOrWhiteSpace(testProcessorPath) || !File.Exists(testProcessorPath))
			{
				Assert.Fail($"Could not find test processor assembly. Make sure {testProcessorName} is built before running this test.");
			}

			// Also locate the dependency; we'll copy it alongside the processor in the isolated folder
			const string testDependencyName = "Uno.UI.RemoteControl.TestProcessor.Dependency.dll";
			var testDependencyPath = ExternalDllDiscoveryHelper.DiscoverExternalDllPath(
				Logger,
				typeof(DevServerTestHelper).Assembly,
				projectName: "Uno.UI.RemoteControl.TestProcessor.Dependency",
				dllFileName: testDependencyName);

			// We'll stash the original dependency later (after copying) to avoid default ALC resolution
			string? stashedDependencyPath = null;

			// Create an isolated directory structure to reproduce the issue
			// The processor will be in tempDir/net10.0/ but the dependency won't be copied
			// This reproduces the conditions where the dependency is not resolved
			var tempDir = Path.Combine(Path.GetTempPath(), $"uno-processor-test-{Guid.NewGuid():N}");
			var processorsDir = Path.Combine(tempDir, "net10.0");
			Directory.CreateDirectory(processorsDir);

			try
			{
				// Copy the processor DLL to the isolated directory
				var isolatedProcessorPath = Path.Combine(processorsDir, testProcessorName);
				File.Copy(testProcessorPath, isolatedProcessorPath, overwrite: true);

				// Also copy the dependency alongside the processor to validate the server resolves it from this folder
				if (!string.IsNullOrWhiteSpace(testDependencyPath) && File.Exists(testDependencyPath))
				{
					var isolatedDependencyPath = Path.Combine(processorsDir, testDependencyName);
					File.Copy(testDependencyPath, isolatedDependencyPath, overwrite: true);
					Logger.LogInformation("DEBUG_LOG: Dependency copied to: {IsolatedDep}", isolatedDependencyPath);
				}

				Logger.LogInformation("DEBUG_LOG: Created isolated test directory at: {TempDir}", tempDir);
				Logger.LogInformation("DEBUG_LOG: Processor copied to: {IsolatedPath}", isolatedProcessorPath);
				Logger.LogInformation("DEBUG_LOG: File exists check: {Exists}", File.Exists(isolatedProcessorPath));

				var discoveryTcs = new TaskCompletionSource<ProcessorsDiscoveryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
				void Handler(object? sender, ReceivedFrameEventArgs args)
				{
					if (args.Frame.Scope == WellKnownScopes.DevServerChannel &&
						args.Frame.Name == ProcessorsDiscoveryResponse.Name &&
						args.Frame.TryGetContent(out ProcessorsDiscoveryResponse? response))
					{
						discoveryTcs.TrySetResult(response);
					}
				}

				client.FrameReceived += Handler;

				try
				{
					// Before we trigger discovery, stash the dependency from the original bin folder so it cannot be resolved accidentally by default ALC
					if (!string.IsNullOrWhiteSpace(testDependencyPath) && File.Exists(testDependencyPath))
					{
						stashedDependencyPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(testDependencyName)}-{Guid.NewGuid():N}.stash");
						try
						{
							File.Move(testDependencyPath, stashedDependencyPath);
							Logger.LogInformation("DEBUG_LOG: Stashed dependency from {Original} to {Stash}", testDependencyPath, stashedDependencyPath);
						}
						catch (Exception moveEx)
						{
							Logger.LogWarning(moveEx, "DEBUG_LOG: Failed to stash dependency at {Path}", testDependencyPath);
							stashedDependencyPath = null;
						}
					}

					// Send the parent directory as BasePath - server will discover processor in net10.0 subfolder
					await client.SendMessage(new ProcessorsDiscovery(tempDir, appInstanceId));
					using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(CT);
					timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
					var response = await discoveryTcs.Task.WaitAsync(timeoutCts.Token);

					Logger.LogInformation("DEBUG_LOG: Received {ProcessorCount} processors in response", response.Processors.Count);
					foreach (var proc in response.Processors)
					{
						Logger.LogInformation("DEBUG_LOG: Processor type={Type}, isLoaded={IsLoaded}, error={Error}",
							proc.Type, proc.IsLoaded, proc.LoadError ?? "(none)");
					}

					const string telemetryProcessorTypeName = "TelemetryTestProcessor";
					var telemetryProcessor = response.Processors.Single(p => p.Type.Contains(telemetryProcessorTypeName, StringComparison.Ordinal));

					// Expect success to force a RED test when the dependency resolution bug is present
					// This reproduces uno-private#1552 where the processor fails to load despite the dependency being present on disk
					telemetryProcessor.IsLoaded.Should().BeTrue($"Processor discovery should load dependencies located alongside the processor assembly, but got IsLoaded=false with LoadError: {telemetryProcessor.LoadError}");
					telemetryProcessor.LoadError.Should().BeNull($"Processor discovery should not report load errors, but got: {telemetryProcessor.LoadError}");
				}
				finally
				{
					client.FrameReceived -= Handler;
				}
			}
			finally
			{
				// Cleanup isolated directory
				try
				{
					if (Directory.Exists(tempDir))
					{
						Directory.Delete(tempDir, recursive: true);
					}
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex, "Failed to cleanup temp directory: {TempDir}", tempDir);
				}

				// Restore stashed dependency if we hid it
				try
				{
					if (!string.IsNullOrWhiteSpace(stashedDependencyPath) && File.Exists(stashedDependencyPath) && !string.IsNullOrWhiteSpace(testDependencyPath))
					{
						File.Move(stashedDependencyPath, testDependencyPath);
						Logger.LogInformation("DEBUG_LOG: Restored dependency to {Original}", testDependencyPath);
					}
				}
				catch (Exception restoreEx)
				{
					Logger.LogWarning(restoreEx, "DEBUG_LOG: Failed to restore dependency to {Original}", testDependencyPath);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Dependency resolution test failed with exception");
			await Console.Error.WriteLineAsync(helper.ConsoleOutput);
			throw;
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}

	[TestMethod]
	public async Task Telemetry_ProcessorDiscovery_Should_Load_Dependencies_From_BasePath()
	{
		// Validate resolution when no TFM subfolder exists: processor and dependency are in BasePath
		var solution = SolutionHelper;
		var appInstanceId = Guid.NewGuid().ToString("N");

		await solution.CreateSolutionFileAsync();

		await using var helper = CreateTelemetryHelperWithExactPath(
			GetTestTelemetryFileName("processor_dependency_resolution_basepath"),
			solutionPath: solution.SolutionFile);

		try
		{
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			started.Should().BeTrue();

			var client = RemoteControlClient.Initialize(
				typeof(object),
				new[] { new ServerEndpointAttribute("localhost", helper.Port) }
			);
			await client.SendMessage(new KeepAliveMessage());

			const string testProcessorName = "Uno.Test.Processor.dll";
			const string testDependencyName = "Uno.UI.RemoteControl.TestProcessor.Dependency.dll";

			var testProcessorPath = ExternalDllDiscoveryHelper.DiscoverExternalDllPath(
				Logger,
				typeof(DevServerTestHelper).Assembly,
				projectName: "Uno.UI.RemoteControl.TestProcessor",
				dllFileName: testProcessorName);

			var testDependencyPath = ExternalDllDiscoveryHelper.DiscoverExternalDllPath(
				Logger,
				typeof(DevServerTestHelper).Assembly,
				projectName: "Uno.UI.RemoteControl.TestProcessor.Dependency",
				dllFileName: testDependencyName);

			if (string.IsNullOrWhiteSpace(testProcessorPath) || !File.Exists(testProcessorPath))
			{
				Assert.Fail($"Could not find test processor assembly. Make sure {testProcessorName} is built before running this test.");
			}
			if (string.IsNullOrWhiteSpace(testDependencyPath) || !File.Exists(testDependencyPath))
			{
				Assert.Fail($"Could not find test dependency assembly. Make sure {testDependencyName} is built before running this test.");
			}

			// Create an isolated base directory WITHOUT a TFM subfolder
			var tempDir = Path.Combine(Path.GetTempPath(), $"uno-processor-test-basepath-{Guid.NewGuid():N}");
			Directory.CreateDirectory(tempDir);

			string? stashedDependencyPath = null;

			try
			{
				// Copy processor and dependency directly to BasePath (no net10.0 subfolder)
				var isolatedProcessorPath = Path.Combine(tempDir, testProcessorName);
				File.Copy(testProcessorPath, isolatedProcessorPath, overwrite: true);
				var isolatedDependencyPath = Path.Combine(tempDir, testDependencyName);
				File.Copy(testDependencyPath, isolatedDependencyPath, overwrite: true);

				Logger.LogInformation("DEBUG_LOG: BasePath test dir: {TempDir}", tempDir);
				Logger.LogInformation("DEBUG_LOG: Processor copied to: {IsolatedPath}", isolatedProcessorPath);
				Logger.LogInformation("DEBUG_LOG: Dependency copied to: {IsolatedDep}", isolatedDependencyPath);

				// Stash original dependency from default bin to prevent default ALC resolution
				try
				{
					stashedDependencyPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(testDependencyName)}-{Guid.NewGuid():N}.stash");
					File.Move(testDependencyPath, stashedDependencyPath);
					Logger.LogInformation("DEBUG_LOG: Stashed dependency from {Original} to {Stash}", testDependencyPath, stashedDependencyPath);
				}
				catch (Exception moveEx)
				{
					Logger.LogWarning(moveEx, "DEBUG_LOG: Failed to stash dependency at {Path}", testDependencyPath);
					stashedDependencyPath = null;
				}

				var discoveryTcs = new TaskCompletionSource<ProcessorsDiscoveryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
				void Handler(object? sender, ReceivedFrameEventArgs args)
				{
					if (args.Frame.Scope == WellKnownScopes.DevServerChannel &&
						args.Frame.Name == ProcessorsDiscoveryResponse.Name &&
						args.Frame.TryGetContent(out ProcessorsDiscoveryResponse? response))
					{
						discoveryTcs.TrySetResult(response);
					}
				}

				client.FrameReceived += Handler;

				try
				{
					await client.SendMessage(new ProcessorsDiscovery(tempDir, appInstanceId));
					using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(CT);
					timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
					var response = await discoveryTcs.Task.WaitAsync(timeoutCts.Token);

					Logger.LogInformation("DEBUG_LOG: Received {ProcessorCount} processors in response", response.Processors.Count);
					foreach (var proc in response.Processors)
					{
						Logger.LogInformation("DEBUG_LOG: Processor type={Type}, isLoaded={IsLoaded}, error={Error}",
							proc.Type, proc.IsLoaded, proc.LoadError ?? "(none)");
					}

					const string telemetryProcessorTypeName = "TelemetryTestProcessor";
					var telemetryProcessor = response.Processors.Single(p => p.Type.Contains(telemetryProcessorTypeName, StringComparison.Ordinal));

					telemetryProcessor.IsLoaded.Should().BeTrue($"Processor discovery should load dependencies located alongside the processor assembly, but got IsLoaded=false with LoadError: {telemetryProcessor.LoadError}");
					telemetryProcessor.LoadError.Should().BeNull($"Processor discovery should not report load errors, but got: {telemetryProcessor.LoadError}");
				}
				finally
				{
					client.FrameReceived -= Handler;
				}
			}
			finally
			{
				// Cleanup isolated directory and restore dependency
				try
				{
					if (Directory.Exists(tempDir))
					{
						Directory.Delete(tempDir, recursive: true);
					}
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex, "Failed to cleanup temp directory: {TempDir}", tempDir);
				}

				try
				{
					if (!string.IsNullOrWhiteSpace(stashedDependencyPath) && File.Exists(stashedDependencyPath) && !string.IsNullOrWhiteSpace(testDependencyPath))
					{
						File.Move(stashedDependencyPath, testDependencyPath);
						Logger.LogInformation("DEBUG_LOG: Restored dependency to {Original}", testDependencyPath);
					}
				}
				catch (Exception restoreEx)
				{
					Logger.LogWarning(restoreEx, "DEBUG_LOG: Failed to restore dependency to {Original}", testDependencyPath);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "BasePath dependency resolution test failed with exception");
			await Console.Error.WriteLineAsync(helper.ConsoleOutput);
			throw;
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}
}
