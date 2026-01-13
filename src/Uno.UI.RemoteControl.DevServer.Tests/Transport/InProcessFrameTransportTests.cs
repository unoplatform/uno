using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.DevServer.Tests.Transport;

[TestClass]
public class InProcessFrameTransportTests
{
	private sealed class BlockingSynchronizationContext : SynchronizationContext
	{
		private readonly Queue<(SendOrPostCallback Callback, object? State)> _work = new();
		private readonly Lock _gate = new();

		public int PendingCount
		{
			get
			{
				lock (_gate)
				{
					return _work.Count;
				}
			}
		}

		public override void Post(SendOrPostCallback d, object? state)
		{
			lock (_gate)
			{
				_work.Enqueue((d, state));
			}
		}

		public void ExecuteAll()
		{
			while (true)
			{
				SendOrPostCallback? callback;
				object? state;

				lock (_gate)
				{
					if (_work.Count == 0)
					{
						return;
					}

					(callback, state) = _work.Dequeue();
				}

				callback(state);
			}
		}
	}

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

	[TestMethod]
	public async Task InProcessTransport_Receive_Should_Not_Complete_Synchronously_On_Send()
	{
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		var original = SynchronizationContext.Current;
		var context = new BlockingSynchronizationContext();

		try
		{
			SynchronizationContext.SetSynchronizationContext(context);

			var receiveTask = peer2.ReceiveAsync(CancellationToken.None);
			receiveTask.IsCompleted.Should().BeFalse();

			await peer1.SendAsync(frame, CancellationToken.None).ConfigureAwait(false);

			receiveTask.IsCompleted.Should().BeFalse();
			context.PendingCount.Should().Be(1);

			context.ExecuteAll();

			var received = await receiveTask.ConfigureAwait(false);
			received.Should().NotBeNull();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(original);
		}
	}

	[TestMethod]
	public async Task InProcessTransport_Should_Handle_Concurrent_Burst_Sends()
	{
		const int producerCount = 8;
		const int framesPerProducer = 5000;
		var totalFrames = producerCount * framesPerProducer;
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;

		var senderTasks = new Task[producerCount];
		for (var p = 0; p < producerCount; p++)
		{
			var producerId = p;
			senderTasks[p] = Task.Run(async () =>
			{
				for (var i = 0; i < framesPerProducer; i++)
				{
					var id = (short)((producerId * framesPerProducer + i) % short.MaxValue);
					await peer1.SendAsync(Frame.Create(id, "scope", "name", id), CancellationToken.None);
				}
			});
		}

		var receiver = Task.Run(async () =>
		{
			for (var i = 0; i < totalFrames; i++)
			{
				var frame = await peer2.ReceiveAsync(CancellationToken.None);
				frame.Should().NotBeNull();
			}

			var end = await peer2.ReceiveAsync(CancellationToken.None);
			end.Should().BeNull();
		});

		await Task.WhenAll(senderTasks);
		await peer1.CloseAsync();
		await receiver;
	}
}
