using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_ProxyLifecycleManager
{
	[TestMethod]
	[Description("tools/list must not block on upstream startup unless wait-for-tools was explicitly requested")]
	public void WhenWaitForToolsIsDisabled_UpstreamWaitIsSkipped()
	{
		ProxyLifecycleManager.ShouldWaitForUpstreamTools(
			forceRootsFallback: false,
			clientSupportsRoots: false,
			waitForTools: false)
			.Should().BeFalse();

		ProxyLifecycleManager.ShouldWaitForUpstreamTools(
			forceRootsFallback: false,
			clientSupportsRoots: true,
			waitForTools: false)
			.Should().BeFalse();
	}

	[TestMethod]
	[Description("Selecting after roots resolution reuses the fresh candidate snapshot and avoids an extra solution scan")]
	public async Task WhenSelectingSolutionAfterRootsResolution_FreshCandidatesAreReused()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var solutionB = Path.Combine(workspaceB, "AppB.slnx");
			var finder = new CountingSolutionFileFinder(
			[
				Path.Combine(workspaceA, "AppA.slnx"),
				solutionB,
			]);

			var created = CreateSubject(solutionFileFinder: finder);
			subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			});

			await InvokeSetRootsAsync(subject, [$"file:///{root.Replace('\\', '/')}"]);
			finder.CallCount.Should().Be(1);

			await subject.SelectSolutionAsync(solutionB);
			finder.CallCount.Should().Be(1);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Any observed workspace mutation invalidates the roots candidate snapshot before the next selection")]
	public async Task WhenWorkspaceMutatedAfterRootsResolution_CandidatesAreRescanned()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var solutionB = Path.Combine(workspaceB, "AppB.slnx");
			var finder = new CountingSolutionFileFinder(
			[
				Path.Combine(workspaceA, "AppA.slnx"),
				solutionB,
			]);

			var created = CreateSubject(solutionFileFinder: finder);
			subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			});

			await InvokeSetRootsAsync(subject, [$"file:///{root.Replace('\\', '/')}"]);
			finder.CallCount.Should().Be(1);

			SetPrivateField(subject, "_workspaceMutationGeneration", 1);

			await subject.SelectSolutionAsync(solutionB);
			finder.CallCount.Should().Be(2);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Selecting a valid Uno candidate starts the deferred DevServer and marks the workspace as explicitly selected")]
	public async Task WhenSelectingValidUnoSolutionAfterDeferredStartup_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspaceDirectory, "MyApp.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			var result = await subject.SelectSolutionAsync(solutionPath);

			result.Status.Should().Be("started");
			result.DevServerAction.Should().Be("Start");
			result.SelectedSolutionPath.Should().Be(solutionPath);
			result.EffectiveWorkspaceDirectory.Should().Be(workspaceDirectory);
			result.Issues.Should().BeNull();
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Selecting the already-running solution returns already_selected without restarting the session")]
	public async Task WhenSelectingCurrentRunningSolution_NoRestartOccurs()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspaceDirectory, "MyApp.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution with { SelectionSource = WorkspaceSelectionSource.UserSelected });
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			monitor.StartMonitoring(workspaceDirectory, port: 0, forwardedArgs: [], workspaceResolution);
			healthService.DevServerStarted = true;

			var initialMonitorTask = GetPrivateField<Task?>(monitor, "_monitor");
			var result = await subject.SelectSolutionAsync(solutionPath);

			result.Status.Should().Be("already_selected");
			result.DevServerAction.Should().Be("None");
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<Task?>(monitor, "_monitor").Should().BeSameAs(initialMonitorTask);
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Selecting a different valid Uno solution restarts the DevServer on the newly selected workspace")]
	public async Task WhenSelectingDifferentUnoSolution_DevServerRestartsOnSelectedWorkspace()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var solutionB = Path.Combine(workspaceB, "AppB.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolutionA = await resolver.ResolveAsync(workspaceA);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", resolutionA);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			monitor.StartMonitoring(workspaceA, port: 0, forwardedArgs: [], resolutionA);
			healthService.DevServerStarted = true;

			var result = await subject.SelectSolutionAsync(solutionB);

			result.Status.Should().Be("restarted");
			result.DevServerAction.Should().Be("Restart");
			result.SelectedSolutionPath.Should().Be(solutionB);
			GetPrivateField<string>(monitor, "_currentDirectory").Should().Be(workspaceB);
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
			healthService.DevServerStarted.Should().BeTrue();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Selecting a non-Uno solution is rejected with a diagnostic result and does not start the DevServer")]
	public async Task WhenSelectingNonUnoSolution_RequestIsRejected()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateNonUnoWorkspaceAsync(root, "src", "MyApp.slnx");
			var solutionPath = Path.Combine(workspaceDirectory, "MyApp.slnx");
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			var subject = created.Subject;
			var healthService = created.HealthService;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);

			var result = await subject.SelectSolutionAsync(solutionPath);

			result.Status.Should().Be("rejected");
			result.DevServerAction.Should().Be("None");
			result.Message.Should().Contain("cannot start");
			var issue = result.Issues.Should().ContainSingle().Subject;
			issue.Code.Should().Be(IssueCode.WorkspaceNotResolved);
			issue.Message.Should().Contain(Path.Combine(workspaceDirectory, "global.json"), "the issue names the global.json that applies");
			issue.Remediation.Should().Contain("uno_app_select_solution", "fixing global.json and selecting again is enough, no bridge restart");
			healthService.DevServerStarted.Should().BeFalse();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A solution outside the session's candidate set is accepted and started: discovery suggests solutions, it does not restrict the agent's choice (#25524)")]
	public async Task WhenSelectingSolutionOutsideCandidates_SelectionIsAccepted()
	{
		var root = CreateTempDirectory();
		var externalRoot = CreateTempDirectory();
		DevServerMonitor? monitor = null;

		try
		{
			await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var externalWorkspace = await CreateUnoWorkspaceAsync(externalRoot, "other", "Other.slnx", "6.6.0-dev.2");
			var externalSolution = Path.Combine(externalWorkspace, "Other.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			var subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			var result = await subject.SelectSolutionAsync(externalSolution);

			result.Status.Should().Be("started");
			result.SelectedSolutionPath.Should().Be(externalSolution);
			result.EffectiveWorkspaceDirectory.Should().Be(externalWorkspace);
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
			healthService.DevServerStarted.Should().BeTrue();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
			await DeleteDirectoryWithRetriesAsync(externalRoot);
		}
	}

	[TestMethod]
	[Description("A solution deeper than the candidate scan is accepted, with the workspace of the nearest Uno global.json above it (#25524)")]
	public async Task WhenSelectingSolutionDeeperThanTheScan_SelectionIsAccepted()
	{
		var root = CreateTempDirectory();
		DevServerMonitor? monitor = null;

		try
		{
			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			var libraryDirectory = Directory.CreateDirectory(Path.Combine(root, "a", "Lib")).FullName;
			await File.WriteAllTextAsync(Path.Combine(libraryDirectory, "Lib.slnx"), string.Empty);
			var appDirectory = Directory.CreateDirectory(Path.Combine(root, "a", "b", "c", "d")).FullName;
			var appSolution = Path.Combine(appDirectory, "App.slnx");
			await File.WriteAllTextAsync(appSolution, string.Empty);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var created = CreateSubject();
			var subject = created.Subject;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", await resolver.ResolveAsync(root));
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			var result = await subject.SelectSolutionAsync(appSolution);

			result.Status.Should().Be("started");
			result.SelectedSolutionPath.Should().Be(appSolution);
			result.EffectiveWorkspaceDirectory.Should().Be(root);
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DataRow(@"\\server\share\repo\App.slnx")]
	[DataRow(@"\??\UNC\server\share\repo\App.slnx")]
	[Description("A network (UNC) solution path is refused before any file-system access, which would authenticate the user to that server")]
	public async Task WhenSelectingUncSolutionPath_RequestIsRejected(string solutionPath)
	{
		var created = CreateSubject();

		var result = await created.Subject.SelectSolutionAsync(solutionPath);

		result.Status.Should().Be("rejected");
		result.Message.Should().Contain("local path");
		created.HealthService.DevServerStarted.Should().BeFalse();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DataRow("file://server.invalid/share/repo")]
	[DataRow(@"\??\UNC\server.invalid\share\repo")]
	[Description("An MCP root on a network share is ignored before any file-system access, as a selected solution would be")]
	public async Task WhenRootsPointToNetworkShare_RootIsIgnored(string rootUri)
	{
		var loggerProvider = new CollectingLoggerProvider();
		var finder = new CountingSolutionFileFinder([]);
		var created = CreateSubject(logger: loggerProvider.CreateLogger<ProxyLifecycleManager>(), solutionFileFinder: finder);

		await InvokeSetRootsAsync(created.Subject, [rootUri]);

		GetPrivateField<WorkspaceResolution?>(created.Subject, "_workspaceResolution").Should().BeNull();
		finder.CallCount.Should().Be(0);
		loggerProvider.Messages.Should().Contain(message => message.StartsWith("Ignoring MCP root", StringComparison.Ordinal));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("A refused or unresolvable MCP root (a share, a NUL character) falls through to the next one, so a multi-root workspace still resolves its local root")]
	public async Task WhenFirstRootsAreRefused_NextRootIsUsed()
	{
		var root = CreateTempDirectory();

		try
		{
			var localRoot = Path.Combine(root, "repo");
			Directory.CreateDirectory(localRoot);
			var created = CreateSubject();

			await InvokeSetRootsAsync(created.Subject, ["file://server.invalid/share/repo", "repo\0x", new Uri(localRoot).AbsoluteUri]);

			var resolution = GetPrivateField<WorkspaceResolution>(created.Subject, "_workspaceResolution");
			PathComparison.PathsEqual(resolution.EffectiveWorkspaceDirectory, localRoot).Should().BeTrue();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A path to an existing file that is not a .sln or .slnx is rejected, naming the expected extensions")]
	public async Task WhenSelectingFileThatIsNotASolution_RequestIsRejected()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var projectPath = Path.Combine(workspaceDirectory, "MyApp.csproj");
			await File.WriteAllTextAsync(projectPath, "<Project />");
			var created = CreateSubject();
			SetPrivateField(created.Subject, "_currentDirectory", root);

			var result = await created.Subject.SelectSolutionAsync(projectPath);

			result.Status.Should().Be("rejected");
			result.Message.Should().Contain(".slnx");
			created.HealthService.DevServerStarted.Should().BeFalse();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A file change after an explicit selection re-resolves the selected solution instead of auto-discovering another one (#25524)")]
	public async Task WhenWorkspaceChangesAfterExplicitSelection_SelectionIsKept()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var (librarySolution, appSolution) = await CreateDeepAppLayoutAsync(root);
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(appSolution)).Status.Should().Be("started");

			await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(librarySolution)!, "Lib2.slnx"), string.Empty);
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
			created.HealthService.CurrentSelection.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
			created.HealthService.DevServerStarted.Should().BeTrue();
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A deleted selected solution degrades the session but stays selected, so the DevServer starts again on it once the file is back (#25524)")]
	public async Task WhenExplicitlySelectedSolutionIsDeletedAndRestored_SessionDegradesThenRestarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var (_, appSolution) = await CreateDeepAppLayoutAsync(root);
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(appSolution)).Status.Should().Be("started");

			File.Delete(appSolution);
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			subject.ConnectionState.Should().Be(ConnectionState.Degraded);
			created.HealthService.DevServerStarted.Should().BeFalse();
			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
			GetWorkspaceObservationRoot(subject).Should().Be(root, "the workspace above the missing solution stays watched, so restoring it is seen");

			await File.WriteAllTextAsync(appSolution, string.Empty);
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			created.HealthService.DevServerStarted.Should().BeTrue();
			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("An Uno SDK version change in a selected solution's global.json restarts the DevServer on the same solution with the new SDK (#25524)")]
	public async Task WhenSelectedSolutionSdkVersionChanges_SameSolutionRestartsWithTheNewSdk()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var (_, appSolution) = await CreateDeepAppLayoutAsync(root);
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(appSolution)).Status.Should().Be("started");

			await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.9"}}""");
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			created.HealthService.DevServerStarted.Should().BeTrue();
			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
			GetPrivateField<WorkspaceResolution>(created.Monitor, "_workspaceResolution").UnoSdkVersion
				.Should().Be("6.6.0-dev.9", "the monitor was restarted with the re-resolved selection");
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A selected solution whose global.json loses the Uno SDK degrades the session, which watches that global.json and restarts once the SDK is back (#25524)")]
	public async Task WhenSelectedSolutionGlobalJsonLosesUnoSdk_SessionDegradesThenRestartsWhenFixed()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var (_, appSolution) = await CreateDeepAppLayoutAsync(root);
			var globalJsonPath = Path.Combine(root, "global.json");
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(appSolution)).Status.Should().Be("started");

			await File.WriteAllTextAsync(globalJsonPath, """{"sdk":{"version":"10.0.100"}}""");
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			subject.ConnectionState.Should().Be(ConnectionState.Degraded);
			GetWorkspaceObservationRoot(subject).Should().Be(root, "the global.json that lost the SDK is watched");

			await File.WriteAllTextAsync(globalJsonPath, """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			created.HealthService.DevServerStarted.Should().BeTrue();
			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A solution selected outside the startup directory is watched in its own workspace, while the suggestions stay those of the startup directory (#25524)")]
	public async Task WhenSolutionOutsideStartDirectoryIsSelected_ItsWorkspaceIsWatchedAndSuggestionsStay()
	{
		var root = CreateTempDirectory();
		var externalRoot = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var localWorkspace = await CreateUnoWorkspaceAsync(root, "src", "Local.slnx", "6.6.0-dev.1");
			var externalWorkspace = await CreateUnoWorkspaceAsync(externalRoot, "other", "Other.slnx", "6.6.0-dev.2");
			var externalSolution = Path.Combine(externalWorkspace, "Other.slnx");
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(externalSolution)).Status.Should().Be("started");

			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			GetWorkspaceObservationRoot(subject).Should().Be(externalWorkspace);
			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(externalSolution);
			created.HealthService.CurrentSelection.CandidateSolutions.Should().Contain(Path.Combine(localWorkspace, "Local.slnx"));
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
			await DeleteDirectoryWithRetriesAsync(externalRoot);
		}
	}

	[TestMethod]
	[Description("A workspace chosen as a directory (--solution-dir) does not pin the solution discovery picked: file changes still re-run discovery")]
	public async Task WhenWorkspaceWasChosenAsDirectory_FileChangesStillRediscoverTheSolution()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.sln", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var directoryResolution = await resolver.ResolveExplicitWorkspaceAsync(workspace);
			directoryResolution.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
			var created = CreateSubject();
			subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", workspace);
			SetPrivateField(subject, "_workspaceResolution", directoryResolution);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			var migratedSolution = Path.Combine(workspace, "App.slnx");
			await File.WriteAllTextAsync(migratedSolution, string.Empty);
			File.Delete(Path.Combine(workspace, "App.sln"));
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(migratedSolution);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("With roots fallback, an explicit solution (--solution) starts the DevServer at once: roots could not change it, so waiting for them would hang uno_app_initialize (#25524)")]
	public async Task WhenRootsFallbackWithExplicitSolution_DevServerStartsWithoutWaitingForRoots()
	{
		var root = CreateTempDirectory();
		DevServerMonitor? monitor = null;

		try
		{
			var (_, appSolution) = await CreateDeepAppLayoutAsync(root);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var (subject, healthService, createdMonitor) = CreateSubject();
			monitor = createdMonitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", await resolver.ResolveSolutionAsync(root, appSolution));
			SetPrivateField(subject, "_forceRootsFallback", true);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			typeof(ProxyLifecycleManager)
				.GetMethod("EnsureDevServerStartedFromSolutionDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(subject, null);

			healthService.DevServerStarted.Should().BeTrue();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	private static string? GetWorkspaceObservationRoot(ProxyLifecycleManager subject)
		=> (string?)typeof(ProxyLifecycleManager)
			.GetMethod("GetWorkspaceObservationRoot", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(subject, null);

	// root/global.json (Uno), a library solution within the solution scan depth, and the app solution below it.
	private static async Task<(string LibrarySolution, string AppSolution)> CreateDeepAppLayoutAsync(string root)
	{
		await File.WriteAllTextAsync(Path.Combine(root, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
		var libraryDirectory = Directory.CreateDirectory(Path.Combine(root, "a", "Lib")).FullName;
		var librarySolution = Path.Combine(libraryDirectory, "Lib.slnx");
		await File.WriteAllTextAsync(librarySolution, string.Empty);
		var appDirectory = Directory.CreateDirectory(Path.Combine(root, "a", "b", "c", "d")).FullName;
		var appSolution = Path.Combine(appDirectory, "App.slnx");
		await File.WriteAllTextAsync(appSolution, string.Empty);
		return (librarySolution, appSolution);
	}

	private static async Task StartSessionAsync(ProxyLifecycleManager subject, string root)
	{
		var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
		SetPrivateField(subject, "_currentDirectory", root);
		SetPrivateField(subject, "_workspaceResolution", await resolver.ResolveAsync(root));
		SetPrivateField(subject, "_devServerPort", 0);
		SetPrivateField(subject, "_forwardedArgs", new List<string>());
	}

	[TestMethod]
	[Description("A solution path that does not exist is rejected as not found")]
	public async Task WhenSelectingMissingSolution_RequestIsRejected()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var missingSolution = Path.Combine(workspaceDirectory, "Missing.slnx");
			var created = CreateSubject();
			SetPrivateField(created.Subject, "_currentDirectory", root);

			var result = await created.Subject.SelectSolutionAsync(missingSolution);

			result.Status.Should().Be("rejected");
			result.Message.Should().ContainEquivalentOf("not found");
			created.HealthService.DevServerStarted.Should().BeFalse();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Explicit solution selection resolves an ambiguous workspace and starts the selected Uno solution")]
	public async Task WhenSelectingSolutionFromAmbiguousWorkspace_SelectedWorkspaceStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.1");
			var solutionB = Path.Combine(workspaceB, "AppB.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var ambiguous = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", ambiguous);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			var result = await subject.SelectSolutionAsync(solutionB);

			result.Status.Should().Be("started");
			result.SelectedSolutionPath.Should().Be(solutionB);
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspaceB);
			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Deferred MCP roots that confirm the selected workspace start the DevServer when roots fallback postponed startup")]
	public async Task WhenRootsConfirmCurrentWorkspaceAfterDeferredStartup_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceDirectory = Path.Combine(root, "src");
			Directory.CreateDirectory(workspaceDirectory);
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "MyApp.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await InvokeSetRootsAsync(subject, [new Uri(workspaceDirectory).AbsoluteUri]);

			subject.ConnectionState.Should().Be(ConnectionState.Discovering);
			healthService.ConnectionState.Should().Be(ConnectionState.Discovering);
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Late MCP roots that confirm an already running workspace do not restart or degrade the current session")]
	public async Task WhenRootsConfirmCurrentWorkspaceDuringActiveSession_SessionRemainsUnchanged()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceDirectory = Path.Combine(root, "src");
			Directory.CreateDirectory(workspaceDirectory);
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "MyApp.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			monitor.StartMonitoring(workspaceDirectory, port: 0, forwardedArgs: [], workspaceResolution);
			healthService.DevServerStarted = true;

			await InvokeSetRootsAsync(subject, [new Uri(workspaceDirectory).AbsoluteUri]);

			subject.ConnectionState.Should().Be(ConnectionState.Initializing);
			healthService.ConnectionState.Should().Be(ConnectionState.Initializing);
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<string>(monitor, "_currentDirectory").Should().Be(workspaceDirectory);
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Late MCP roots that point to a different Uno workspace are a hint only: the running workspace is kept and the session is not degraded (#25524)")]
	public async Task WhenRootsPointToDifferentWorkspaceWhileRunning_RunningWorkspaceIsKept()
	{
		var root = CreateTempDirectory();
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceA = Path.Combine(root, "srcA");
			var workspaceB = Path.Combine(root, "srcB");
			Directory.CreateDirectory(workspaceA);
			Directory.CreateDirectory(workspaceB);

			await File.WriteAllTextAsync(Path.Combine(workspaceA, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspaceA, "AppA.slnx"), string.Empty);
			await File.WriteAllTextAsync(Path.Combine(workspaceB, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.2"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspaceB, "AppB.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(workspaceA);

			var (subject, healthService, createdMonitor) = CreateSubject();
			monitor = createdMonitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);
			monitor.StartMonitoring(workspaceA, port: 0, forwardedArgs: [], workspaceResolution);
			healthService.DevServerStarted = true;

			await InvokeSetRootsAsync(subject, [new Uri(workspaceB).AbsoluteUri]);

			subject.ConnectionState.Should().NotBe(ConnectionState.Degraded);
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<string>(monitor, "_currentDirectory").Should().Be(workspaceA);
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspaceA);
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Before any DevServer started (deferred roots startup), MCP roots naming another Uno workspace start it there instead of waiting on the unused one")]
	public async Task WhenRootsPointToDifferentWorkspaceBeforeStartup_RootsWorkspaceStarts()
	{
		var root = CreateTempDirectory();
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

			var (subject, healthService, createdMonitor) = CreateSubject();
			monitor = createdMonitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", await resolver.ResolveAsync(workspaceA));
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await InvokeSetRootsAsync(subject, [new Uri(workspaceB).AbsoluteUri]);

			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<string>(monitor, "_currentDirectory").Should().Be(workspaceB);
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("MCP roots do not take over a selected solution that currently does not resolve: it stays selected (#25524)")]
	public async Task WhenRootsArriveWhileSelectedSolutionDoesNotResolve_SelectionIsKept()
	{
		var root = CreateTempDirectory();
		var otherRoot = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var (_, appSolution) = await CreateDeepAppLayoutAsync(root);
			var otherWorkspace = await CreateUnoWorkspaceAsync(otherRoot, "src", "Other.slnx", "6.6.0-dev.2");
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(appSolution)).Status.Should().Be("started");
			File.Delete(appSolution);
			await subject.ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);

			await InvokeSetRootsAsync(subject, [new Uri(otherWorkspace).AbsoluteUri]);

			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
			created.HealthService.DevServerStarted.Should().BeFalse("roots must not start the other workspace in place of the selection");
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
			await DeleteDirectoryWithRetriesAsync(otherRoot);
		}
	}

	[TestMethod]
	[Description("MCP roots never replace a solution the agent selected explicitly, whatever workspace they point to (#25524)")]
	public async Task WhenRootsArriveAfterExplicitSelection_SelectionIsKept()
	{
		var root = CreateTempDirectory();
		var otherRoot = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var (_, appSolution) = await CreateDeepAppLayoutAsync(root);
			var otherWorkspace = await CreateUnoWorkspaceAsync(otherRoot, "src", "Other.slnx", "6.6.0-dev.2");
			var created = CreateSubject();
			subject = created.Subject;
			await StartSessionAsync(subject, root);
			(await subject.SelectSolutionAsync(appSolution)).Status.Should().Be("started");

			await InvokeSetRootsAsync(subject, [new Uri(otherWorkspace).AbsoluteUri]);

			created.HealthService.CurrentSelection!.SelectedSolutionPath.Should().Be(appSolution);
			created.HealthService.DevServerStarted.Should().BeTrue();
			subject.ConnectionState.Should().NotBe(ConnectionState.Degraded);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
			await DeleteDirectoryWithRetriesAsync(otherRoot);
		}
	}

	[TestMethod]
	[Description("Late MCP roots that still do not resolve to a valid Uno workspace keep the session in immediate diagnostic mode")]
	public async Task WhenRootsResolveToNonUnoDirectory_WorkspaceIsStillAccepted()
	{
		var root = CreateTempDirectory();

		try
		{
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var nonUno = Path.Combine(root, "src");
			Directory.CreateDirectory(nonUno);
			await File.WriteAllTextAsync(Path.Combine(nonUno, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(nonUno, "App.slnx"), string.Empty);

			var (subject, healthService, _) = CreateSubject();
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);

			await InvokeSetRootsAsync(subject, [new Uri(nonUno).AbsoluteUri]);

			// The workspace directory should be accepted even without an Uno solution
			var currentResolution = GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution");
			currentResolution.EffectiveWorkspaceDirectory.Should().NotBeNullOrWhiteSpace();
			PathComparison.PathsEqual(currentResolution.EffectiveWorkspaceDirectory, nonUno).Should().BeTrue();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("set_roots on an empty directory with force-roots-fallback accepts the workspace and sets effectiveWorkspaceDirectory")]
	public async Task WhenSetRootsOnEmptyDirectoryWithForceRootsFallback_WorkspaceIsAccepted()
	{
		var root = CreateTempDirectory();

		try
		{
			// Empty directory — no .sln, no global.json
			var emptyWorkspace = Path.Combine(root, "my-project");
			Directory.CreateDirectory(emptyWorkspace);

			var (subject, healthService, _) = CreateSubject();
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_forceRootsFallback", true);
			SetPrivateField(subject, "_workspaceResolution", new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			});

			await InvokeSetRootsAsync(subject, [new Uri(emptyWorkspace).AbsoluteUri]);

			// The workspace should be accepted even without a solution
			var resolution = GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution");
			resolution.EffectiveWorkspaceDirectory.Should().NotBeNullOrWhiteSpace(
				"set_roots with force-roots-fallback should accept an empty directory as workspace");
			PathComparison.PathsEqual(resolution.EffectiveWorkspaceDirectory, emptyWorkspace).Should().BeTrue();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("set_roots on a directory with a non-Uno solution with force-roots-fallback still accepts the workspace")]
	public async Task WhenSetRootsOnNonUnoDirectoryWithForceRootsFallback_WorkspaceIsAccepted()
	{
		var root = CreateTempDirectory();

		try
		{
			// Directory with a .sln but no Uno SDK in global.json
			var workspace = Path.Combine(root, "src");
			Directory.CreateDirectory(workspace);
			await File.WriteAllTextAsync(Path.Combine(workspace, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspace, "App.slnx"), string.Empty);

			var (subject, healthService, _) = CreateSubject();
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_forceRootsFallback", true);
			SetPrivateField(subject, "_workspaceResolution", new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			});

			await InvokeSetRootsAsync(subject, [new Uri(workspace).AbsoluteUri]);

			var resolution = GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution");
			resolution.EffectiveWorkspaceDirectory.Should().NotBeNullOrWhiteSpace(
				"set_roots with force-roots-fallback should accept a non-Uno directory as workspace");
			PathComparison.PathsEqual(resolution.EffectiveWorkspaceDirectory, workspace).Should().BeTrue();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("File system transitions from no workspace to a valid Uno workspace start the DevServer monitor immediately")]
	public async Task WhenFileSystemFindsUnoWorkspace_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspace = await resolver.ResolveAsync(workspace);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);

			subject.ConnectionState.Should().Be(ConnectionState.Discovering);
			healthService.ConnectionState.Should().Be(ConnectionState.Discovering);
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspace);
			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("File system transitions that remove the current Uno workspace stop the current DevServer session and degrade health immediately")]
	public async Task WhenFileSystemLosesUnoWorkspace_DevServerStops()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspace = await resolver.ResolveAsync(workspace);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = workspace,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", resolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			monitor.StartMonitoring(workspace, port: 0, forwardedArgs: [], resolvedWorkspace);
			healthService.DevServerStarted = true;

			await subject.ApplyWorkspaceResolutionAsync(unresolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);

			subject.ConnectionState.Should().Be(ConnectionState.Degraded);
			healthService.ConnectionState.Should().Be(ConnectionState.Degraded);
			healthService.DevServerStarted.Should().BeFalse();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").ResolutionKind.Should().Be(WorkspaceResolutionKind.NoCandidates);
			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("File system transitions that move from workspace A to workspace B restart the DevServer monitor on the newly selected workspace")]
	public async Task WhenFileSystemSwitchesWorkspaces_DevServerRestartsOnNewWorkspace()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspaceA = await resolver.ResolveAsync(workspaceA);
			var resolvedWorkspaceB = await resolver.ResolveAsync(workspaceB);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", resolvedWorkspaceA);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			monitor.StartMonitoring(workspaceA, port: 0, forwardedArgs: [], resolvedWorkspaceA);
			healthService.DevServerStarted = true;

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspaceB, WorkspaceTransitionTrigger.FileSystem);

			subject.ConnectionState.Should().Be(ConnectionState.Discovering);
			healthService.ConnectionState.Should().Be(ConnectionState.Discovering);
			healthService.DevServerStarted.Should().BeTrue();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspaceB);
			GetPrivateField<string>(monitor, "_currentDirectory").Should().Be(workspaceB);
			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
		}
		finally
		{
			if (monitor is not null)
			{
				await monitor.StopMonitoringAsync();
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Only solution and global.json mutations trigger workspace reevaluation")]
	public void WhenWorkspaceMutationPathIsRelevant_PathIsDetected()
	{
		ProxyLifecycleManager.IsWorkspaceMutationPath(Path.Combine(Path.GetTempPath(), "repo", "global.json")).Should().BeTrue();
		ProxyLifecycleManager.IsWorkspaceMutationPath(Path.Combine(Path.GetTempPath(), "repo", "App.sln")).Should().BeTrue();
		ProxyLifecycleManager.IsWorkspaceMutationPath(Path.Combine(Path.GetTempPath(), "repo", "App.slnx")).Should().BeTrue();
		ProxyLifecycleManager.IsWorkspaceMutationPath(Path.Combine(Path.GetTempPath(), "repo", "README.md")).Should().BeFalse();
		ProxyLifecycleManager.IsWorkspaceMutationPath(null).Should().BeFalse();
	}

	[TestMethod]
	[Description("A live workspace watcher starts the DevServer automatically when an empty directory becomes a valid Uno workspace")]
	public async Task WhenWatcherSeesUnoWorkspaceAppear_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			subject.StartWorkspaceMutationWatcher();
			await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");

			await WaitUntilAsync(() => healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Discovering);

			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").ResolutionKind.Should().NotBe(WorkspaceResolutionKind.NoCandidates);
		}
		finally
		{
			if (subject is not null)
			{
				try
				{
					await subject.StopWorkspaceMutationWatcherAsync();
				}
				catch
				{
					// Ignore cleanup errors for test teardown.
				}
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher stops the DevServer immediately when the active Uno workspace loses global.json")]
	public async Task WhenWatcherSeesWorkspaceBecomeInvalid_DevServerStops()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspace = await resolver.ResolveAsync(workspace);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);
			subject.StartWorkspaceMutationWatcher();
			await WaitUntilAsync(() =>
				PathComparison.PathsEqual(
					GetPrivateField<string?>(subject, "_workspaceMutationWatcherRoot"),
					root));

			File.Delete(Path.Combine(workspace, "global.json"));

			await WaitUntilAsync(() => !healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Degraded);

			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").IsResolved.Should().BeFalse();
		}
		finally
		{
			if (subject is not null)
			{
				try
				{
					await subject.StopWorkspaceMutationWatcherAsync();
				}
				catch
				{
					// Ignore cleanup errors for test teardown.
				}
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher starts the DevServer when a non-Uno workspace becomes a valid Uno workspace")]
	public async Task WhenWatcherSeesGlobalJsonBecomeUno_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspace = await CreateNonUnoWorkspaceAsync(root, "src", "App.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var unresolvedWorkspace = await resolver.ResolveAsync(workspace);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			subject.StartWorkspaceMutationWatcher();
			await File.WriteAllTextAsync(
				Path.Combine(workspace, "global.json"),
				"""{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");

			await WaitUntilAsync(() => healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Discovering);

			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspace);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher stays diagnostic when a non-Uno solution appears but no Uno workspace can be resolved")]
	public async Task WhenWatcherSeesNonUnoSolutionAppear_HealthStaysDiagnostic()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			subject.StartWorkspaceMutationWatcher();
			await CreateNonUnoWorkspaceAsync(root, "src", "App.slnx");

			await WaitUntilAsync(() => subject.ConnectionState == ConnectionState.Degraded);

			healthService.DevServerStarted.Should().BeFalse();
			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher stops the DevServer when the active solution file disappears")]
	public async Task WhenWatcherSeesSolutionDeleted_DevServerStops()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspace, "App.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspace = await resolver.ResolveAsync(workspace);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);
			subject.StartWorkspaceMutationWatcher();

			File.Delete(solutionPath);

			await WaitUntilAsync(() => !healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Degraded);

			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").ResolutionKind.Should().Be(WorkspaceResolutionKind.NoCandidates);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher restarts the DevServer when repo mutations replace workspace A with workspace B")]
	public async Task WhenWatcherSeesWorkspaceSwitch_DevServerRestarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var workspaceBGlobalJson = Path.Combine(workspaceB, "global.json");
			File.Delete(workspaceBGlobalJson);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspaceA = await resolver.ResolveAsync(workspaceA);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspaceA, WorkspaceTransitionTrigger.FileSystem);
			subject.StartWorkspaceMutationWatcher();
			await WaitUntilAsync(() =>
				PathComparison.PathsEqual(
					GetPrivateField<string?>(subject, "_workspaceMutationWatcherRoot"),
					root));

			File.Delete(Path.Combine(workspaceA, "global.json"));
			await File.WriteAllTextAsync(workspaceBGlobalJson, """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.2"}}""");

			await WaitUntilAsync(() => healthService.DevServerStarted
				&& subject.ConnectionState == ConnectionState.Discovering
				&& GetPrivateField<string>(monitor, "_currentDirectory") == workspaceB);

			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspaceB);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher does not restart the DevServer for unrelated file changes when the effective workspace remains the same")]
	public async Task WhenWatcherSeesUnrelatedChange_DevServerDoesNotRestart()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspace = await resolver.ResolveAsync(workspace);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);
			subject.StartWorkspaceMutationWatcher();
			var runningMonitor = GetPrivateField<Task?>(monitor, "_monitor");

			await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "updated");
			await Task.Delay(750);

			healthService.DevServerStarted.Should().BeTrue();
			subject.ConnectionState.Should().Be(ConnectionState.Discovering);
			GetPrivateField<Task?>(monitor, "_monitor").Should().BeSameAs(runningMonitor);
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspace);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher stops the DevServer and moves to diagnostics when an equally valid second Uno workspace appears")]
	public async Task WhenWatcherSeesAmbiguousWorkspace_DevServerStopsAndDegrades()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = Path.Combine(root, "srcB");
			Directory.CreateDirectory(workspaceB);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspaceA = await resolver.ResolveAsync(workspaceA);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspaceA, WorkspaceTransitionTrigger.FileSystem);
			subject.StartWorkspaceMutationWatcher();

			await File.WriteAllTextAsync(Path.Combine(workspaceB, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.2"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspaceB, "AppB.slnx"), string.Empty);

			await WaitUntilAsync(() => !healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Degraded);

			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").ResolutionKind.Should().Be(WorkspaceResolutionKind.Ambiguous);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Selecting a malformed absolute solution path is rejected cleanly instead of throwing from path normalization")]
	public async Task WhenSelectingMalformedAbsoluteSolutionPath_RequestIsRejected()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			var subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			var malformedPath = OperatingSystem.IsWindows()
				? "C:\\\0\\bad.slnx"
				: "/\0/bad.slnx";
			var result = await subject.SelectSolutionAsync(malformedPath);

			result.Status.Should().Be("rejected");
			result.DevServerAction.Should().Be("None");
			result.Message.Should().ContainEquivalentOf("absolute path");
			result.Issues.Should().NotBeNull();
			result.Issues!.Should().Contain(issue => issue.Code == IssueCode.WorkspaceNotResolved);
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("An unresolved workspace with a populated solution directory does not start the DevServer monitor during initial MCP startup")]
	public void WhenEnsuringStartupWithUnresolvedWorkspaceAndSolutionDirectory_DevServerDoesNotStart()
	{
		var root = CreateTempDirectory();

		try
		{
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			var subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_solutionDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);

			var method = typeof(ProxyLifecycleManager).GetMethod("EnsureDevServerStartedFromSolutionDirectory", BindingFlags.Instance | BindingFlags.NonPublic);
			method.Should().NotBeNull();

			method!.Invoke(subject, []);

			healthService.DevServerStarted.Should().BeFalse();
			subject.ConnectionState.Should().Be(ConnectionState.Degraded);
			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
		}
		finally
		{
			DeleteDirectoryWithRetriesAsync(root).GetAwaiter().GetResult();
		}
	}

	[TestMethod]
	[Description("Explicit solution selection follows filesystem path semantics: a differently-cased path selects the solution only where the filesystem is case-insensitive")]
	public async Task WhenSelectingSolutionWithDifferentCase_UsesFilesystemPathSemantics()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "MyApp.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspaceDirectory, "MyApp.slnx");
			var alteredCaseSolutionPath = AlterPathCase(solutionPath);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			var subject = created.Subject;
			var healthService = created.HealthService;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			var result = await subject.SelectSolutionAsync(alteredCaseSolutionPath);

			// The selection stands or falls with the file system: case-insensitive volumes find the file, others do not.
			if (File.Exists(alteredCaseSolutionPath))
			{
				result.Status.Should().NotBe("rejected");
			}
			else
			{
				result.Status.Should().Be("rejected");
				healthService.DevServerStarted.Should().BeFalse();
			}
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Rename events re-evaluate the workspace when a relevant file is renamed away from the watched set")]
	public async Task WhenWorkspaceMutationRenamesRelevantFileAway_DevServerStopsAndDegrades()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspace = await CreateUnoWorkspaceAsync(root, "src", "App.slnx", "6.6.0-dev.1");
			var globalJsonPath = Path.Combine(workspace, "global.json");
			var renamedGlobalJsonPath = Path.Combine(workspace, "global.json.bak");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolvedWorkspace = await resolver.ResolveAsync(workspace);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = root,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);

			File.Move(globalJsonPath, renamedGlobalJsonPath);

			var method = typeof(ProxyLifecycleManager).GetMethod("OnWorkspaceMutation", BindingFlags.Instance | BindingFlags.NonPublic);
			method.Should().NotBeNull();
			method!.Invoke(subject, [subject, new RenamedEventArgs(WatcherChangeTypes.Renamed, workspace, "global.json.bak", "global.json")]);

			await WaitUntilAsync(() => !healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Degraded);

			GetPrivateField<Task?>(monitor, "_monitor").Should().BeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("A live workspace watcher starts the DevServer when an ambiguous repo becomes a single clear Uno workspace")]
	public async Task WhenWatcherSeesAmbiguousBecomeSingle_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var ambiguousResolution = await resolver.ResolveAsync(root);
			ambiguousResolution.ResolutionKind.Should().Be(WorkspaceResolutionKind.Ambiguous);

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", ambiguousResolution);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			subject.StartWorkspaceMutationWatcher();
			File.Delete(Path.Combine(workspaceB, "global.json"));

			await WaitUntilAsync(() => healthService.DevServerStarted && subject.ConnectionState == ConnectionState.Discovering);

			GetPrivateField<Task?>(monitor, "_monitor").Should().NotBeNull();
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").EffectiveWorkspaceDirectory.Should().Be(workspaceA);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("When MCP roots select a workspace outside the launch directory, the workspace watcher is realigned to the active root")]
	public async Task WhenMcpRootsSelectExternalWorkspace_WatcherTracksSelectedRoot()
	{
		var launchRoot = CreateTempDirectory();
		var externalRoot = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var externalWorkspace = await CreateUnoWorkspaceAsync(externalRoot, "src", "App.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var unresolvedWorkspace = new WorkspaceResolution
			{
				RequestedWorkingDirectory = launchRoot,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};
			var resolvedExternalWorkspace = await resolver.ResolveAsync(externalRoot);

			var created = CreateSubject();
			subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", launchRoot);
			SetPrivateField(subject, "_workspaceResolution", unresolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			subject.StartWorkspaceMutationWatcher();
			await subject.ApplyWorkspaceResolutionAsync(resolvedExternalWorkspace, WorkspaceTransitionTrigger.McpRoots);

			await WaitUntilAsync(() =>
				PathComparison.PathsEqual(
					GetPrivateField<string?>(subject, "_workspaceMutationWatcherRoot"),
					externalRoot));

			GetPrivateField<string?>(subject, "_workspaceMutationWatcherRoot").Should().Be(externalRoot);
			GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution").RequestedWorkingDirectory.Should().Be(externalRoot);
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(launchRoot);
			await DeleteDirectoryWithRetriesAsync(externalRoot);
		}
	}

	[TestMethod]
	[Description("Late watcher events after teardown do not throw when the debounce token source was already disposed")]
	public void WhenWorkspaceMutationArrivesAfterDebounceSourceWasDisposed_NoExceptionIsThrown()
	{
		var created = CreateSubject();
		var subject = created.Subject;
		using var disposedCts = new CancellationTokenSource();
		disposedCts.Dispose();
		SetPrivateField(subject, "_workspaceMutationDebounceCts", disposedCts);

		var method = typeof(ProxyLifecycleManager).GetMethod("OnWorkspaceMutation", BindingFlags.Instance | BindingFlags.NonPublic);
		method.Should().NotBeNull();

		var action = () => method!.Invoke(subject, [subject, new FileSystemEventArgs(WatcherChangeTypes.Changed, @"D:\repo", "global.json")]);

		action.Should().NotThrow();
	}

	[TestMethod]
	[Timeout(15_000)]
	[Description("Rapid FSW buffer-overflow errors must be debounced; without the fix each error spawns a separate workspace scan (issue #22828)")]
	public async Task When_WatcherReportsRapidErrors_FindSolutionFiles_IsBounded()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;

		try
		{
			var finder = new CountingSolutionFileFinder([]);
			var created = CreateSubject(solutionFileFinder: finder);
			subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", root);

			var method = typeof(ProxyLifecycleManager)
				.GetMethod("OnWorkspaceMutationWatcherError", BindingFlags.Instance | BindingFlags.NonPublic);
			method.Should().NotBeNull();

			// Simulate 10 rapid buffer-overflow events (the bug causes 10 separate workspace scans).
			for (var i = 0; i < 10; i++)
			{
				method!.Invoke(subject, [subject, new ErrorEventArgs(new InternalBufferOverflowException("simulated overflow"))]);
			}

			// Wait for at least one scan to complete (adaptive, up to 5s), then allow 300ms for
			// any concurrent calls to land so the count stabilizes before asserting.
			await WaitUntilAsync(() => finder.CallCount >= 1, timeoutMs: 5000);
			await Task.Delay(300);

			// With the fix, only the final debounced task runs the workspace scan → ≤ 2 calls.
			// (≤ 2 instead of 1 to tolerate a single timing edge case where two tasks slip through.)
			finder.CallCount.Should().BeLessThanOrEqualTo(2,
				"rapid FSW errors must be debounced; without the fix each error spawns a separate workspace scan");
		}
		finally
		{
			if (subject is not null)
			{
				await StopWatcherAndMonitorAsync(subject);
			}

			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Replacing the workspace debounce source installs the newest token source and cancels the previous one without disposing the superseded source yet")]
	public void ReplaceWorkspaceMutationDebounceSource_WhenCalled_ReplacesAndCancelsPrevious()
	{
		CancellationTokenSource? field = new();
		var first = field;

		var second = ProxyLifecycleManager.ReplaceWorkspaceMutationDebounceSource(ref field);

		field.Should().BeSameAs(second);
		first!.IsCancellationRequested.Should().BeTrue();
		Action disposeFirst = () => first.Dispose();
		disposeFirst.Should().NotThrow();

		var third = ProxyLifecycleManager.ReplaceWorkspaceMutationDebounceSource(ref field);

		field.Should().BeSameAs(third);
		second.IsCancellationRequested.Should().BeTrue();
		Action disposeSecond = () => second.Dispose();
		disposeSecond.Should().NotThrow();

		third.Dispose();
	}

	[TestMethod]
	[Description("A completed debounce source clears the current field when it is still the current debounce source")]
	public void ClearCompletedWorkspaceMutationDebounceSource_WhenCurrent_ClearsField()
	{
		CancellationTokenSource? field = new();
		var current = field;

		var cleared = ProxyLifecycleManager.ClearCompletedWorkspaceMutationDebounceSource(ref field, current!);

		cleared.Should().BeTrue();
		field.Should().BeNull();
		current!.IsCancellationRequested.Should().BeFalse();
		current.Dispose();
	}

	[TestMethod]
	[Description("A completed debounce source does not clear a newer current debounce source")]
	public void ClearCompletedWorkspaceMutationDebounceSource_WhenSuperseded_LeavesCurrentSourceIntact()
	{
		CancellationTokenSource? field = new();
		var completed = field;
		var current = ProxyLifecycleManager.ReplaceWorkspaceMutationDebounceSource(ref field);

		var cleared = ProxyLifecycleManager.ClearCompletedWorkspaceMutationDebounceSource(ref field, completed!);

		cleared.Should().BeFalse();
		field.Should().BeSameAs(current);
		current.IsCancellationRequested.Should().BeFalse();

		completed!.Dispose();
		current.Dispose();
	}

	[TestMethod]
	[Description("Watcher startup failures are logged and do not leave a partially initialized watcher behind")]
	public void WhenWatcherStartupFails_NoExceptionEscapesAndNoWatcherIsRetained()
	{
		var loggerProvider = new CollectingLoggerProvider();
		var created = CreateSubject(
			logger: loggerProvider.CreateLogger<ProxyLifecycleManager>(),
			workspaceMutationWatcherFactory: _ => throw new IOException("boom"));
		var subject = created.Subject;
		var workspaceRoot = CreateTempDirectory();

		try
		{
			SetPrivateField(subject, "_currentDirectory", workspaceRoot);

			var action = () => subject.StartWorkspaceMutationWatcher();

			action.Should().NotThrow();
			GetPrivateField<FileSystemWatcher?>(subject, "_workspaceMutationWatcher").Should().BeNull();
			GetPrivateField<string?>(subject, "_workspaceMutationWatcherRoot").Should().BeNull();
			loggerProvider.Messages.Should().Contain(message => message.Contains("Unable to start workspace mutation watcher", StringComparison.Ordinal));
		}
		finally
		{
			Directory.Delete(workspaceRoot, recursive: true);
		}
	}



	private static (ProxyLifecycleManager Subject, HealthService HealthService, DevServerMonitor Monitor) CreateSubject(
		ILogger<ProxyLifecycleManager>? logger = null,
		ISolutionFileFinder? solutionFileFinder = null,
		Func<string, FileSystemWatcher>? workspaceMutationWatcherFactory = null)
	{
		var services = new ServiceCollection()
			.AddSingleton(new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance))
			.BuildServiceProvider();
		var monitor = new DevServerMonitor(services, NullLogger<DevServerMonitor>.Instance);
		var upstreamClient = new McpUpstreamClient(NullLogger<McpUpstreamClient>.Instance, monitor);
		var toolListManager = new ToolListManager(NullLogger<ToolListManager>.Instance, upstreamClient);
		var healthService = new HealthService(upstreamClient, monitor, toolListManager);
		var stdioServer = new McpStdioServer(NullLogger<McpStdioServer>.Instance, toolListManager, healthService, upstreamClient);
		var finder = solutionFileFinder ?? new FileSystemSolutionFileFinder();
		var workspaceResolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance, finder);

		var subject = new ProxyLifecycleManager(
			logger ?? NullLogger<ProxyLifecycleManager>.Instance,
			monitor,
			upstreamClient,
			healthService,
			toolListManager,
			stdioServer,
			workspaceResolver,
			finder,
			workspaceMutationWatcherFactory);

		return (subject, healthService, monitor);
	}

	private static async Task InvokeSetRootsAsync(ProxyLifecycleManager subject, string[] roots)
	{
		var method = typeof(ProxyLifecycleManager).GetMethod("SetRoots", BindingFlags.Instance | BindingFlags.NonPublic);
		method.Should().NotBeNull();

		var task = method!.Invoke(subject, [roots]) as Task;
		task.Should().NotBeNull();
		await task!;
	}

	private static void SetPrivateField(object instance, string name, object? value)
	{
		var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		field.Should().NotBeNull($"Expected private field {name} to exist on {instance.GetType().Name}");
		field!.SetValue(instance, value);
	}

	private static T GetPrivateField<T>(object instance, string name)
	{
		var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		field.Should().NotBeNull($"Expected private field {name} to exist on {instance.GetType().Name}");
		return (T)field!.GetValue(instance)!;
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-proxy-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private static async Task<string> CreateUnoWorkspaceAsync(string root, string directoryName, string solutionFileName, string unoSdkVersion)
	{
		var workspace = Path.Combine(root, directoryName);
		Directory.CreateDirectory(workspace);
		await File.WriteAllTextAsync(
			Path.Combine(workspace, "global.json"),
			$"{{\"msbuild-sdks\":{{\"Uno.Sdk\":\"{unoSdkVersion}\"}}}}");
		await File.WriteAllTextAsync(Path.Combine(workspace, solutionFileName), string.Empty);
		return workspace;
	}

	private static async Task<string> CreateNonUnoWorkspaceAsync(string root, string directoryName, string solutionFileName)
	{
		var workspace = Path.Combine(root, directoryName);
		Directory.CreateDirectory(workspace);
		await File.WriteAllTextAsync(Path.Combine(workspace, "global.json"), """{"sdk":{"version":"10.0.100"}}""");
		await File.WriteAllTextAsync(Path.Combine(workspace, solutionFileName), string.Empty);
		return workspace;
	}

	private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollingIntervalMs = 100)
	{
		using var cts = new CancellationTokenSource(timeoutMs);
		while (!condition())
		{
			await Task.Delay(pollingIntervalMs, cts.Token);
		}
	}

	private static async Task StopWatcherAndMonitorAsync(ProxyLifecycleManager subject)
	{
		try
		{
			await subject.StopWorkspaceMutationWatcherAsync();
		}
		catch
		{
			// Ignore cleanup errors for test teardown.
		}

		var monitor = GetPrivateField<DevServerMonitor>(subject, "_devServerMonitor");
		await monitor.StopMonitoringAsync();
	}

	private static async Task DeleteDirectoryWithRetriesAsync(string path, int attempts = 10, int delayMs = 100)
	{
		for (var attempt = 1; attempt <= attempts; attempt++)
		{
			try
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive: true);
				}

				return;
			}
			catch (IOException) when (attempt < attempts)
			{
				await Task.Delay(delayMs);
			}
			catch (UnauthorizedAccessException) when (attempt < attempts)
			{
				await Task.Delay(delayMs);
			}
		}
	}

	private static string AlterPathCase(string path)
	{
		return string.Concat(path.Select(c =>
			char.IsLetter(c)
				? (char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c))
				: c));
	}

	private sealed class CollectingLoggerProvider
	{
		public List<string> Messages { get; } = [];

		public ILogger<T> CreateLogger<T>() => new CollectingLogger<T>(Messages);
	}

	private sealed class CollectingLogger<T>(List<string> messages) : ILogger<T>
	{
		private readonly List<string> _messages = messages;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			_messages.Add(formatter(state, exception));
		}
	}

	private sealed class CountingSolutionFileFinder(string[] solutions) : ISolutionFileFinder
	{
		private readonly string[] _solutions = solutions;
		private int _callCount;

		public int CallCount => Volatile.Read(ref _callCount);

		public string[] FindSolutionFiles(string directory, int maxDepth = 3)
		{
			Interlocked.Increment(ref _callCount);
			return [.. _solutions];
		}
	}
}
