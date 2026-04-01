using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_HealthService
{
	[TestMethod]
	[Description("uno_health keeps the explicitly selected workspace metadata even while the host is still launching")]
	public void WhenSelectionIsKnownButDiscoveryIsIncomplete_SelectionMetadataIsPreserved()
	{
		var (_, healthService, monitor) = CreateSubject();
		var selectedSolutionPath = @"D:\src\repo\src\App.slnx";
		var workspaceDirectory = @"D:\src\repo\src";
		var candidateSolutions = new[]
		{
			selectedSolutionPath,
			@"D:\src\repo\src\Other.slnx",
		};

		SetPrivateField(
			monitor,
			"_lastDiscoveryInfo",
			new DiscoveryInfo
			{
				RequestedWorkingDirectory = @"D:\src\repo",
				EffectiveWorkspaceDirectory = workspaceDirectory,
				ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
				CandidateSolutions = candidateSolutions,
			});

		healthService.DevServerStarted = true;
		healthService.ConnectionState = ConnectionState.Launching;
		healthService.CurrentSelection = new WorkspaceSelectionSnapshot
		{
			EffectiveWorkspaceDirectory = workspaceDirectory,
			SelectedSolutionPath = selectedSolutionPath,
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			SelectionSource = WorkspaceSelectionSource.UserSelected,
			CandidateSolutions = candidateSolutions,
		};

		var report = healthService.BuildHealthReport();

		report.Status.Should().NotBe(HealthStatus.Healthy);
		report.SelectedSolutionPath.Should().Be(selectedSolutionPath);
		report.EffectiveWorkspaceDirectory.Should().Be(workspaceDirectory);
		report.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
		report.CandidateSolutions.Should().BeEquivalentTo(candidateSolutions);
		report.Discovery.Should().NotBeNull();
		report.Discovery!.SelectedSolutionPath.Should().Be(selectedSolutionPath);
		report.Discovery.SelectionSource.Should().Be(WorkspaceSelectionSource.UserSelected);
	}

	private static (ProxyLifecycleManager Subject, HealthService HealthService, DevServerMonitor Monitor) CreateSubject()
	{
		var services = new ServiceCollection()
			.AddSingleton(new UnoToolsLocator(NullLogger<UnoToolsLocator>.Instance))
			.BuildServiceProvider();
		var monitor = new DevServerMonitor(services, NullLogger<DevServerMonitor>.Instance);
		var upstreamClient = new McpUpstreamClient(NullLogger<McpUpstreamClient>.Instance, monitor);
		var toolListManager = new ToolListManager(NullLogger<ToolListManager>.Instance, upstreamClient);
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

	private static void SetPrivateField(object instance, string name, object? value)
	{
		var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		field.Should().NotBeNull($"Expected private field {name} to exist on {instance.GetType().Name}");
		field!.SetValue(instance, value);
	}
}
