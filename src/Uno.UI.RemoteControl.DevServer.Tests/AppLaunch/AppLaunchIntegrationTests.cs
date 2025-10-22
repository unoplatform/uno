using System.Diagnostics;
using System.Text.Json;
using Uno.UI.RemoteControl.DevServer.Tests.Telemetry;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.AppLaunch;

[TestClass]
public class AppLaunchIntegrationTests : TelemetryTestBase
{
	private static readonly string? _serverProcessorAssembly = ExternalDllDiscoveryHelper.DiscoverExternalDllPath(
		Logger,
		typeof(DevServerTestHelper).Assembly,
		projectName: "Uno.UI.RemoteControl.Server.Processors",
		dllFileName: "Uno.UI.RemoteControl.Server.Processors.dll");

	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<AppLaunchIntegrationTests>(context);

	[TestMethod]
	public async Task WhenRegisteredAndRuntimeConnects_SuccessEventEmitted()
	{
		// PRE-ARRANGE: Create a solution file
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync();

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_success"));
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile, enableIdeChannel: false);

		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			var asm = typeof(AppLaunchIntegrationTests).Assembly;
			var mvid = ApplicationInfoHelper.GetMvid(asm);
			var platform = ApplicationInfoHelper.GetTargetPlatform(asm) is { } p ? "&platform=" + Uri.EscapeDataString(p) : null;
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Register app launch via HTTP GET (simulating IDE -> dev server)
			using (var http = new HttpClient())
			{
				var url = $"http://localhost:{helper.Port}/applaunch/{mvid}?isDebug={isDebug.ToString().ToLowerInvariant()}{platform}";
				var response = await http.GetAsync(url, CT);
				var body = await response.Content.ReadAsStringAsync();
				response.EnsureSuccessStatusCode();
			}

			// ACT - STEP 2: Connect from application (simulating app -> dev server)
			var rcClient = RemoteControlClient.Initialize(
				typeof(AppLaunchIntegrationTests),
				[new ServerEndpointAttribute("localhost", helper.Port)],
				_serverProcessorAssembly,
				autoRegisterAppIdentity: true);

			await WaitForClientConnectionAsync(rcClient, TimeSpan.FromSeconds(10));

			// ACT - STEP 3: Stop and gather telemetry events
			await Task.Delay(1500, CT);
			await helper.AttemptGracefulShutdownAsync(CT);

			// ASSERT
			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue();
			events.Should().NotBeEmpty();
			WriteEventsList(events);
			AssertHasEvent(events, "uno/dev-server/app-launch/launched");
			AssertHasEvent(events, "uno/dev-server/app-launch/connected");

			// Normal IDE-initiated launch: registration came before connection
			AssertEventHasProperty(events, "uno/dev-server/app-launch/connected", "WasIdeInitiated", "True");

