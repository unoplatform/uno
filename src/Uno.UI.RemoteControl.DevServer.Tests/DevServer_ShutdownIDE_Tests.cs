using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Messaging.Messages;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class DevServer_ShutdownIDE_Tests
{
	private static ILogger<DevServerTestHelper> _logger = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
		});
		_logger = loggerFactory.CreateLogger<DevServerTestHelper>();
	}

	public TestContext? TestContext { get; set; }

	private CancellationToken CT => TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;

	[TestMethod]
	public async Task DevServer_ShouldShutdown_Gracefully_OnShutdownRequestMessage_FromIDE()
	{
		await using var helper = new DevServerTestHelper(_logger);
		try
		{
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			started.Should().BeTrue();
			helper.AssertRunning();

			// Simulate sending the shutdown message via the IDE channel
			var ideChannel = new TestIdeChannelClient(helper.IdeChannelGuid);
			await ideChannel.SendToDevServerAsync(new ShutdownIdeMessage(), CT);

			// Wait for the server to stop
			var shutdownTimeout = TimeSpan.FromSeconds(5);
			var shutdownStart = DateTime.UtcNow;
			while (helper.IsRunning && DateTime.UtcNow - shutdownStart < shutdownTimeout)
			{
				await Task.Delay(100, CT);
			}
			helper.IsRunning.Should().BeFalse("DevServer should exit after graceful shutdown message via IDE channel");
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}
}
