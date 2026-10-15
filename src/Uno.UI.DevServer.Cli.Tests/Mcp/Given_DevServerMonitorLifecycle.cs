using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_DevServerMonitorLifecycle
{
	[TestMethod]
	[Description("When an explicit solution is selected, DevServerMonitor launches that exact solution instead of an arbitrary directory scan result")]
	public void WhenWorkspaceResolutionHasSelectedSolution_UsesSelectedSolutionForLaunch()
	{
		var currentDirectory = CreatePath("repo");
		var selectedSolution = CreatePath("repo", "srcB", "AppB.slnx");
		string[] solutionFiles =
		[
			CreatePath("repo", "srcA", "AppA.slnx"),
			selectedSolution,
		];
		var workspaceResolution = new WorkspaceResolution
		{
			RequestedWorkingDirectory = currentDirectory,
			EffectiveWorkspaceDirectory = CreatePath("repo", "srcB"),
			SelectedSolutionPath = selectedSolution,
			SelectedGlobalJsonPath = CreatePath("repo", "srcB", "global.json"),
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = solutionFiles,
		};

		var solution = DevServerMonitor.DetermineSolutionToLaunch(solutionFiles, workspaceResolution);

		solution.Should().Be(selectedSolution);
	}

	[TestMethod]
	[Description("When no explicit solution is selected, DevServerMonitor chooses a deterministic default solution")]
	public void WhenWorkspaceResolutionDoesNotSelectSolution_UsesSortedSolutionOrder()
	{
		var currentDirectory = CreatePath("repo");
		string[] solutionFiles =
		[
			CreatePath("repo", "srcB", "AppB.slnx"),
			CreatePath("repo", "srcA", "AppA.slnx"),
		];

		var solution = DevServerMonitor.DetermineSolutionToLaunch(solutionFiles, workspaceResolution: null);

		solution.Should().Be(CreatePath("repo", "srcA", "AppA.slnx"));
	}

	[TestMethod]
	[Description("Selected-solution matching follows platform path semantics so Linux keeps case-sensitive distinctions")]
	public void WhenSelectedSolutionDiffersOnlyByCase_SelectionMatchesCurrentPlatformSemantics()
	{
		var currentDirectory = CreatePath("repo");
		var firstSolution = CreatePath("repo", "srcA", "AppA.slnx");
		var selectedSolution = CreatePath("repo", "srcB", "AppB.slnx");
		var requestedSolution = selectedSolution.ToUpperInvariant();
		string[] solutionFiles =
		[
			firstSolution,
			selectedSolution,
		];
		var workspaceResolution = new WorkspaceResolution
		{
			RequestedWorkingDirectory = currentDirectory,
			EffectiveWorkspaceDirectory = CreatePath("repo", "srcB"),
			SelectedSolutionPath = requestedSolution,
			SelectedGlobalJsonPath = CreatePath("repo", "srcB", "global.json"),
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = solutionFiles,
		};

		var solution = DevServerMonitor.DetermineSolutionToLaunch(solutionFiles, workspaceResolution);

		if (OperatingSystem.IsLinux())
		{
			solution.Should().Be(firstSolution);
		}
		else
		{
			solution.Should().Be(selectedSolution);
		}
	}

	[TestMethod]
	[Description("A resolved solution deeper than the monitor's rescan is launched, not the first shallower solution of the rescan (unoplatform/uno#25524)")]
	public void WhenSelectedSolutionIsBeyondTheRescan_UsesSelectedSolutionForLaunch()
	{
		var root = CreateTempDirectory();

		try
		{
			var shallowSolution = CreateFile(root, "a", "Lib", "Lib.slnx");
			var selectedSolution = CreateFile(root, "a", "b", "c", "d", "App.slnx");
			string[] solutionFiles = [shallowSolution];

			var solution = DevServerMonitor.DetermineSolutionToLaunch(solutionFiles, CreateResolution(root, selectedSolution, solutionFiles));

			solution.Should().Be(selectedSolution);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("A resolved solution that no longer exists falls back to the deterministic default instead of launching a missing file")]
	public void WhenSelectedSolutionNoLongerExists_UsesSortedSolutionOrder()
	{
		var root = CreateTempDirectory();

		try
		{
			var shallowSolution = CreateFile(root, "a", "Lib", "Lib.slnx");
			var missingSolution = Path.Combine(root, "a", "b", "c", "d", "App.slnx");
			string[] solutionFiles = [shallowSolution];

			var solution = DevServerMonitor.DetermineSolutionToLaunch(solutionFiles, CreateResolution(root, missingSolution, solutionFiles));

			solution.Should().Be(shallowSolution);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("When the rescan finds nothing but the resolved solution exists, the monitor reports it instead of SolutionNotFound (unoplatform/uno#25524)")]
	public async Task WhenRescanIsEmptyButSelectedSolutionExists_SolutionIsNotReportedMissing()
	{
		var root = CreateTempDirectory();
		var (monitor, warnings) = CreateMonitorWithoutHost();

		try
		{
			var selectedSolution = CreateFile(root, "a", "b", "c", "d", "App.slnx");

			await StartAndWaitForScanAsync(monitor, root, CreateResolution(root, selectedSolution, [selectedSolution]));

			monitor.SolutionNotFound.Should().BeFalse();
			monitor.DiscoveredSolutions.Should().Equal(Path.GetRelativePath(root, selectedSolution));
			warnings.Should().BeEmpty();
		}
		finally
		{
			await monitor.StopMonitoringAsync();
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("A shallow library solution and a deeper resolved app solution are both reported, without a fallback warning (unoplatform/uno#25524)")]
	public async Task WhenSelectedSolutionIsDeeperThanAnotherSolution_BothAreDiscoveredWithoutWarning()
	{
		var root = CreateTempDirectory();
		var (monitor, warnings) = CreateMonitorWithoutHost();

		try
		{
			var shallowSolution = CreateFile(root, "a", "Lib", "Lib.slnx");
			var selectedSolution = CreateFile(root, "a", "b", "c", "d", "App.slnx");

			await StartAndWaitForScanAsync(monitor, root, CreateResolution(root, selectedSolution, [shallowSolution]));

			monitor.SolutionNotFound.Should().BeFalse();
			monitor.DiscoveredSolutions.Should().Equal(
				Path.GetRelativePath(root, shallowSolution),
				Path.GetRelativePath(root, selectedSolution));
			warnings.Should().BeEmpty();
		}
		finally
		{
			await monitor.StopMonitoringAsync();
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("A resolved solution deleted from disk falls back to a scanned one, and the switch is logged as a warning")]
	public async Task WhenSelectedSolutionWasDeleted_FallbackIsLoggedAsWarning()
	{
		var root = CreateTempDirectory();
		var (monitor, warnings) = CreateMonitorWithoutHost();

		try
		{
			CreateFile(root, "a", "Lib", "Lib.slnx");
			var deletedSolution = Path.Combine(root, "a", "b", "c", "d", "App.slnx");

			await StartAndWaitForScanAsync(monitor, root, CreateResolution(root, deletedSolution, []));

			warnings.Should().ContainSingle().Which.Should().Contain(deletedSolution);
		}
		finally
		{
			await monitor.StopMonitoringAsync();
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("Without a resolved solution, launching the first of several scanned solutions is logged as a warning")]
	public async Task WhenSeveralSolutionsAreScannedWithoutSelection_ChoiceIsLoggedAsWarning()
	{
		var root = CreateTempDirectory();
		var (monitor, warnings) = CreateMonitorWithoutHost();

		try
		{
			CreateFile(root, "srcA", "AppA.slnx");
			CreateFile(root, "srcB", "AppB.slnx");

			monitor.StartMonitoring(root, port: 0, forwardedArgs: [], workspaceResolution: null);
			await WaitForScanAsync(monitor);

			warnings.Should().ContainSingle().Which.Should().Contain("No solution was selected");
		}
		finally
		{
			await monitor.StopMonitoringAsync();
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("DevServerMonitor can be stopped and started again so workspace restart transitions remain possible")]
	public async Task WhenStopped_CanStartMonitoringAgain()
	{
		var services = new ServiceCollection().BuildServiceProvider();
		var monitor = new DevServerMonitor(services, NullLogger<DevServerMonitor>.Instance);
		var firstDirectory = CreateTempDirectory();
		var secondDirectory = CreateTempDirectory();

		try
		{
			monitor.StartMonitoring(firstDirectory, port: 0, forwardedArgs: []);
			await monitor.StopMonitoringAsync();

			Func<Task> act = async () =>
			{
				monitor.StartMonitoring(secondDirectory, port: 0, forwardedArgs: []);
				await monitor.StopMonitoringAsync();
			};

			await act.Should().NotThrowAsync();
		}
		finally
		{
			Directory.Delete(firstDirectory, recursive: true);
			Directory.Delete(secondDirectory, recursive: true);
		}
	}

	[TestMethod]
	[Description("Stopping the monitor disposes the internal cancellation token source so repeated stop/start cycles do not leak resources")]
	public async Task WhenStopped_CancellationTokenSourceIsDisposed()
	{
		var services = new ServiceCollection().BuildServiceProvider();
		var monitor = new DevServerMonitor(services, NullLogger<DevServerMonitor>.Instance);
		var directory = CreateTempDirectory();

		try
		{
			monitor.StartMonitoring(directory, port: 0, forwardedArgs: []);
			var ctsField = typeof(DevServerMonitor).GetField("_cts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			ctsField.Should().NotBeNull();
			var cts = ctsField!.GetValue(monitor) as CancellationTokenSource;
			cts.Should().NotBeNull();

			await monitor.StopMonitoringAsync();

			ctsField.GetValue(monitor).Should().BeNull();
			var accessWaitHandle = () => _ = cts!.Token.WaitHandle;
			accessWaitHandle.Should().Throw<ObjectDisposedException>();
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-monitor-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private static string CreateFile(string root, params string[] segments)
	{
		var path = Path.Combine([root, .. segments]);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, string.Empty);
		return path;
	}

	private static WorkspaceResolution CreateResolution(string root, string selectedSolution, IReadOnlyList<string> candidates)
		=> new()
		{
			RequestedWorkingDirectory = Path.GetDirectoryName(selectedSolution)!,
			EffectiveWorkspaceDirectory = root,
			SelectedSolutionPath = selectedSolution,
			SelectedGlobalJsonPath = Path.Combine(root, "global.json"),
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = candidates,
		};

	// A real locator over a temp folder without global.json resolves no host, so the monitor runs its scan and
	// solution choice, then stops short of launching anything. This assumes no ancestor of the temp folder holds an
	// Uno global.json (the locator walks up), as on developer machines and CI agents.
	private static (DevServerMonitor Monitor, ConcurrentQueue<string> Warnings) CreateMonitorWithoutHost()
	{
		var services = new ServiceCollection()
			.AddSingleton(new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance))
			.BuildServiceProvider();
		var warnings = new ConcurrentQueue<string>();
		return (new DevServerMonitor(services, new WarningCollector(warnings)), warnings);
	}

	private static async Task StartAndWaitForScanAsync(DevServerMonitor monitor, string root, WorkspaceResolution resolution)
	{
		monitor.StartMonitoring(root, port: 0, forwardedArgs: [], resolution);
		await WaitForScanAsync(monitor);
	}

	private static async Task WaitForScanAsync(DevServerMonitor monitor)
	{
		// The first cycle publishes the scan before resolving the host; a regression leaves the list empty.
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (monitor.DiscoveredSolutions.Count == 0 && DateTime.UtcNow < deadline)
		{
			await Task.Delay(50);
		}

		monitor.DiscoveredSolutions.Should().NotBeEmpty("the monitor's first cycle reports the solution it will launch");
	}

	private sealed class WarningCollector(ConcurrentQueue<string> warnings) : ILogger<DevServerMonitor>
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (logLevel == LogLevel.Warning)
			{
				warnings.Enqueue(formatter(state, exception));
			}
		}
	}

	private static string CreatePath(params string[] segments)
	{
		var path = Path.GetTempPath();
		foreach (var segment in segments)
		{
			path = Path.Combine(path, segment);
		}

		return path;
	}
}
