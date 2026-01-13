using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests.Transport;

[TestClass]
public class InProcessFrameTransportTests
{
	[TestMethod]
	public async Task InProcessTransport_Should_Send_And_Receive()
	{
		var (client, server) = FrameTransportPair.Create();
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		await client.SendAsync(frame, CancellationToken.None);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var received = await server.ReceiveAsync(cts.Token);

		received.Should().NotBeNull();
		received!.Scope.Should().Be(frame.Scope);
		received.Name.Should().Be(frame.Name);
		received.Content.Should().Be(frame.Content);

		await client.CloseAsync();
		await server.CloseAsync();
	}

	[TestMethod]
	public async Task InProcessTransport_Close_Should_End_Remote_Receive()
	{
		var (client, server) = FrameTransportPair.Create();

		await client.CloseAsync();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var received = await server.ReceiveAsync(cts.Token);
		received.Should().BeNull();
	}

	[TestMethod]
	public async Task InProcessTransport_Close_After_Send_Should_Drain_Then_End()
	{
		var (client, server) = FrameTransportPair.Create();
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		await client.SendAsync(frame, CancellationToken.None);
		await client.CloseAsync();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var first = await server.ReceiveAsync(cts.Token);
		first.Should().NotBeNull();

		var second = await server.ReceiveAsync(cts.Token);
		second.Should().BeNull();
	}
}
