using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_DevServerMonitorLifecycle
{
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
