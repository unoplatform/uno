using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using Uno.UI.RemoteControl.VS.IdeChannel;
using VsLogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;

namespace Uno.UI.RemoteControl.DevServer.Tests.IDEChannel;

[TestClass]
public class Given_IdeChannelClient
{
	[TestInitialize]
	public void Initialize()
	{
		if (!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("The IDE channel client belongs to the Visual Studio extension and relies on a Windows named-pipe API.");
		}
	}

	private sealed class RecordingLogger : VsLogger
	{
		public ConcurrentQueue<(string Level, string Message)> Entries { get; } = new();

		public void Info(string message) => Entries.Enqueue(("Info", message));
		public void Debug(string message) => Entries.Enqueue(("Debug", message));
		public void Warn(string message) => Entries.Enqueue(("Warn", message));
		public void Error(string message) => Entries.Enqueue(("Error", message));
		public void Verbose(string message) => Entries.Enqueue(("Verbose", message));

		public string[] Messages(string level) => Entries.Where(e => e.Level == level).Select(e => e.Message).ToArray();
	}

	private static async Task<bool> CompletesWithin(Task? task, TimeSpan timeout)
	{
		if (task is null)
		{
			return false;
		}

#pragma warning disable VSTHRD003 // The task belongs to the caller, it is only observed here with a time bound.
		await Task.WhenAny(task, Task.Delay(timeout));
#pragma warning restore VSTHRD003
		return task.IsCompleted;
	}

	[TestMethod]
	public async Task WhenPipeNeverCreated_GivesUpAtDeadline()
	{
		var guid = Guid.NewGuid();
		var logger = new RecordingLogger();
		var client = new IdeChannelClient(guid, logger, TimeSpan.FromMilliseconds(500));
		var connected = 0;
		client.Connected += (_, _) => Interlocked.Increment(ref connected);

		try
		{
			var elapsed = Stopwatch.StartNew();
			client.ConnectToHost();

			(await CompletesWithin(client.ConnectTask, TimeSpan.FromSeconds(5))).Should().BeTrue();

			// It waited for the deadline, it did not give up at the first probe
			elapsed.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(400));
			connected.Should().Be(0);
			logger.Messages("Warn").Should().ContainSingle().Which.Should().Contain(guid.ToString());
			logger.Messages("Error").Should().BeEmpty();
		}
		finally
		{
			client.Dispose();
		}
	}

	[TestMethod]
	public async Task WhenDisposedWhilePipeMissing_StopsWaiting()
	{
		var logger = new RecordingLogger();
		var client = new IdeChannelClient(Guid.NewGuid(), logger);

		client.ConnectToHost();
		await Task.Delay(300);
		client.Dispose();

		(await CompletesWithin(client.ConnectTask, TimeSpan.FromSeconds(2))).Should().BeTrue();
		client.ConnectTask!.IsFaulted.Should().BeFalse();
		logger.Messages("Warn").Should().BeEmpty();
		logger.Messages("Error").Should().BeEmpty();
	}

	[TestMethod]
	public async Task WhenProbedRepeatedly_DoesNotConsumeSingleInstance()
	{
		var name = Guid.NewGuid().ToString();
		IdeChannelClient.PipeMayExist(Guid.NewGuid().ToString()).Should().BeFalse();

		using var server = new NamedPipeServerStream(
			name, PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
		var waiting = server.WaitForConnectionAsync();

		for (var i = 0; i < 50; i++)
		{
			IdeChannelClient.PipeMayExist(name).Should().BeTrue();
		}

		using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
		await client.ConnectAsync(2000);

		(await CompletesWithin(waiting, TimeSpan.FromSeconds(2))).Should().BeTrue();
		server.IsConnected.Should().BeTrue();
	}

	[TestMethod]
	public async Task WhenPipeAppearsLate_ConnectsOnce()
	{
		var guid = Guid.NewGuid();
		var logger = new RecordingLogger();
		var client = new IdeChannelClient(guid, logger);
		var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var connectedCount = 0;
		client.Connected += (_, _) =>
		{
			Interlocked.Increment(ref connectedCount);
			connected.TrySetResult(true);
		};

		NamedPipeServerStream? server = null;
		try
		{
			client.ConnectToHost();
			await Task.Delay(700);

			server = new NamedPipeServerStream(
				guid.ToString(), PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await server.WaitForConnectionAsync(timeout.Token);

			(await CompletesWithin(connected.Task, TimeSpan.FromSeconds(5))).Should().BeTrue();
			await Task.Delay(300);
			connectedCount.Should().Be(1);
			logger.Messages("Error").Should().BeEmpty();
		}
		finally
		{
			client.Dispose();
			if (server is not null)
			{
				await server.DisposeAsync();
			}
		}
	}
}
