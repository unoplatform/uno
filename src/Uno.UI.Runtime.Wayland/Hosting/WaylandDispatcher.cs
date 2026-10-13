#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Helpers;

namespace Uno.UI.Runtime.Wayland;

// The UI loop. Stop is quiescent: callbacks never run afterwards, and its task completes once the loop thread exits.
internal sealed class WaylandDispatcher
{
	private readonly object _gate = new();
	private readonly EventLoop _loop;
	private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private bool _started;
	private volatile bool _stopped;

	internal WaylandDispatcher()
	{
		_loop = new EventLoop(run => new Thread(() =>
		{
			try
			{
				run();
			}
			finally
			{
				_exited.TrySetResult();
			}
		})
		{ IsBackground = true });
	}

	internal void Schedule(Action action)
	{
		lock (_gate)
		{
			if (_stopped)
			{
				return;
			}
			_started = true;
			// EventLoop still runs the rest of a batch it took before being disposed.
			_loop.Schedule(() =>
			{
				if (!_stopped)
				{
					action();
				}
			});
		}
	}

	internal Task Stop()
	{
		lock (_gate)
		{
			_stopped = true;
			_loop.Dispose();
			if (!_started)
			{
				_exited.TrySetResult();
			}
			return _exited.Task;
		}
	}
}
