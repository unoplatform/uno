#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Uno.UI.Runtime.Wayland.Native;

// Bounded pipe transfers for wl_data_offer.receive and wl_data_source.send. Callers run these on worker
// threads, never on the dispatch or UI thread. Both directions close the descriptor they are given.
internal static unsafe class WaylandPipe
{
	private const int OCloexec = 0x80000;
	private const int ONonblock = 0x800;
	internal const int MaximumTransferSize = 64 * 1024 * 1024;
	private static readonly TimeSpan _maximumTransferDuration = TimeSpan.FromMinutes(2);
	private const short PollIn = 1;
	private const short PollOut = 4;
	private const int EIntr = 4;
	private const int EAgain = 11;

	internal static (int Read, int Write) Create()
	{
		int* fds = stackalloc int[2];
		if (Posix.pipe2(fds, OCloexec) != 0)
		{
			throw new IOException($"pipe2 failed: {Marshal.GetLastPInvokeError()}");
		}
		return (fds[0], fds[1]);
	}

	/// <summary>
	/// Reads until end of file. Returns null when the size limit is exceeded or the writer makes no progress
	/// for <paramref name="timeout"/>, the total deadline expires, or the caller cancels the transfer.
	/// </summary>
	internal static byte[]? ReadToEnd(int fd, TimeSpan timeout, int maximumSize = MaximumTransferSize, CancellationToken cancellationToken = default, TimeSpan? totalTimeout = null)
	{
		try
		{
			SetNonblocking(fd);
			// Progress resets the idle timeout, but never the two-minute total deadline: trickling peers cannot pin a worker and fd.
			var total = Stopwatch.StartNew();
			var deadline = totalTimeout is { } requested && requested < _maximumTransferDuration ? requested : _maximumTransferDuration;
			var idle = Stopwatch.StartNew();
			using var buffer = new MemoryStream();
			var chunk = new byte[64 * 1024];
			while (true)
			{
				if (!Wait(fd, PollIn, Remaining(timeout - idle.Elapsed, deadline - total.Elapsed), cancellationToken))
				{
					return null;
				}
				nint count;
				fixed (byte* pointer = chunk)
				{
					count = Posix.read(fd, pointer, (nuint)chunk.Length);
				}
				if (count == 0)
				{
					return buffer.ToArray();
				}
				if (count < 0)
				{
					var error = Marshal.GetLastPInvokeError();
					if (error is EIntr or EAgain)
					{
						continue;
					}
					throw new IOException($"read failed: {error}");
				}
				if (count > maximumSize - buffer.Length)
				{
					return null;
				}
				buffer.Write(chunk, 0, (int)count);
				idle.Restart();
			}
		}
		finally
		{
			_ = Posix.close(fd);
		}
	}

	/// <summary>Writes all of <paramref name="data"/>. Returns false when the reader went away or stopped reading for <paramref name="timeout"/>, the total deadline expired, or the caller cancelled.</summary>
	internal static bool WriteAll(int fd, ReadOnlySpan<byte> data, TimeSpan timeout, CancellationToken cancellationToken = default, TimeSpan? totalTimeout = null)
	{
		try
		{
			SetNonblocking(fd);
			// Progress resets the idle timeout, but never the two-minute total deadline: trickling peers cannot pin a worker and fd.
			var total = Stopwatch.StartNew();
			var deadline = totalTimeout is { } requested && requested < _maximumTransferDuration ? requested : _maximumTransferDuration;
			var idle = Stopwatch.StartNew();
			if (data.Length > MaximumTransferSize)
			{
				return false;
			}
			fixed (byte* start = data)
			{
				var written = 0;
				while (written < data.Length)
				{
					if (!Wait(fd, PollOut, Remaining(timeout - idle.Elapsed, deadline - total.Elapsed), cancellationToken))
					{
						return false;
					}
					var count = Posix.write(fd, start + written, (nuint)(data.Length - written));
					if (count < 0)
					{
						var error = Marshal.GetLastPInvokeError();
						if (error is EIntr or EAgain)
						{
							continue;
						}
						// EPIPE: the reader closed its end. The runtime ignores SIGPIPE.
						return false;
					}
					written += (int)count;
					if (count > 0)
					{
						idle.Restart();
					}
				}
			}
			return true;
		}
		finally
		{
			_ = Posix.close(fd);
		}
	}

	private static TimeSpan Remaining(TimeSpan idle, TimeSpan total) => idle < total ? idle : total;

	private static void SetNonblocking(int fd)
	{
		var flags = Posix.fcntl(fd, 3, 0);
		if (flags < 0 || Posix.fcntl(fd, 4, flags | ONonblock) < 0)
		{
			throw new IOException($"fcntl failed: {Marshal.GetLastPInvokeError()}");
		}
	}

	private static bool Wait(int fd, short events, TimeSpan timeout, CancellationToken cancellationToken = default)
	{
		var elapsed = Stopwatch.StartNew();
		var pollFd = new Posix.PollFd { Fd = fd, Events = events };
		while (true)
		{
			var remaining = timeout - elapsed.Elapsed;
			if (remaining <= TimeSpan.Zero || cancellationToken.IsCancellationRequested)
			{
				return false;
			}
			var result = Posix.poll(&pollFd, 1, (int)Math.Min(100, Math.Ceiling(remaining.TotalMilliseconds)));
			if (result > 0)
			{
				// Hang-up and error states are reported by the following read() or write().
				return true;
			}
			if (result < 0 && Marshal.GetLastPInvokeError() != EIntr)
			{
				throw new IOException($"poll failed: {Marshal.GetLastPInvokeError()}");
			}
		}
	}
}
