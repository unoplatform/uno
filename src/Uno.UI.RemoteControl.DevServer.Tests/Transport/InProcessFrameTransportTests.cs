using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests.Transport;

[TestClass]
public class InProcessFrameTransportTests
{
	[TestMethod]
	public async Task InProcessTransport_Should_Send_And_Receive()
	{
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		await peer1.SendAsync(frame, CancellationToken.None);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var received = await peer2.ReceiveAsync(cts.Token);

		received.Should().NotBeNull();
		received!.Scope.Should().Be(frame.Scope);
		received.Name.Should().Be(frame.Name);
		received.Content.Should().Be(frame.Content);
	}

	[TestMethod]
	public async Task InProcessTransport_Close_Should_End_Remote_Receive()
	{
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;

		await peer1.CloseAsync();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var received = await peer2.ReceiveAsync(cts.Token);
		received.Should().BeNull();
	}

	[TestMethod]
	public async Task InProcessTransport_Close_After_Send_Should_Drain_Then_End()
	{
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		await peer1.SendAsync(frame, CancellationToken.None);
		await peer1.CloseAsync();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var first = await peer2.ReceiveAsync(cts.Token);
		first.Should().NotBeNull();

		var second = await peer2.ReceiveAsync(cts.Token);
		second.Should().BeNull();
	}
}
