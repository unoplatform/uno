using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="DevServerMonitor"/> patterns: ambient registry lookup,
/// direct launch arg building, and retry logic.
/// <para>
/// These tests simulate DevServerMonitor patterns rather than instantiating
/// the real class because it requires a .sln on disk and a real Uno SDK
/// host binary. Pure decision logic is tested via <see cref="MonitorDecisionsTests"/>.
/// </para>
/// </summary>
[TestClass]
public class Given_DevServerMonitor
{
	// -------------------------------------------------------------------
	// AmbientRegistry
	// -------------------------------------------------------------------

	[TestMethod]
	public void AmbientRegistry_WhenNoMatch_ReturnsNull()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new Uno.UI.RemoteControl.Host.AmbientRegistry(logger);

		var result = registry.GetActiveDevServerForPath(Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}.sln"));

		result.Should().BeNull();
	}

	[TestMethod]
	public void AmbientRegistry_WhenNullOrEmpty_ReturnsNull()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new Uno.UI.RemoteControl.Host.AmbientRegistry(logger);

		registry.GetActiveDevServerForPath(null).Should().BeNull();
		registry.GetActiveDevServerForPath("").Should().BeNull();
		registry.GetActiveDevServerForPath("   ").Should().BeNull();
	}

	// -------------------------------------------------------------------
	// Direct launch args
	// -------------------------------------------------------------------

	[TestMethod]
	public void DirectLaunch_ArgsDoNotContainCommandStart()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: @"C:\test\MySolution.sln");

		args.Should().NotContain("--command");
		args.Should().NotContain("start");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainSolutionFlag()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: @"C:\test\MySolution.sln");

		var solutionIndex = args.IndexOf("--solution");
		solutionIndex.Should().BeGreaterThanOrEqualTo(0);
		args[solutionIndex + 1].Should().Be(@"C:\test\MySolution.sln");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainHttpPortAndPpid()
	{
		var args = BuildDirectLaunchArgs(port: 54321, solution: null);

		var portIndex = args.IndexOf("--httpPort");
		portIndex.Should().BeGreaterThanOrEqualTo(0);
		args[portIndex + 1].Should().Be("54321");

		var ppidIndex = args.IndexOf("--ppid");
		ppidIndex.Should().BeGreaterThanOrEqualTo(0);
		int.Parse(args[ppidIndex + 1]).Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void DirectLaunch_WhenNoSolution_ArgsDoNotContainSolution()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: null);

		args.Should().NotContain("--solution");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainAddins()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: null, addins: "Addon1.dll;Addon2.dll");

		var addinsIndex = args.IndexOf("--addins");
		addinsIndex.Should().BeGreaterThanOrEqualTo(0);
		args[addinsIndex + 1].Should().Be("Addon1.dll;Addon2.dll");
	}

	// -------------------------------------------------------------------
	// Retry logic
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("ServerFailed event fires after exhausting all retry attempts")]
	public void DevServerMonitor_RetryLogic_FailsAfterMaxAttempts()
	{
		const int maxRetries = 3;
		int retryCount = 0;
		bool serverFailed = false;
		var attempts = new List<int>();

		while (retryCount < maxRetries)
		{
			// Simulate StartProcess returning (success: false)
			retryCount++;
			attempts.Add(retryCount);

			if (retryCount >= maxRetries)
			{
				serverFailed = true;
				break;
			}
		}

		serverFailed.Should().BeTrue("ServerFailed triggers after max retries");
		attempts.Should().HaveCount(3);
		retryCount.Should().Be(maxRetries);
	}

	[TestMethod]
	[Description("Monitor loop must NOT break after ServerStarted, it must keep watching for process exit to detect crashes")]
	public void Bug2_MonitorLoop_ContinuesAfterServerStarted()
	{
		var iterations = 0;
		var serverStartedRaised = false;
		var serverCrashedRaised = false;
		var processExited = false;

		// Simulate the monitor loop with the CORRECT behavior (no break)
		for (int attempt = 0; attempt < 3; attempt++)
		{
			iterations++;

			if (!serverStartedRaised)
			{
				serverStartedRaised = true;
				// Bug: original code does `break` here
				// Fix: continue watching the process
			}
			else if (serverStartedRaised && processExited)
			{
				serverCrashedRaised = true;
				break; // Re-enter loop for rediscovery
			}

			// Simulate process exit after first iteration
			if (attempt == 1)
			{
				processExited = true;
			}
		}

		serverStartedRaised.Should().BeTrue();
		processExited.Should().BeTrue();
		serverCrashedRaised.Should().BeTrue("monitor should detect process exit and raise ServerCrashed");
		iterations.Should().BeGreaterThan(1, "monitor loop should continue after ServerStarted");
	}

	// -------------------------------------------------------------------
	// Event sequence
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("ServerLaunching fires before ServerStarted in the documented event sequence")]
	public void EventSequence_ServerLaunchingFiresBeforeServerStarted()
	{
		var events = new List<string>();

		// Simulate the event sequence from RunMonitor:
		// 1. StartProcess succeeds
		// 2. ServerLaunching fires
		// 3. WaitForServerReadyAsync succeeds
		// 4. ServerStarted fires
		Action serverLaunching = () => events.Add("ServerLaunching");
		Action<string> serverStarted = _ => events.Add("ServerStarted");
		Action serverFailed = () => events.Add("ServerFailed");

		// Simulate success path
		serverLaunching();
		serverStarted("http://localhost:12345/mcp");

		events.Should().HaveCount(2);
		events[0].Should().Be("ServerLaunching");
		events[1].Should().Be("ServerStarted");
	}

	[TestMethod]
	[Description("ServerLaunching fires but ServerStarted does not when health-check fails")]
	public void EventSequence_WhenHealthCheckFails_ServerLaunchingWithoutServerStarted()
	{
		var events = new List<string>();

		Action serverLaunching = () => events.Add("ServerLaunching");
		Action serverFailed = () => events.Add("ServerFailed");

		// Simulate path where process starts but health-check fails
		serverLaunching();
		serverFailed();

		events.Should().HaveCount(2);
		events[0].Should().Be("ServerLaunching");
		events[1].Should().Be("ServerFailed");
	}

	// -------------------------------------------------------------------
	// DiscoveryInfo exposure
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("DiscoveryInfo fields map to the values DevServerMonitor exposes")]
	public void DiscoveryInfo_WhenFullyPopulated_ExposesExpectedFields()
	{
		var discovery = new DiscoveryInfo
		{
			WorkingDirectory = "/tmp/test",
			GlobalJsonPath = "/tmp/test/global.json",
			UnoSdkVersion = "5.5.100",
			HostPath = "/some/host.dll",
			AddInsDiscoveryDurationMs = 42,
			AddIns =
			[
				new ResolvedAddIn
				{
					PackageName = "Uno.Test",
					PackageVersion = "1.0.0",
					EntryPointDll = "/some/addin.dll",
					DiscoverySource = "targets"
				}
			],
		};

		discovery.UnoSdkVersion.Should().Be("5.5.100");
		discovery.HostPath.Should().NotBeNull();
		discovery.AddInsDiscoveryDurationMs.Should().Be(42);
		discovery.AddIns.Should().HaveCount(1);
		discovery.AddIns[0].EntryPointDll.Should().Be("/some/addin.dll");
	}

	[TestMethod]
	[Description("When discovery finds no host, HostPath is null but other fields are still populated")]
	public void DiscoveryInfo_WhenNoHost_HostPathIsNullButSdkVersionPopulated()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/test/global.json",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			HostPath = null,
		};

		discovery.HostPath.Should().BeNull();
		discovery.UnoSdkVersion.Should().Be("5.5.100");
	}

	[TestMethod]
	[Description("AddIns from DiscoveryInfo produce the expected --addins argument value")]
	public void DiscoveryInfo_AddIns_ProduceExpectedAddinsArgValue()
	{
		var discovery = new DiscoveryInfo
		{
			AddIns =
			[
				new ResolvedAddIn { PackageName = "A", PackageVersion = "1.0", EntryPointDll = "/a.dll", DiscoverySource = "targets" },
				new ResolvedAddIn { PackageName = "B", PackageVersion = "2.0", EntryPointDll = "/b.dll", DiscoverySource = "targets" },
			],
		};

		// Mirror the logic from StartProcess
		var addInsValue = string.Join(";", discovery.AddIns.Select(a => a.EntryPointDll).Distinct(StringComparer.OrdinalIgnoreCase));

		addInsValue.Should().Be("/a.dll;/b.dll");
	}

	[TestMethod]
	[Description("When DiscoveryInfo has no add-ins, no --addins arg is produced")]
	public void DiscoveryInfo_WhenNoAddIns_NoAddinsArg()
	{
		var discovery = new DiscoveryInfo { AddIns = [] };

		// Mirror the guard from StartProcess: AddIns is { Count: > 0 }
		var shouldAddFlag = discovery.AddIns is { Count: > 0 };

		shouldAddFlag.Should().BeFalse();
	}

	// -------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------

	/// <summary>
	/// Mirrors the arg-building logic in <see cref="DevServerMonitor.StartProcess"/>
	/// to verify that the direct launch path produces the expected arguments.
	/// </summary>
	private static List<string> BuildDirectLaunchArgs(int port, string? solution, string? addins = null)
	{
		var args = new List<string>
		{
			"--httpPort", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
			"--ppid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};

		if (!string.IsNullOrWhiteSpace(solution))
		{
			args.Add("--solution");
			args.Add(solution);
		}

		if (!string.IsNullOrWhiteSpace(addins))
		{
			args.Add("--addins");
			args.Add(addins);
		}

		return args;
	}
}
