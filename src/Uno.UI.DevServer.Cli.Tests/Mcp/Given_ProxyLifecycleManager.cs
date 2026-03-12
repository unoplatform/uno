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
	[Description("Selecting a valid Uno candidate starts the deferred DevServer and marks the workspace as explicitly selected")]
	public async Task WhenSelectingValidUnoSolutionAfterDeferredStartup_DevServerStarts()
	{
		var root = CreateTempDirectory();
		ProxyLifecycleManager? subject = null;
		DevServerMonitor? monitor = null;

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "StudioLive.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspaceDirectory, "StudioLive.slnx");
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
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "StudioLive.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspaceDirectory, "StudioLive.slnx");
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
			var workspaceDirectory = await CreateNonUnoWorkspaceAsync(root, "src", "StudioLive.slnx");
			var solutionPath = Path.Combine(workspaceDirectory, "StudioLive.slnx");
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
			result.Issues.Should().NotBeNull();
			result.Issues!.Should().Contain(issue => issue.Code == IssueCode.WorkspaceNotResolved || issue.Code == IssueCode.UnoSdkNotInGlobalJson || issue.Code == IssueCode.NoSolutionFound);
			healthService.DevServerStarted.Should().BeFalse();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Selecting a solution that is not one of the current candidates is rejected cleanly")]
	public async Task WhenSelectingSolutionOutsideCandidates_RequestIsRejected()
	{
		var root = CreateTempDirectory();
		var externalRoot = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "StudioLive.slnx", "6.6.0-dev.1");
			var externalWorkspace = await CreateUnoWorkspaceAsync(externalRoot, "other", "Other.slnx", "6.6.0-dev.2");
			var externalSolution = Path.Combine(externalWorkspace, "Other.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			var subject = created.Subject;
			var healthService = created.HealthService;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			var result = await subject.SelectSolutionAsync(externalSolution);

			result.Status.Should().Be("rejected");
			result.Message.Should().ContainEquivalentOf("candidate");
			result.Issues.Should().NotBeNull();
			healthService.DevServerStarted.Should().BeFalse();
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
			await DeleteDirectoryWithRetriesAsync(externalRoot);
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
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "StudioLive.slnx"), string.Empty);

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
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "StudioLive.slnx"), string.Empty);

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
	[Description("Late MCP roots that point to a different Uno workspace degrade the session instead of switching workspaces")]
	public async Task WhenRootsPointToDifferentWorkspace_SessionBecomesDiagnostic()
	{
		var root = CreateTempDirectory();

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

			var (subject, healthService, _) = CreateSubject();
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			await InvokeSetRootsAsync(subject, [new Uri(workspaceB).AbsoluteUri]);

			subject.ConnectionState.Should().Be(ConnectionState.Degraded);
			healthService.ConnectionState.Should().Be(ConnectionState.Degraded);
			healthService.DevServerStarted.Should().BeFalse();

			var currentResolution = GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution");
			currentResolution.EffectiveWorkspaceDirectory.Should().Be(workspaceA);
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	[TestMethod]
	[Description("Late MCP roots that still do not resolve to a valid Uno workspace keep the session in immediate diagnostic mode")]
	public async Task WhenRootsRemainUnresolved_SessionStaysDiagnosticWithoutStarting()
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

			subject.ConnectionState.Should().Be(ConnectionState.Degraded);
			healthService.ConnectionState.Should().Be(ConnectionState.Degraded);
			healthService.DevServerStarted.Should().BeFalse();

			var currentResolution = GetPrivateField<WorkspaceResolution>(subject, "_workspaceResolution");
			currentResolution.ResolutionKind.Should().Be(WorkspaceResolutionKind.NoValidWorkspace);
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
		ProxyLifecycleManager.IsWorkspaceMutationPath(@"D:\repo\global.json").Should().BeTrue();
		ProxyLifecycleManager.IsWorkspaceMutationPath(@"D:\repo\App.sln").Should().BeTrue();
		ProxyLifecycleManager.IsWorkspaceMutationPath(@"D:\repo\App.slnx").Should().BeTrue();
		ProxyLifecycleManager.IsWorkspaceMutationPath(@"D:\repo\README.md").Should().BeFalse();
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
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "StudioLive.slnx", "6.6.0-dev.1");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			var subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			var result = await subject.SelectSolutionAsync("C:\\\0\\bad.slnx");

			result.Status.Should().Be("rejected");
			result.DevServerAction.Should().Be("None");
			result.Message.Should().ContainEquivalentOf("valid absolute path");
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
	[Description("Explicit solution selection uses filesystem-aware path comparison when validating candidate solutions")]
	public async Task WhenSelectingSolutionWithDifferentCase_UsesFilesystemPathSemantics()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = await CreateUnoWorkspaceAsync(root, "src", "StudioLive.slnx", "6.6.0-dev.1");
			var solutionPath = Path.Combine(workspaceDirectory, "StudioLive.slnx");
			var alteredCaseSolutionPath = AlterPathCase(solutionPath);
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var created = CreateSubject();
			var subject = created.Subject;
			var healthService = created.HealthService;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			var result = await subject.SelectSolutionAsync(alteredCaseSolutionPath);

			if (PathComparison.PathsEqual(solutionPath, alteredCaseSolutionPath))
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
	[Description("Replacing the workspace debounce source installs the newest token source and cancels the previous one")]
	public void ReplaceWorkspaceMutationDebounceSource_WhenCalled_ReplacesAndCancelsPrevious()
	{
		CancellationTokenSource? field = new();
		var first = field;

		var second = ProxyLifecycleManager.ReplaceWorkspaceMutationDebounceSource(ref field);

		field.Should().BeSameAs(second);
		first!.IsCancellationRequested.Should().BeTrue();
		Action cancelDisposedFirst = () => first.Cancel();
		cancelDisposedFirst.Should().Throw<ObjectDisposedException>();

		var third = ProxyLifecycleManager.ReplaceWorkspaceMutationDebounceSource(ref field);

		field.Should().BeSameAs(third);
		second.IsCancellationRequested.Should().BeTrue();
		Action cancelDisposedSecond = () => second.Cancel();
		cancelDisposedSecond.Should().Throw<ObjectDisposedException>();

		third.Dispose();
	}

	[TestMethod]
	[Description("A completed debounce source is cleared and disposed only if it is still the current debounce source")]
	public void ClearCompletedWorkspaceMutationDebounceSource_WhenCurrent_ClearsAndDisposes()
	{
		CancellationTokenSource? field = new();
		var current = field;

		ProxyLifecycleManager.ClearCompletedWorkspaceMutationDebounceSource(ref field, current!);

		field.Should().BeNull();
		current!.IsCancellationRequested.Should().BeTrue();
		Action cancelDisposedCurrent = () => current.Cancel();
		cancelDisposedCurrent.Should().Throw<ObjectDisposedException>();
	}

	[TestMethod]
	[Description("A completed debounce source does not clear or dispose a newer current debounce source")]
	public void ClearCompletedWorkspaceMutationDebounceSource_WhenSuperseded_LeavesCurrentSourceIntact()
	{
		CancellationTokenSource? field = new();
		var completed = field;
		var current = ProxyLifecycleManager.ReplaceWorkspaceMutationDebounceSource(ref field);

		ProxyLifecycleManager.ClearCompletedWorkspaceMutationDebounceSource(ref field, completed!);

		field.Should().BeSameAs(current);
		current.IsCancellationRequested.Should().BeFalse();

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

	[TestMethod]
	[Description("Explicit solution selection updates the workspace hash to the selected workspace")]
	public async Task WhenSelectingSolution_WorkspaceHashTracksSelectedWorkspace()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceA = await CreateUnoWorkspaceAsync(root, "srcA", "AppA.slnx", "6.6.0-dev.1");
			var workspaceB = await CreateUnoWorkspaceAsync(root, "srcB", "AppB.slnx", "6.6.0-dev.2");
			var solutionB = Path.Combine(workspaceB, "AppB.slnx");
			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var resolutionA = await resolver.ResolveAsync(workspaceA);

			var created = CreateSubject();
			var subject = created.Subject;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", resolutionA);

			await subject.SelectSolutionAsync(solutionB);

			GetPrivateField<string?>(subject, "_workspaceHash")
				.Should().Be(ToolCacheFile.ComputeWorkspaceHash(workspaceB));
		}
		finally
		{
			await DeleteDirectoryWithRetriesAsync(root);
		}
	}

	private static (ProxyLifecycleManager Subject, HealthService HealthService, DevServerMonitor Monitor) CreateSubject(
		ILogger<ProxyLifecycleManager>? logger = null,
		Func<string, FileSystemWatcher>? workspaceMutationWatcherFactory = null)
	{
		var services = new ServiceCollection()
			.AddSingleton(new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance))
			.BuildServiceProvider();
		var monitor = new DevServerMonitor(services, NullLogger<DevServerMonitor>.Instance);
		var upstreamClient = new McpUpstreamClient(NullLogger<McpUpstreamClient>.Instance, monitor);
		var toolListManager = new ToolListManager(NullLogger<ToolListManager>.Instance, upstreamClient, monitor)
		{
			IsToolCacheEnabled = false,
		};
		var healthService = new HealthService(upstreamClient, monitor, toolListManager);
		var stdioServer = new McpStdioServer(NullLogger<McpStdioServer>.Instance, toolListManager, healthService, upstreamClient);
		var workspaceResolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);

		var subject = new ProxyLifecycleManager(
			logger ?? NullLogger<ProxyLifecycleManager>.Instance,
			monitor,
			upstreamClient,
			healthService,
			toolListManager,
			stdioServer,
			workspaceResolver,
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
}
