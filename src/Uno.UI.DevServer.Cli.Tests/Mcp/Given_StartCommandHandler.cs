using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_StartCommandHandler
{
	[TestMethod]
	[Description("Direct launch args must NOT contain --command when the start command is used")]
	public void BuildDirectLaunchArgs_DoesNotContainCommand()
	{
		var handler = CreateHandler();
		var parsed = StartCommandHandler.ParseStartArgs(
			["start", "--httpPort", "50000", "--solution", "/app/app.sln", "--ideChannel", "test-guid"]);

		var args = handler.BuildDirectLaunchArgs(
			"/path/to/host.dll", parsed, addins: null, workingDirectory: "/app");

		var joinedArgs = args.Arguments;
		joinedArgs.Should().NotContain("--command");
	}

	[TestMethod]
	[Description("Direct launch args must forward --ideChannel to the host process")]
	public void BuildDirectLaunchArgs_ForwardsIdeChannel()
	{
		var handler = CreateHandler();
		var parsed = StartCommandHandler.ParseStartArgs(
			["start", "--httpPort", "50000", "--solution", "/app/app.sln", "--ideChannel", "test-guid"]);

		var args = handler.BuildDirectLaunchArgs(
			"/path/to/host.dll", parsed, addins: null, workingDirectory: "/app");

		var joinedArgs = args.Arguments;
		joinedArgs.Should().Contain("--ideChannel");
		joinedArgs.Should().Contain("test-guid");
	}

	[TestMethod]
	[Description("Direct launch args must forward --addins when provided")]
	public void BuildDirectLaunchArgs_ForwardsAddins()
	{
		var handler = CreateHandler();
		var parsed = StartCommandHandler.ParseStartArgs(
			["start", "--httpPort", "50000", "--solution", "/app/app.sln"]);

		var args = handler.BuildDirectLaunchArgs(
			"/path/to/host.dll", parsed, addins: "/path/to/addin.dll", workingDirectory: "/app");

		var joinedArgs = args.Arguments;
		joinedArgs.Should().Contain("--addins");
		joinedArgs.Should().Contain("/path/to/addin.dll");
	}

	[TestMethod]
	[Description("Direct launch args must forward --metadata-updates when present in original args")]
	public void BuildDirectLaunchArgs_ForwardsMetadataUpdates()
	{
		var handler = CreateHandler();
		var parsed = StartCommandHandler.ParseStartArgs(
			["start", "--httpPort", "50000", "--solution", "/app/app.sln", "--metadata-updates", "true"]);

		var args = handler.BuildDirectLaunchArgs(
			"/path/to/host.dll", parsed, addins: null, workingDirectory: "/app");

		var joinedArgs = args.Arguments;
		joinedArgs.Should().Contain("--metadata-updates");
		joinedArgs.Should().Contain("true");
	}

	[TestMethod]
	[Description("Direct launch args must include --httpPort and --solution")]
	public void BuildDirectLaunchArgs_IncludesRequiredArgs()
	{
		var handler = CreateHandler();
		var parsed = StartCommandHandler.ParseStartArgs(
			["start", "--httpPort", "50000", "--solution", "/app/app.sln"]);

		var args = handler.BuildDirectLaunchArgs(
			"/path/to/host.dll", parsed, addins: null, workingDirectory: "/app");

		var joinedArgs = args.Arguments;
		joinedArgs.Should().Contain("--httpPort");
		joinedArgs.Should().Contain("50000");
		joinedArgs.Should().Contain("--solution");
		joinedArgs.Should().Contain("/app/app.sln");
	}

	[TestMethod]
	[Description("ParseStartArgs extracts known parameters from the argument list")]
	public void ParseStartArgs_ExtractsKnownArgs()
	{
		var parsed = StartCommandHandler.ParseStartArgs(
			["start", "--httpPort", "8080", "--solution", "/app/app.sln",
			 "--ideChannel", "my-channel", "--ppid", "1234",
			 "--metadata-updates", "true", "--solution-dir", "/app"]);

		parsed.HttpPort.Should().Be(8080);
		parsed.Solution.Should().Be("/app/app.sln");
		parsed.IdeChannel.Should().Be("my-channel");
		parsed.ParentPid.Should().Be(1234);
		parsed.PassthroughArgs.Should().Contain("--metadata-updates");
		parsed.PassthroughArgs.Should().Contain("true");
	}

	[TestMethod]
	[Description("ParseStartArgs defaults httpPort to 0 when not specified")]
	public void ParseStartArgs_DefaultsPortToZero()
	{
		var parsed = StartCommandHandler.ParseStartArgs(["start", "--solution", "/app/app.sln"]);

		parsed.HttpPort.Should().Be(0);
	}

	[TestMethod]
	[Description("When an existing server is found by solution with ideChannel, handler should rebind and skip spawn")]
	public async Task RunAsync_ExistingServerBySolution_RebindsIdeChannel()
	{
		var rebindCalled = false;
		var handler = CreateHandler(
			existingServerBySolution: new FakeServerInfo(ProcessId: 100, Port: 9000, SolutionPath: "/app/app.sln", ParentProcessId: Environment.ProcessId),
			onRebindIdeChannel: (port, channel) =>
			{
				rebindCalled = true;
				port.Should().Be(9000);
				channel.Should().Be("test-channel");
				return Task.FromResult(true);
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--solution", "/app/app.sln", "--ideChannel", "test-channel"],
			workingDirectory: "/app",
			addins: null);

		exitCode.Should().Be(0);
		rebindCalled.Should().BeTrue();
	}

	[TestMethod]
	[Description("When no existing server is found, handler should spawn a process in direct mode")]
	public async Task RunAsync_NoExistingServer_SpawnsDirectProcess()
	{
		ProcessStartInfo? capturedStartInfo = null;
		var handler = CreateHandler(
			onSpawnProcess: psi =>
			{
				capturedStartInfo = psi;
				return Task.FromResult(DirectSpawnResult.Ready);
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--solution", "/app/app.sln", "--ideChannel", "test-channel"],
			workingDirectory: "/app",
			addins: null);

		capturedStartInfo.Should().NotBeNull();
		var joinedArgs = capturedStartInfo!.Arguments;
		joinedArgs.Should().NotContain("--command");
		joinedArgs.Should().Contain("--ideChannel");
		joinedArgs.Should().Contain("test-channel");
	}

	[TestMethod]
	[Description("Non-start commands (stop, list, cleanup) should still use --command= via BuildHostArgs")]
	public void BuildHostArgs_NonStartCommands_UseCommandFlag()
	{
		var handler = CreateHandler();

		var args = handler.BuildControllerModeArgs(
			"/path/to/host.dll", ["list"], workingDirectory: "/app", addins: null);

		var joinedArgs = args.Arguments;
		joinedArgs.Should().Contain("--command=list");
	}

	[TestMethod]
	[Description("When --solution is absent from CLI args, resolvedSolutionPath should be used as fallback")]
	public async Task RunAsync_NoSolutionArg_UsesResolvedSolutionPath()
	{
		ProcessStartInfo? capturedStartInfo = null;
		var handler = CreateHandler(
			onSpawnProcess: psi =>
			{
				capturedStartInfo = psi;
				return Task.FromResult(DirectSpawnResult.Ready);
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--ideChannel", "test-channel"],
			workingDirectory: "/app",
			addins: null,
			resolvedSolutionPath: "/app/auto-discovered.sln");

		capturedStartInfo.Should().NotBeNull();
		var joinedArgs = capturedStartInfo!.Arguments;
		joinedArgs.Should().Contain("--solution");
		joinedArgs.Should().Contain("/app/auto-discovered.sln");
	}

	[TestMethod]
	[Description("When --solution IS in CLI args, resolvedSolutionPath should NOT override it")]
	public async Task RunAsync_ExplicitSolutionArg_IgnoresResolvedSolutionPath()
	{
		ProcessStartInfo? capturedStartInfo = null;
		var handler = CreateHandler(
			onSpawnProcess: psi =>
			{
				capturedStartInfo = psi;
				return Task.FromResult(DirectSpawnResult.Ready);
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--solution", "/app/explicit.sln", "--ideChannel", "test-channel"],
			workingDirectory: "/app",
			addins: null,
			resolvedSolutionPath: "/app/auto-discovered.sln");

		capturedStartInfo.Should().NotBeNull();
		var joinedArgs = capturedStartInfo!.Arguments;
		joinedArgs.Should().Contain("/app/explicit.sln");
		joinedArgs.Should().NotContain("auto-discovered");
	}

	[TestMethod]
	[Description("When --solution is absent and resolvedSolutionPath matches an existing server, handler should reuse it")]
	public async Task RunAsync_ResolvedSolution_FindsExistingServer()
	{
		var rebindCalled = false;
		var handler = CreateHandler(
			existingServerBySolution: new FakeServerInfo(ProcessId: 100, Port: 9000, SolutionPath: "/app/auto-discovered.sln", ParentProcessId: Environment.ProcessId),
			onRebindIdeChannel: (port, channel) =>
			{
				rebindCalled = true;
				port.Should().Be(9000);
				return Task.FromResult(true);
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--ideChannel", "test-channel"],
			workingDirectory: "/app",
			addins: null,
			resolvedSolutionPath: "/app/auto-discovered.sln");

		exitCode.Should().Be(0);
		rebindCalled.Should().BeTrue();
	}

	// --- Safe-mode retry (uno-private#1968) ---

	[TestMethod]
	[Description("When the host dies before becoming ready and add-ins were in play, the handler must retry exactly once in safe mode (--addins with the zero-paths sentinel) and succeed.")]
	public async Task RunAsync_HostDiesBeforeReady_RetriesOnceInSafeMode()
	{
		var spawns = new List<ProcessStartInfo>();
		var results = new Queue<DirectSpawnResult>([
			new DirectSpawnResult(1, DirectSpawnFailure.DiedBeforeReady, -532462766, "Unhandled exception. FileNotFoundException: System.Text.Encodings.Web"),
			DirectSpawnResult.Ready,
		]);

		var handler = CreateHandler(
			onSpawnProcess: psi =>
			{
				spawns.Add(psi);
				return Task.FromResult(results.Dequeue());
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--solution", "/app/app.sln"],
			workingDirectory: "/app",
			addins: "/path/to/addin.dll");

		exitCode.Should().Be(0, "the safe-mode attempt succeeded");
		spawns.Should().HaveCount(2, "one normal attempt, one safe-mode retry");
		spawns[0].Arguments.Should().Contain("/path/to/addin.dll");
		spawns[1].Arguments.Should().Contain($"--addins {StartCommandHandler.SafeModeAddInsValue}",
			"the retry must disable add-ins via the zero-paths sentinel");
		spawns[1].Arguments.Should().NotContain("/path/to/addin.dll");
	}

	[TestMethod]
	[Description("When the safe-mode attempt also dies, there must be no third attempt — the crash is not add-in related and the failure exit code propagates.")]
	public async Task RunAsync_SafeModeAlsoDies_NoThirdAttempt()
	{
		var spawnCount = 0;
		var handler = CreateHandler(
			onSpawnProcess: _ =>
			{
				spawnCount++;
				return Task.FromResult(new DirectSpawnResult(1, DirectSpawnFailure.DiedBeforeReady, -1, "boom"));
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--solution", "/app/app.sln"],
			workingDirectory: "/app",
			addins: null);

		exitCode.Should().Be(1);
		spawnCount.Should().Be(2, "exactly one normal attempt and one safe-mode retry, never more");
	}

	[TestMethod]
	[Description("When neither pre-resolved add-ins nor a solution are in play, the host loads no add-ins — a startup crash must not trigger a pointless safe-mode retry.")]
	public async Task RunAsync_NoAddInsInPlay_NoSafeModeRetry()
	{
		var spawnCount = 0;
		var handler = CreateHandler(
			onSpawnProcess: _ =>
			{
				spawnCount++;
				return Task.FromResult(new DirectSpawnResult(1, DirectSpawnFailure.DiedBeforeReady, -1, "boom"));
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000"],
			workingDirectory: "/app",
			addins: null);

		exitCode.Should().Be(1);
		spawnCount.Should().Be(1);
	}

	[TestMethod]
	[Description("A readiness timeout (process alive, port unreachable) is a different failure class — no safe-mode retry.")]
	public async Task RunAsync_ReadinessTimeout_NoSafeModeRetry()
	{
		var spawnCount = 0;
		var handler = CreateHandler(
			onSpawnProcess: _ =>
			{
				spawnCount++;
				return Task.FromResult(new DirectSpawnResult(1, DirectSpawnFailure.ReadinessTimeout, null, ""));
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/path/to/host.dll",
			originalArgs: ["start", "--httpPort", "50000", "--solution", "/app/app.sln"],
			workingDirectory: "/app",
			addins: "/path/to/addin.dll");

		exitCode.Should().Be(1);
		spawnCount.Should().Be(1);
	}

	[TestMethod]
	[DataRow((int)DirectSpawnFailure.DiedBeforeReady, "/a.dll", null, true, DisplayName = "died + pre-resolved add-ins → retry")]
	[DataRow((int)DirectSpawnFailure.DiedBeforeReady, null, "/app.sln", true, DisplayName = "died + solution discovery → retry")]
	[DataRow((int)DirectSpawnFailure.DiedBeforeReady, null, null, false, DisplayName = "died + nothing to load → no retry")]
	[DataRow((int)DirectSpawnFailure.DiedBeforeReady, ";", "/app.sln", false, DisplayName = "died in safe mode → no second retry")]
	[DataRow((int)DirectSpawnFailure.ReadinessTimeout, "/a.dll", null, false, DisplayName = "timeout → no retry")]
	[DataRow((int)DirectSpawnFailure.None, "/a.dll", null, false, DisplayName = "success → no retry")]
	[Description("Decision matrix for the safe-mode retry: only a died-before-ready crash with add-ins actually in play qualifies. The failure kind is passed as int because the enum is internal to the CLI assembly while MSTest requires a public method signature.")]
	public void ShouldRetryInSafeMode_DecisionMatrix(
		int failureKind, string? addins, string? solution, bool expected)
	{
		var failure = (DirectSpawnFailure)failureKind;
		var result = new DirectSpawnResult(
			failure == DirectSpawnFailure.None ? 0 : 1, failure, null, "");

		StartCommandHandler.ShouldRetryInSafeMode(result, addins, solution)
			.Should().Be(expected);
	}

	// --- Helpers ---

	private static StartCommandHandler CreateHandler(
		FakeServerInfo? existingServerBySolution = null,
		FakeServerInfo? existingServerByPort = null,
		Func<int, string, Task<bool>>? onRebindIdeChannel = null,
		Func<ProcessStartInfo, Task<DirectSpawnResult>>? onSpawnProcess = null)
	{
		return new StartCommandHandler(
			NullLogger.Instance,
			new FakeAmbientLookup(existingServerBySolution, existingServerByPort),
			onRebindIdeChannel ?? ((_, _) => Task.FromResult(true)),
			onSpawnProcess ?? (_ => Task.FromResult(DirectSpawnResult.Ready)));
	}

	internal record FakeServerInfo(int ProcessId, int Port, string? SolutionPath, int ParentProcessId = 0);

	private sealed class FakeAmbientLookup(
		FakeServerInfo? bySolution,
		FakeServerInfo? byPort) : IDevServerLookup
	{
		public (int ProcessId, int Port, string? SolutionPath, int ParentProcessId)? FindBySolution(string solution)
			=> bySolution is not null ? (bySolution.ProcessId, bySolution.Port, bySolution.SolutionPath, bySolution.ParentProcessId) : null;

		public (int ProcessId, int Port, string? SolutionPath, int ParentProcessId)? FindByPort(int port)
			=> byPort is not null ? (byPort.ProcessId, byPort.Port, byPort.SolutionPath, byPort.ParentProcessId) : null;
	}
}
