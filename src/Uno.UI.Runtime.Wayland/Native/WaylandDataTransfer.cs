#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Wayland.Native;

internal static class WaylandDataTransfer
{
	// Owns fd from entry, including while a delayed provider is producing its data.
	internal static async Task<bool> SendAsync(int fd, Task<byte[]?> production, TimeSpan timeout, CancellationToken cancellationToken = default)
	{
		var ownedFd = fd;
		try
		{
			var bytes = await production.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			var writing = Task.Run(() => WaylandPipe.WriteAll(fd, bytes ?? [], timeout, cancellationToken));
			ownedFd = -1;
			return await writing.ConfigureAwait(false);
		}
		finally
		{
			if (ownedFd >= 0)
			{
				_ = Posix.close(ownedFd);
			}
		}
	}
}
