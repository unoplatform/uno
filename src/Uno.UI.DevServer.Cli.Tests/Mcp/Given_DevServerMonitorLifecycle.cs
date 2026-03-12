using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
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
		var currentDirectory = @"D:\repo";
		var selectedSolution = @"D:\repo\srcB\AppB.slnx";
		string[] solutionFiles =
		[
			@"D:\repo\srcA\AppA.slnx",
			selectedSolution,
		];
		var workspaceResolution = new WorkspaceResolution
		{
			RequestedWorkingDirectory = currentDirectory,
			EffectiveWorkspaceDirectory = @"D:\repo\srcB",
			SelectedSolutionPath = selectedSolution,
			SelectedGlobalJsonPath = @"D:\repo\srcB\global.json",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = solutionFiles,
		};

		var solution = DevServerMonitor.DetermineSolutionToLaunch(currentDirectory, solutionFiles, workspaceResolution);

		solution.Should().Be(selectedSolution);
	}

	[TestMethod]
	[Description("When no explicit solution is selected, DevServerMonitor chooses a deterministic default solution")]
	public void WhenWorkspaceResolutionDoesNotSelectSolution_UsesSortedSolutionOrder()
	{
		var currentDirectory = @"D:\repo";
		string[] solutionFiles =
		[
			@"D:\repo\srcB\AppB.slnx",
			@"D:\repo\srcA\AppA.slnx",
		];

		var solution = DevServerMonitor.DetermineSolutionToLaunch(currentDirectory, solutionFiles, workspaceResolution: null);

		solution.Should().Be(@"D:\repo\srcA\AppA.slnx");
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

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-monitor-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}
