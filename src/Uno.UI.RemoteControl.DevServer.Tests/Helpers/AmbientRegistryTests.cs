using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Host;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class AmbientRegistryTests
{
	[TestMethod]
	public void ResolveLocalApplicationDataPath_ReturnsAbsolutePath()
	{
		var path = AmbientRegistry.ResolveLocalApplicationDataPath();

		path.Should().NotBeNullOrWhiteSpace();
		Path.IsPathRooted(path).Should().BeTrue();
	}

	[TestMethod]
	public void UpdateIdeChannel_WhenRegistered_UpdatesTheActiveRegistration()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new AmbientRegistry(logger);

		registry.Register(solution: @"D:\src\repo\Repo.sln", ppid: 123, httpPort: 61616, ideChannelId: null);
		try
		{
			registry.UpdateIdeChannel("new-channel");

			var updated = registry.GetActiveDevServerForPort(61616);
			updated.Should().NotBeNull();
			updated!.IdeChannelId.Should().Be("new-channel");
		}
		finally
		{
			registry.Unregister();
		}
	}
}
