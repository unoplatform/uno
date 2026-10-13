#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Wayland.Rendering;

// Suspend managed dispatch only after its prepared read has been resolved. Native EGL readers remain free.
internal sealed class WaylandPresentationCoordinator(object syncRoot, Action<Action, Action> post, Action resume)
{
	private readonly SemaphoreSlim _gate = new(1);
	internal bool IsActive { get; private set; }

	internal bool Submit(Func<bool> submit)
	{
		_gate.Wait();
		try
		{
			var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			post(() =>
			{
				IsActive = true;
				ready.TrySetResult();
			}, () => ready.TrySetException(new IOException("Wayland dispatch stopped before presentation.")));
			ready.Task.GetAwaiter().GetResult();
			return submit();
		}
		finally
		{
			lock (syncRoot)
			{
				IsActive = false;
				resume();
			}
			_gate.Release();
		}
	}
}
