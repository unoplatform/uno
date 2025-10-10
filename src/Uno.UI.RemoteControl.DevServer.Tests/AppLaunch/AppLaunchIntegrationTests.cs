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
			var platform = ApplicationInfoHelper.GetTargetPlatformOrDefault(asm);
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Register app launch via HTTP GET (simulating IDE -> dev server)
			using (var http = new HttpClient())
			{
				var url = $"http://localhost:{helper.Port}/applaunch/{mvid}?platform={Uri.EscapeDataString(platform)}&isDebug={isDebug.ToString().ToLowerInvariant()}";
				var response = await http.GetAsync(url, CT);
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
			var platform = ApplicationInfoHelper.GetTargetPlatformOrDefault(asm);
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
			var platform = ApplicationInfoHelper.GetTargetPlatformOrDefault(asm);
			var isDebug = Debugger.IsAttached;

			// ACT - STEP 1: Register app launch via IDE channel (IDE -> dev server)
			using (var ide = helper.CreateIdeChannelClient())
			{
				await ide.EnsureConnectedAsync(CT);
				await Task.Delay(1500, CT);
				await ide.SendToDevServerAsync(new AppLaunchRegisterIdeMessage(mvid, platform, isDebug), CT);
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

	private static List<(string Prefix, JsonDocument Json)> ParseTelemetryFileIfExists(string path)
		=> File.Exists(path) ? ParseTelemetryEvents(File.ReadAllText(path)) : [];

	private static void DeleteIfExists(string path)
	{
		if (File.Exists(path)) { try { File.Delete(path); } catch { } }
	}
}