			helper.ConsoleOutput.Length.Should().BeGreaterThan(0, "Dev server should produce some output");
		}
		finally
		{
			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	[TestMethod]
	public async Task WhenRegisteredByAssemblyPathAndRuntimeConnects_SuccessEventEmitted()
	{
		// PRE-ARRANGE: Create a solution file
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync();

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_success_by_path"));
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile, enableIdeChannel: false);

		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			var asm = typeof(AppLaunchIntegrationTests).Assembly;
			var asmPath = asm.Location;
			var platform = ApplicationInfoHelper.GetTargetPlatform(asm);
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Register app launch via HTTP GET using assembly path (new endpoint)
			using (var http = new HttpClient())
			{
				// The assembly path should be URL-encoded
				var encodedPath = Uri.EscapeDataString(asmPath);
				var url = $"http://localhost:{helper.Port}/applaunch/asm/{encodedPath}?IsDebug={isDebug.ToString().ToLowerInvariant()}";
				var response = await http.GetAsync(url, CT);
				response.EnsureSuccessStatusCode();
				TestContext!.WriteLine("Http Response: " + await response.Content.ReadAsStringAsync());
			}

			// ACT - STEP 2: Connect from application (simulating app -> dev server)
			var rcClient = RemoteControlClient.Initialize(
				typeof(AppLaunchIntegrationTests),
				[new ServerEndpointAttribute("localhost", helper.Port)],
				_serverProcessorAssembly,
				autoRegisterAppIdentity: true);

			await WaitForClientConnectionAsync(rcClient, TimeSpan.FromSeconds(10));

			// ACT - STEP 3: Stop and gather telemetry events
			await Task.Delay(1500, CT);
			await helper.AttemptGracefulShutdownAsync(CT);

			// ASSERT
			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue();
			events.Should().NotBeEmpty();
			WriteEventsList(events);
			AssertHasEvent(events, "uno/dev-server/app-launch/launched");
			AssertHasEvent(events, "uno/dev-server/app-launch/connected");

			// Normal IDE-initiated launch: registration came before connection
			AssertEventHasProperty(events, "uno/dev-server/app-launch/connected", "WasIdeInitiated", "True");

			helper.ConsoleOutput.Length.Should().BeGreaterThan(0, "Dev server should produce some output");
		}
		finally
		{
			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	[TestMethod]
	[Ignore]
	public async Task WhenRegisteredAndRuntimeConnects_SuccessEventEmitted_UsingIdeChannel()
	{
		// PRE-ARRANGE: Create a solution file
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync();

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_success_idechannel"));
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile, enableIdeChannel: true);

		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			var asm = typeof(AppLaunchIntegrationTests).Assembly;
			var mvid = ApplicationInfoHelper.GetMvid(asm);
			var platform = ApplicationInfoHelper.GetTargetPlatform(asm);
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Register app launch via IDE channel (IDE -> dev server)
			using (var ide = helper.CreateIdeChannelClient())
			{
				await ide.EnsureConnectedAsync(CT);
				await Task.Delay(1500, CT);
				await ide.SendToDevServerAsync(new AppLaunchRegisterIdeMessage(mvid, platform, isDebug, "UnitTestIDE", "unit-plugin"), CT);
			}

			// ACT - STEP 2: Connect from application (app -> dev server)
			var rcClient = RemoteControlClient.Initialize(
				typeof(AppLaunchIntegrationTests),
				[new ServerEndpointAttribute("localhost", helper.Port)],
				_serverProcessorAssembly,
				autoRegisterAppIdentity: true);

			await WaitForClientConnectionAsync(rcClient, TimeSpan.FromSeconds(10));

			// ACT - STEP 3: Stop and gather telemetry events
			await Task.Delay(1500, CT);
			await helper.AttemptGracefulShutdownAsync(CT);

			// ASSERT
			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue();
			events.Should().NotBeEmpty();
			WriteEventsList(events);
			AssertHasEvent(events, "uno/dev-server/app-launch/launched");
			AssertHasEvent(events, "uno/dev-server/app-launch/connected");

			helper.ConsoleOutput.Length.Should().BeGreaterThan(0, "Dev server should produce some output");
		}
		finally
		{
			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	[TestMethod]
	public async Task WhenRegisteredButNoConnection_TimeoutEventEmitted()
	{
		// PRE-ARRANGE: Create a solution file
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync();

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_timeout"));

		// Create helper with environment variable for short timeout (0.5 seconds)
		var envVars = new Dictionary<string, string>
		{
			{ "UNO_PLATFORM_TELEMETRY_FILE", filePath },
			{ "UNO_DEVSERVER_APPLAUNCH_TIMEOUT", "0.5" } // 0.5 second timeout
		};

		await using var helper = new DevServerTestHelper(Logger, solutionPath: solution.SolutionFile, environmentVariables: envVars);

		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			var asm = typeof(AppLaunchIntegrationTests).Assembly;
			var mvid = ApplicationInfoHelper.GetMvid(asm);
			var platform = ApplicationInfoHelper.GetTargetPlatform(asm) is { } p ? "&platform=" + Uri.EscapeDataString(p) : null;
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Register app launch via HTTP GET (simulating IDE -> dev server)
			using (var http = new HttpClient())
			{
				var url = $"http://localhost:{helper.Port}/applaunch/{mvid}?isDebug={isDebug.ToString().ToLowerInvariant()}{platform}";
				var response = await http.GetAsync(url, CT);
				var body = await response.Content.ReadAsStringAsync();
				response.EnsureSuccessStatusCode();
			}

			// ACT - STEP 2: Wait for timeout to occur
			await Task.Delay(5_000, CT); // Wait 5 seconds (should be far more than enough for the timeout to occur)

			// ACT - STEP 3: Stop and gather telemetry events
			await helper.AttemptGracefulShutdownAsync(CT);

			// ASSERT
			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue();
			events.Should().NotBeEmpty();
			WriteEventsList(events);
			AssertHasEvent(events, "uno/dev-server/app-launch/launched");
			AssertHasEvent(events, "uno/dev-server/app-launch/connection-timeout");

			helper.ConsoleOutput.Length.Should().BeGreaterThan(0, "Dev server should produce some output");
		}
		finally
		{
			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	[TestMethod]
	public async Task WhenConnectedBeforeRegistered_ThenAssociatedAfterRegistration()
	{
		// PRE-ARRANGE: Create a solution file
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync();

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_connected_before_registered"));
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile, enableIdeChannel: false);

		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			var asm = typeof(AppLaunchIntegrationTests).Assembly;
			var mvid = ApplicationInfoHelper.GetMvid(asm);
			var platform = ApplicationInfoHelper.GetTargetPlatform(asm);
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Connect from application FIRST (app -> dev server)
			var rcClient = RemoteControlClient.Initialize(
				typeof(AppLaunchIntegrationTests),
				[new ServerEndpointAttribute("localhost", helper.Port)],
				_serverProcessorAssembly,
				autoRegisterAppIdentity: true);

			await WaitForClientConnectionAsync(rcClient, TimeSpan.FromSeconds(10));

			// ACT - STEP 2: Register app launch LATER via HTTP GET (simulating late IDE -> dev server)
			using (var http = new HttpClient())
			{
				var platformQs = platform is { Length: > 0 } ? "&platform=" + Uri.EscapeDataString(platform) : string.Empty;
				var url = $"http://localhost:{helper.Port}/applaunch/{mvid}?isDebug={isDebug.ToString().ToLowerInvariant()}{platformQs}";
				var response = await http.GetAsync(url, CT);
				response.EnsureSuccessStatusCode();
			}

			// ACT - STEP 3: Stop and gather telemetry events
			await Task.Delay(1500, CT);
			await helper.AttemptGracefulShutdownAsync(CT);

			// ASSERT
			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue();
			events.Should().NotBeEmpty();
			WriteEventsList(events);

			// Desired behavior: even if connection arrived before registration, we should eventually see both events
			AssertHasEvent(events, "uno/dev-server/app-launch/launched");
			AssertHasEvent(events, "uno/dev-server/app-launch/connected");

			// The connection should be flagged as IDE-initiated since a registration was received (even if late)
			AssertEventHasProperty(events, "uno/dev-server/app-launch/connected", "WasIdeInitiated", "True");
		}
		finally
		{
			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	[TestMethod]
	public async Task WhenConnectedWithoutRegistration_ThenClassifiedAsNonIdeLaunch()
	{
		// PRE-ARRANGE: Create a solution file
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync();

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_unsolicited_connection"));
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile, enableIdeChannel: false);

		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			// ACT - connect an app but never register a launch (unsolicited connection)
			var rcClient = RemoteControlClient.Initialize(
				typeof(AppLaunchIntegrationTests),
				[new ServerEndpointAttribute("localhost", helper.Port)],
				_serverProcessorAssembly,
				autoRegisterAppIdentity: true);

			await WaitForClientConnectionAsync(rcClient, TimeSpan.FromSeconds(10));

			// Allow some time for the server to potentially classify this case
			await Task.Delay(3000, CT);

			// Stop and gather telemetry
			await helper.AttemptGracefulShutdownAsync(CT);

			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue();
			events.Should().NotBeEmpty();
			WriteEventsList(events);

			// Desired behavior: an unsolicited connection should still be logged as a connection, even without IDE registration
			AssertHasEvent(events, "uno/dev-server/app-launch/connected");

			// CRITICAL: The connection should be flagged as NOT IDE-initiated (manual launch, F5 in browser, etc.)
			AssertEventHasProperty(events, "uno/dev-server/app-launch/connected", "WasIdeInitiated", "False");
		}
		finally
		{
			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	private static List<(string Prefix, JsonDocument Json)> ParseTelemetryFileIfExists(string path)
		=> File.Exists(path) ? ParseTelemetryEvents(File.ReadAllText(path)) : [];

	private static void DeleteIfExists(string path)
	{
		if (File.Exists(path)) { try { File.Delete(path); } catch { } }
	}
}
