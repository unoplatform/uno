using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Messaging;

/// <summary>
/// In-process implementation of <see cref="IFrameTransport"/> backed by in-memory queues.
/// </summary>
internal sealed class InProcessFrameTransport : IFrameTransport
{
	private sealed class Endpoint
	{
		private readonly ConcurrentQueue<Frame> _queue = new();
		private readonly SemaphoreSlim _signal = new(0, int.MaxValue);
		private int _isClosed;
		private int _isRemoteClosed;
		private Endpoint? _remote;

		public bool IsConnected => _isClosed == 0 && _isRemoteClosed == 0;

		public void AttachRemote(Endpoint remote) => _remote = remote;

		public Task SendAsync(Frame frame, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();

			if (_isClosed != 0)
			{
				throw new ObjectDisposedException(nameof(InProcessFrameTransport));
			}

			var remote = _remote ?? throw new InvalidOperationException("Transport endpoint is not connected.");
			if (remote._isClosed != 0)
			{
				throw new InvalidOperationException("Remote transport is closed.");
			}

			remote.Enqueue(frame);
			return Task.CompletedTask;
		}

		public async Task<Frame?> ReceiveAsync(CancellationToken ct)
		{
			while (true)
			{
				if (_queue.TryDequeue(out var frame))
				{
					return frame;
				}

				if (_isRemoteClosed != 0 || _isClosed != 0)
				{
					return null; // Closed, return null.
				}

				await _signal.WaitAsync(ct);
			}
		}

		public Task CloseAsync()
		{
			if (Interlocked.Exchange(ref _isClosed, 1) == 1)
			{
				return Task.CompletedTask;
			}

			Signal();
			_remote?.MarkRemoteClosed();

			return Task.CompletedTask;
		}

		private void MarkRemoteClosed()
		{
			if (Interlocked.Exchange(ref _isRemoteClosed, 1) == 1)
			{
				return;
			}

			Signal();
		}

		private void Enqueue(Frame frame)
		{
			_queue.Enqueue(frame);
			Signal();
		}

		private void Signal()
		{
			try
			{
				_signal.Release();
			}
			catch (SemaphoreFullException)
			{
				// Ignore spurious releases.
			}
		}
	}

	private readonly Endpoint _endpoint;

	private InProcessFrameTransport(Endpoint endpoint)
	{
		_endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
	}

	/// <inheritdoc />
	public bool IsConnected => _endpoint.IsConnected;

	/// <inheritdoc />
	public Task<Frame?> ReceiveAsync(CancellationToken ct) => _endpoint.ReceiveAsync(ct);

	/// <inheritdoc />
	public Task SendAsync(Frame frame, CancellationToken ct) => _endpoint.SendAsync(frame, ct);

	/// <inheritdoc />
	public Task CloseAsync() => _endpoint.CloseAsync();

	/// <inheritdoc />
	public void Dispose() => _ = CloseAsync();

	internal static (IFrameTransport Peer1, IFrameTransport Peer2) CreatePair()
	{
		var peer1 = new Endpoint();
		var peer2 = new Endpoint();
		peer1.AttachRemote(peer2);
		peer2.AttachRemote(peer1);

		return (new InProcessFrameTransport(peer1), new InProcessFrameTransport(peer2));
	}
}
