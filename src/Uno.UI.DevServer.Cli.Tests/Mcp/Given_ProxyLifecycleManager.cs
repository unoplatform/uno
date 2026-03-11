using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_ProxyLifecycleManager
{
	[TestMethod]
	[Description("Late MCP roots that confirm the selected workspace do not restart or degrade the current session")]
	public async Task WhenRootsConfirmCurrentWorkspace_SessionRemainsUnchanged()
	{
		var root = CreateTempDirectory();

		try
		{
			var workspaceDirectory = Path.Combine(root, "src");
			Directory.CreateDirectory(workspaceDirectory);
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "global.json"), """{"msbuild-sdks":{"Uno.Sdk":"6.6.0-dev.1"}}""");
			await File.WriteAllTextAsync(Path.Combine(workspaceDirectory, "StudioLive.slnx"), string.Empty);

			var resolver = new WorkspaceResolver(NullLogger<WorkspaceResolver>.Instance);
			var workspaceResolution = await resolver.ResolveAsync(root);

			var (subject, healthService, _) = CreateSubject();
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", workspaceResolution);

			await InvokeSetRootsAsync(subject, [new Uri(workspaceDirectory).AbsoluteUri]);

			subject.ConnectionState.Should().Be(ConnectionState.Initializing);
			healthService.ConnectionState.Should().Be(ConnectionState.Initializing);
			healthService.DevServerStarted.Should().BeFalse();
		}
		finally
		{
			Directory.Delete(root, recursive: true);
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
			Directory.Delete(root, recursive: true);
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
			Directory.Delete(root, recursive: true);
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

			Directory.Delete(root, recursive: true);
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

			Directory.Delete(root, recursive: true);
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

			Directory.Delete(root, recursive: true);
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

			Directory.Delete(root, recursive: true);
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

			var created = CreateSubject();
			subject = created.Subject;
			var healthService = created.HealthService;
			var monitor = created.Monitor;
			SetPrivateField(subject, "_currentDirectory", root);
			SetPrivateField(subject, "_workspaceResolution", resolvedWorkspace);
			SetPrivateField(subject, "_devServerPort", 0);
			SetPrivateField(subject, "_forwardedArgs", new List<string>());

			await subject.ApplyWorkspaceResolutionAsync(resolvedWorkspace, WorkspaceTransitionTrigger.FileSystem);
			subject.StartWorkspaceMutationWatcher();

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

			Directory.Delete(root, recursive: true);
		}
	}

	private static (ProxyLifecycleManager Subject, HealthService HealthService, DevServerMonitor Monitor) CreateSubject()
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
			NullLogger<ProxyLifecycleManager>.Instance,
			monitor,
			upstreamClient,
			healthService,
			toolListManager,
			stdioServer,
			workspaceResolver);

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

	private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollingIntervalMs = 100)
	{
		using var cts = new CancellationTokenSource(timeoutMs);
		while (!condition())
		{
			await Task.Delay(pollingIntervalMs, cts.Token);
		}
	}
}
