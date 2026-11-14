#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Wayland;

// Same-MIME getters share a transfer; its lifetime ends when the last reader leaves or the offer is destroyed.
internal sealed class WaylandDataOfferReads(Func<string, CancellationToken, byte[]?> receive) : IDisposable
{
	private readonly Dictionary<string, Read> _reads = new();
	private readonly List<Read> _started = new();
	private bool _destroyed;

	internal async Task<byte[]?> ReadAsync(string mimeType, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Read read;
		lock (_reads)
		{
			if (_destroyed)
			{
				return null;
			}
			if (!_reads.TryGetValue(mimeType, out read!))
			{
				read = new Read();
				_reads.Add(mimeType, read);
				_started.Add(read);
				var transfer = read;
				read.Task = Task.Run(() => Receive(mimeType, transfer));
			}
			read.Readers++;
		}
		try
		{
			return await read.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			lock (_reads)
			{
				if (--read.Readers == 0 && !read.Completed)
				{
					read.Cancellation.Cancel();
					_reads.Remove(mimeType);
				}
			}
		}
	}

	internal async Task<bool> WhenReadsCompleted()
	{
		Task<byte[]?>[] reads;
		lock (_reads)
		{
			reads = _started.Select(read => read.Task).ToArray();
		}
		var results = await Task.WhenAll(reads).ConfigureAwait(false);
		return results.All(result => result is not null);
	}

	private byte[]? Receive(string mimeType, Read read)
	{
		try
		{
			return receive(mimeType, read.Cancellation.Token);
		}
		finally
		{
			lock (_reads)
			{
				read.Completed = true;
				read.Cancellation.Dispose();
			}
		}
	}

	public void Dispose()
	{
		lock (_reads)
		{
			_destroyed = true;
			foreach (var read in _started)
			{
				if (!read.Completed)
				{
					read.Cancellation.Cancel();
				}
			}
		}
	}

	private sealed class Read
	{
		internal readonly CancellationTokenSource Cancellation = new();
		internal Task<byte[]?> Task = null!;
		internal int Readers;
		internal bool Completed;
	}
}
