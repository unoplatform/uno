using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Messaging.Messages;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class DevServer_ShutdownTests
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
	public async Task DevServer_ShouldShutdown_Gracefully_OnShutdownIdeMessage()
	{
		await using var helper = new DevServerTestHelper(_logger);
		try
		{
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();
			started.Should().BeTrue();
			helper.AssertRunning();

			// Création du client RemoteControl
			var client = RemoteControlClient.Initialize(
				typeof(object),
				new[] { new ServerEndpointAttribute("localhost", helper.Port) }
			);

			// Attendre la connexion du client
			var timeout = TimeSpan.FromSeconds(10);
			var start = DateTime.UtcNow;
			while (client.Status.State != RemoteControlStatus.ConnectionState.Connected && DateTime.UtcNow - start < timeout)
			{
				await Task.Delay(100, CT);
			}
			client.Status.State.Should().Be(RemoteControlStatus.ConnectionState.Connected, "Client should connect to DevServer");

			// Envoi du message de shutdown via le client (nouveau nom générique)
			await client.SendMessage(new ShutdownClientMessage());

			// Attendre que le serveur s'arrête
			var shutdownTimeout = TimeSpan.FromSeconds(5);
			var shutdownStart = DateTime.UtcNow;
			while (helper.IsRunning && DateTime.UtcNow - shutdownStart < shutdownTimeout)
			{
				await Task.Delay(100, CT);
			}
			helper.IsRunning.Should().BeFalse("DevServer should exit after graceful shutdown message");
		}
		finally
		{
			await helper.StopAsync(CT);
		}
	}
}
