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
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		// Act
		await peer1.SendAsync(frame, CancellationToken.None);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var received = await peer2.ReceiveAsync(cts.Token);

		// Assert
		received.Should().NotBeNull();
		received!.Scope.Should().Be(frame.Scope);
		received.Name.Should().Be(frame.Name);
		received.Content.Should().Be(frame.Content);
	}

	[TestMethod]
	public async Task InProcessTransport_Close_Should_End_Remote_Receive()
	{
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;

		// Act
		await peer1.CloseAsync();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var received = await peer2.ReceiveAsync(cts.Token);

		// Assert
		received.Should().BeNull();
	}

	[TestMethod]
	public async Task InProcessTransport_Close_After_Send_Should_Drain_Then_End()
	{
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		var frame = Frame.Create(1, "scope", "name", new { Value = 42 });

		// Act
		await peer1.SendAsync(frame, CancellationToken.None);
		await peer1.CloseAsync();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var first = await peer2.ReceiveAsync(cts.Token);
		first.Should().NotBeNull();

		var second = await peer2.ReceiveAsync(cts.Token);

		// Assert
		second.Should().BeNull();
	}

	[TestMethod]
	public async Task InProcessTransport_Receive_Should_Not_Complete_Synchronously_On_Send()
	{
		// Arrange
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

			// Act
			await peer1.SendAsync(frame, CancellationToken.None).ConfigureAwait(false);

			receiveTask.IsCompleted.Should().BeFalse();
			context.PendingCount.Should().Be(1);

			context.ExecuteAll();

			var received = await receiveTask.ConfigureAwait(false);

			// Assert
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
		// Arrange
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

		// Act
		await Task.WhenAll(senderTasks);
		await peer1.CloseAsync();
		await receiver;
	}

	[TestMethod]
	public async Task InProcessTransport_Close_Both_Sides_Should_Complete()
	{
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;

		var receive1 = peer1.ReceiveAsync(CancellationToken.None);
		var receive2 = peer2.ReceiveAsync(CancellationToken.None);

		// Act
		await Task.WhenAll(peer1.CloseAsync(), peer2.CloseAsync());

		// Assert
		(await receive1).Should().BeNull();
		(await receive2).Should().BeNull();
	}

	[TestMethod]
	public async Task InProcessTransport_Send_While_Closing_Should_Not_Hang()
	{
		// Arrange
		const int count = 2000;
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;

		var sender = Task.Run(async () =>
		{
			for (var i = 0; i < count; i++)
			{
				try
				{
					await peer1.SendAsync(Frame.Create((short)(i % short.MaxValue), "scope", "name", i), CancellationToken.None);
				}
				catch (InvalidOperationException)
				{
					break;
				}
			}
		});

		var receiver = Task.Run(async () =>
		{
			while (await peer2.ReceiveAsync(CancellationToken.None) is not null)
			{
			}
		});

		// Act
		await peer2.CloseAsync();
		await sender;
		await receiver;

		// Assert
		peer1.IsConnected.Should().BeFalse();
		peer2.IsConnected.Should().BeFalse();
	}

	[TestMethod]
	public async Task InProcessTransport_Cancelled_Receive_Should_Throw()
	{
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		using var cts = new CancellationTokenSource();

		// Act
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => peer2.ReceiveAsync(cts.Token));
		await peer1.CloseAsync();
		await peer2.CloseAsync();
	}

	[TestMethod]
	public async Task InProcessTransport_Send_After_Remote_Close_Should_Fail()
	{
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;
		await peer2.CloseAsync();

		// Act
		var act = () => peer1.SendAsync(Frame.Create(1, "scope", "name", 1), CancellationToken.None);

		// Assert
		await Assert.ThrowsExactlyAsync<InvalidOperationException>(act);
	}

	[TestMethod]
	public async Task InProcessTransport_Multiple_Receivers_Should_Complete_Independently()
	{
		// Arrange
		using var pair = FrameTransportPair.Create();
		var (peer1, peer2) = pair;

		var receive1 = peer2.ReceiveAsync(CancellationToken.None);
		var receive2 = peer2.ReceiveAsync(CancellationToken.None);

		// Act
		await peer1.SendAsync(Frame.Create(1, "scope", "name", 1), CancellationToken.None);
		await peer1.SendAsync(Frame.Create(2, "scope", "name", 2), CancellationToken.None);

		var result1 = await receive1;
		var result2 = await receive2;

		// Assert
		result1.Should().NotBeNull();
		result2.Should().NotBeNull();
	}
}
