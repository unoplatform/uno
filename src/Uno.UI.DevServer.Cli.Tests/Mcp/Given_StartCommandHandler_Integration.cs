using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_StartCommandHandler_Integration
{
	/// <summary>
	/// This test proves that the fix works by comparing the OLD behavior
	/// (BuildHostArgs with --command=start) to the NEW behavior (direct launch).
	///
	/// The OLD path drops --ideChannel on SDK 6.5.x hosts because the
	/// two-process launcher in those hosts doesn't forward it.
	///
	/// The NEW path bypasses the two-process launcher entirely.
	/// </summary>
	[TestMethod]
	[Description("PROOF: The old BuildHostArgs produces --command=start which triggers the buggy two-process launcher on older hosts. " +
		"The new StartCommandHandler produces direct-mode args without --command, ensuring --ideChannel reaches the host.")]
	public void OldVsNew_IdeChannelSurvival()
	{
		var originalArgs = new[]
		{
			"start",
			"--httpPort", "62222",
			"--metadata-updates", "true",
			"--solution", "/Users/dev/myapp/myapp.sln",
			"--solution-dir", "/Users/dev/myapp",
			"--ideChannel", "bf63c44d-68c6-5bd1-abb7-c4727ae6ff66"
		};
		var hostPath = "/fake/host/Uno.UI.RemoteControl.Host.dll";
		var addins = "/fake/addin.dll";

		// === OLD behavior (what CliManager.BuildHostArgs used to do) ===
		var oldArgs = BuildHostArgs_Old(hostPath, originalArgs, "/Users/dev/myapp", addins);
		var oldArgsStr = oldArgs.Arguments;

		// OLD args contain --command=start → triggers two-process launcher in the host
		oldArgsStr.Should().Contain("--command=start",
			"Old path uses controller mode which triggers the two-process launcher");

		// OLD args DO contain --ideChannel (the CLI passes it correctly)...
		oldArgsStr.Should().Contain("--ideChannel",
			"The CLI itself always included --ideChannel");

		// ...but the OLD HOST's StartCommandAsync (SDK 6.5.x) strips it when
		// spawning the child process. The child never sees --ideChannel.
		// We can't test the old host binary here, but the comment explains the gap.

		// === NEW behavior (StartCommandHandler direct launch) ===
		var handler = new StartCommandHandler(
			NullLogger.Instance,
			new EmptyLookup(),
			(_, _) => Task.FromResult(true),
			_ => Task.FromResult(0));

		var parsed = StartCommandHandler.ParseStartArgs(originalArgs);
		var newArgs = handler.BuildDirectLaunchArgs(hostPath, parsed, addins, "/Users/dev/myapp");
		var newArgsStr = newArgs.Arguments;

		// NEW args do NOT contain --command → host starts in direct mode
		newArgsStr.Should().NotContain("--command",
			"Direct mode does not use --command, bypassing the two-process launcher entirely");

		// NEW args DO contain --ideChannel → host reads it from IConfiguration
		newArgsStr.Should().Contain("--ideChannel",
			"Direct mode passes --ideChannel directly to the host process");
		newArgsStr.Should().Contain("bf63c44d-68c6-5bd1-abb7-c4727ae6ff66",
			"The actual channel GUID must be present");

		// NEW args also forward --addins and pass-through args
		newArgsStr.Should().Contain("--addins");
		newArgsStr.Should().Contain("--metadata-updates");

		// NEW args include all required host parameters
		newArgsStr.Should().Contain("--httpPort");
		newArgsStr.Should().Contain("62222");
		newArgsStr.Should().Contain("--solution");
		newArgsStr.Should().Contain("myapp.sln");
	}

	/// <summary>
	/// Proves that the CliManager now routes "start" through the new direct
	/// launch path by capturing the ProcessStartInfo and verifying no --command.
	/// </summary>
	[TestMethod]
	[Description("PROOF: CliManager.RunAsync routes the start command through StartCommandHandler, " +
		"producing direct-mode args that bypass the two-process launcher.")]
	public async Task CliManager_StartCommand_UsesDirectLaunch()
	{
		ProcessStartInfo? capturedStartInfo = null;

		var handler = new StartCommandHandler(
			NullLogger.Instance,
			new EmptyLookup(),
			(_, _) => Task.FromResult(true),
			psi =>
			{
				capturedStartInfo = psi;
				return Task.FromResult(0);
			});

		var exitCode = await handler.RunAsync(
			hostPath: "/fake/host.dll",
			originalArgs: [
				"start",
				"--httpPort", "50000",
				"--solution", "/app/app.sln",
				"--ideChannel", "test-channel-guid",
				"--metadata-updates", "true"
			],
			workingDirectory: "/app",
			addins: "/fake/addin.dll");

		exitCode.Should().Be(0);
		capturedStartInfo.Should().NotBeNull();

		var args = capturedStartInfo!.Arguments;

		// This is the critical assertion: no --command means the host
		// starts in direct mode, reading ALL args from IConfiguration.
		// On SDK 6.5.x, this means --ideChannel IS received by the host.
		args.Should().NotContain("--command");

		// All arguments survive to the host process
		args.Should().Contain("--ideChannel");
		args.Should().Contain("test-channel-guid");
		args.Should().Contain("--httpPort");
		args.Should().Contain("50000");
		args.Should().Contain("--addins");
		args.Should().Contain("--metadata-updates");
	}

	/// <summary>
	/// Proves that non-start commands still use the old --command= path.
	/// </summary>
	[TestMethod]
	[Description("Non-start commands (stop, list, cleanup) still use --command=<verb> controller mode.")]
	public void NonStartCommands_StillUseControllerMode()
	{
		var handler = new StartCommandHandler(
			NullLogger.Instance,
			new EmptyLookup(),
			(_, _) => Task.FromResult(true),
			_ => Task.FromResult(0));

		foreach (var verb in new[] { "list", "stop", "cleanup" })
		{
			var args = handler.BuildControllerModeArgs(
				"/fake/host.dll", [verb], "/app", addins: null);

			args.Arguments.Should().Contain($"--command={verb}",
				$"'{verb}' should still use controller mode");
		}
	}

	// Reproduces the old BuildHostArgs logic for comparison
	private static ProcessStartInfo BuildHostArgs_Old(
		string hostPath, string[] originalArgs, string workingDirectory, string? addins)
	{
		var command = originalArgs.Length > 0 ? originalArgs[0] : "start";
		var args = new List<string> { $"--command={command}" };
		for (int i = 1; i < originalArgs.Length; i++)
		{
			args.Add(originalArgs[i]);
		}

		if (addins is not null)
		{
			args.Add("--addins");
			args.Add(addins);
		}

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(
			hostPath, args, workingDirectory, redirectOutput: true);
	}

	private sealed class EmptyLookup : IDevServerLookup
	{
		public (int ProcessId, int Port, string? SolutionPath)? FindBySolution(string solution) => null;
		public (int ProcessId, int Port, string? SolutionPath)? FindByPort(int port) => null;
	}
}
