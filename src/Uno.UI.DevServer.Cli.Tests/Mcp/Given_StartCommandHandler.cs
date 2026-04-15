using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

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
			existingServerBySolution: new FakeServerInfo(ProcessId: 100, Port: 9000, SolutionPath: "/app/app.sln"),
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
				return Task.FromResult(0);
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
				return Task.FromResult(0);
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
				return Task.FromResult(0);
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
			existingServerBySolution: new FakeServerInfo(ProcessId: 100, Port: 9000, SolutionPath: "/app/auto-discovered.sln"),
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

	// --- Helpers ---

	private static StartCommandHandler CreateHandler(
		FakeServerInfo? existingServerBySolution = null,
		FakeServerInfo? existingServerByPort = null,
		Func<int, string, Task<bool>>? onRebindIdeChannel = null,
		Func<ProcessStartInfo, Task<int>>? onSpawnProcess = null)
	{
		return new StartCommandHandler(
			NullLogger.Instance,
			new FakeAmbientLookup(existingServerBySolution, existingServerByPort),
			onRebindIdeChannel ?? ((_, _) => Task.FromResult(true)),
			onSpawnProcess ?? (_ => Task.FromResult(0)));
	}

	internal record FakeServerInfo(int ProcessId, int Port, string? SolutionPath);

	private sealed class FakeAmbientLookup(
		FakeServerInfo? bySolution,
		FakeServerInfo? byPort) : IDevServerLookup
	{
		public (int ProcessId, int Port, string? SolutionPath)? FindBySolution(string solution)
			=> bySolution is not null ? (bySolution.ProcessId, bySolution.Port, bySolution.SolutionPath) : null;

		public (int ProcessId, int Port, string? SolutionPath)? FindByPort(int port)
			=> byPort is not null ? (byPort.ProcessId, byPort.Port, byPort.SolutionPath) : null;
	}
}
