using System.IO.Pipes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.DevServer.Tests.IDEChannel;

[TestClass]
public class Given_IdeChannelServer
{
	[TestMethod]
	public async Task WhenRebinding_ChannelIsListeningBeforeTheIdeConnects()
	{
		var channelId = $"ide-channel-{Guid.NewGuid():N}";
		using var server = CreateServer();

		var rebound = await Rebind(server, channelId);

		rebound.Should().BeTrue();
		Manager(server).ChannelId.Should().Be(channelId);
		Manager(server).IsConnected.Should().BeFalse();

		using var client = CreateClient(channelId);
		using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(ct.Token);

		(await server.WaitForReady()).Should().BeTrue();
		Manager(server).IsConnected.Should().BeTrue();
	}

	[TestMethod]
	public async Task WhenRebinding_SameChannelId_DoesNotRecreatePipe()
	{
		var channelId = $"ide-channel-{Guid.NewGuid():N}";
		using var server = CreateServer();

		// First rebind — creates the pipe.
		var rebound1 = await Rebind(server, channelId);
		rebound1.Should().BeTrue();

		// Connect a client to the first pipe.
		using var client = CreateClient(channelId);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(cts.Token);
		(await server.WaitForReady()).Should().BeTrue();
		Manager(server).IsConnected.Should().BeTrue();

		// Second rebind with the SAME channelId — should be idempotent.
		var rebound2 = await Rebind(server, channelId);
		rebound2.Should().BeTrue();

		// The existing connection must still be alive.
		Manager(server).ChannelId.Should().Be(channelId);
		Manager(server).IsConnected.Should().BeTrue();
	}

	[TestMethod]
	public async Task WhenRebinding_DifferentChannelId_DisposesOldPipe()
	{
		var channelA = $"ide-channel-{Guid.NewGuid():N}";
		var channelB = $"ide-channel-{Guid.NewGuid():N}";
		using var server = CreateServer();

		// Bind to channel A and connect a client.
		(await Rebind(server, channelA)).Should().BeTrue();
		using var clientA = CreateClient(channelA);
		using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await clientA.ConnectAsync(cts1.Token);
		(await server.WaitForReady()).Should().BeTrue();

		// Rebind to channel B — channel A should be disposed.
		(await Rebind(server, channelB)).Should().BeTrue();
		Manager(server).ChannelId.Should().Be(channelB);

		// A client should be able to connect to channel B.
		using var clientB = CreateClient(channelB);
		using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await clientB.ConnectAsync(cts2.Token);
		(await server.WaitForReady()).Should().BeTrue();
		Manager(server).IsConnected.Should().BeTrue();
	}

	[TestMethod]
	public async Task WhenWaitForReady_CallerCancelled_PipeStillAcceptsConnections()
	{
		var channelId = $"ide-channel-{Guid.NewGuid():N}";
		using var server = CreateServer();

		(await Rebind(server, channelId)).Should().BeTrue();

		// A caller cancels WaitForReady before the IDE connects — this must NOT kill the pipe.
		using var shortCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
		var waitAct = async () => await server.WaitForReady(shortCts.Token);
		await waitAct.Should().ThrowAsync<OperationCanceledException>();

		// The pipe should still accept a connection.
		using var client = CreateClient(channelId);
		using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(connectCts.Token);

		// WaitForReady without cancellation should now succeed.
		(await server.WaitForReady()).Should().BeTrue();
		Manager(server).IsConnected.Should().BeTrue();
	}

	[TestMethod]
	public async Task WhenConcurrentRebinds_LastWriterWins()
	{
		using var server = CreateServer();

		// Fire multiple rebinds concurrently — the last one to swap wins.
		var channels = Enumerable.Range(0, 5)
			.Select(_ => $"ide-channel-{Guid.NewGuid():N}")
			.ToArray();

		var tasks = channels.Select(id => Rebind(server, id)).ToArray();
		await Task.WhenAll(tasks);

		// Exactly one channel should be active.
		var activeChannelId = Manager(server).ChannelId;
		activeChannelId.Should().NotBeNullOrWhiteSpace();
		channels.Should().Contain(activeChannelId);

		// A client must be able to connect to the winning channel.
		using var client = CreateClient(activeChannelId!);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(cts.Token);
		(await server.WaitForReady()).Should().BeTrue();
	}

