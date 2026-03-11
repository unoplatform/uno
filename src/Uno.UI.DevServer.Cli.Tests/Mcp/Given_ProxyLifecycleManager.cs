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

			var (subject, healthService) = CreateSubject();
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

			var (subject, healthService) = CreateSubject();
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

			var (subject, healthService) = CreateSubject();
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

	private static (ProxyLifecycleManager Subject, HealthService HealthService) CreateSubject()
	{
		var services = new ServiceCollection().BuildServiceProvider();
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

		return (subject, healthService);
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
}
