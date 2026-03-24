using System.IO.Pipes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Uno.UI.RemoteControl.Host.IdeChannel;

namespace Uno.UI.RemoteControl.DevServer.Tests.IDEChannel;

[TestClass]
public class Given_IdeChannelServer
{
	[TestMethod]
	public async Task WhenRebinding_ChannelIsListeningBeforeTheIdeConnects()
	{
		var channelId = $"ide-channel-{Guid.NewGuid():N}";
		using var server = new IdeChannelServer(
			new NullLogger<IdeChannelServer>(),
			new StaticOptionsMonitor<IdeChannelServerOptions>(new IdeChannelServerOptions()));

		var rebindTask = ((IIdeChannelManager)server).RebindAsync(channelId);
		var rebound = await rebindTask.WaitAsync(TimeSpan.FromSeconds(1));

		rebound.Should().BeTrue();
		((IIdeChannelManager)server).ChannelId.Should().Be(channelId);
		((IIdeChannelManager)server).IsConnected.Should().BeFalse();

		using var client = new NamedPipeClientStream(
			serverName: ".",
			pipeName: channelId,
			direction: PipeDirection.InOut,
			options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

		using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(ct.Token);

		(await server.WaitForReady()).Should().BeTrue();
		((IIdeChannelManager)server).IsConnected.Should().BeTrue();
	}

	private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
	{
		public T CurrentValue => currentValue;

		public T Get(string? name) => currentValue;

		public IDisposable? OnChange(Action<T, string?> listener) => null;
	}
}