	[TestMethod]
	public async Task WhenRebindAfterStartup_TrySendSucceedsOnceClientConnects()
	{
		var channelId = $"ide-channel-{Guid.NewGuid():N}";
		using var server = CreateServer();
		IIdeChannel channel = server;

		// At startup the Host has no IDE channel — TrySend should fail.
		var sentBeforeRebind = await channel.TrySendToIdeAsync(
			new KeepAliveIdeMessage("test"), CancellationToken.None);
		sentBeforeRebind.Should().BeFalse("no channel is configured yet");

		// Rebind creates the pipe.
		(await Rebind(server, channelId)).Should().BeTrue();

		// Connect a client.
		using var client = CreateClient(channelId);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(cts.Token);
		(await server.WaitForReady()).Should().BeTrue();

		// Now TrySend should succeed — this is the path UnoDevEnvironmentService
		// takes after receiving the first KeepAlive from the IDE.
		var sentAfterRebind = await channel.TrySendToIdeAsync(
			new KeepAliveIdeMessage("test"), CancellationToken.None);
		sentAfterRebind.Should().BeTrue("channel is connected after rebind");
	}

	[TestMethod]
	public async Task WhenRebindAfterStartup_MessageFromIdeSignalsReconnect()
	{
		// Integration test: simulates what UnoDevEnvironmentService does when the
		// Host starts without --ideChannel (MCP scenario). The service tries to
		// send a Ready message, fails, subscribes to MessageFromIde, then retries
		// after the IDE channel is established via rebind + client connection.

		var channelId = $"ide-channel-{Guid.NewGuid():N}";
		using var server = CreateServer();
		IIdeChannel channel = server;

		// Step 1: TrySend fails (no channel).
		var sent = await channel.TrySendToIdeAsync(
			new KeepAliveIdeMessage("ready-sim"), CancellationToken.None);
		sent.Should().BeFalse();

		// Step 2: Subscribe to incoming messages (like UnoDevEnvironmentService does).
		var ideMessageReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		channel.MessageFromIde += (_, _) => ideMessageReceived.TrySetResult();

		// Step 3: Rebind + client connects.
		(await Rebind(server, channelId)).Should().BeTrue();
		using var client = CreateClient(channelId);
		using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await client.ConnectAsync(connectCts.Token);
		(await server.WaitForReady()).Should().BeTrue();

		// Step 4: Client sends a KeepAlive (simulates IDE sending its first message).
		var rpcClient = StreamJsonRpc.JsonRpc.Attach<IIdeChannelServer>(client);
		await rpcClient.SendToDevServerAsync(
			IdeMessageSerializer.Serialize(new KeepAliveIdeMessage("IDE")),
			CancellationToken.None);

		// Step 5: The MessageFromIde event should fire (signal for retry).
		using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		await ideMessageReceived.Task.WaitAsync(waitCts.Token);

		// Step 6: Re-send succeeds — the IDE can now leave "Starting...".
		var sentAfter = await channel.TrySendToIdeAsync(
			new KeepAliveIdeMessage("ready-sim"), CancellationToken.None);
		sentAfter.Should().BeTrue("channel is live after IDE message triggers retry");
	}

	// ── Helpers ──────────────────────────────────────────────────────

	private static IdeChannelServer CreateServer()
		=> new(
			new NullLogger<IdeChannelServer>(),
			new StaticOptionsMonitor<IdeChannelServerOptions>(new IdeChannelServerOptions()));

	private static IIdeChannelManager Manager(IdeChannelServer server)
		=> server;

	private static async Task<bool> Rebind(IdeChannelServer server, string channelId)
		=> await ((IIdeChannelManager)server).RebindAsync(channelId)
			.WaitAsync(TimeSpan.FromSeconds(1));

	private static NamedPipeClientStream CreateClient(string channelId)
		=> new(
			serverName: ".",
			pipeName: channelId,
			direction: PipeDirection.InOut,
			options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

	private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
	{
		public T CurrentValue => currentValue;

		public T Get(string? name) => currentValue;

		public IDisposable? OnChange(Action<T, string?> listener) => null;
	}
}
