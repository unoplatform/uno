#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Wayland.Rendering;

// Single owner thread for one window's renderer, frame scheduler and graphics context.
internal sealed class WaylandRenderThread : IDisposable
{
	private readonly BlockingCollection<Action> _queue = new();
	private readonly Thread _thread;
	private readonly object _gate = new();
	private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private bool _completed;

	internal Task Completion => _completion.Task;
	private readonly Action<Exception> _onError;

	public WaylandRenderThread(string name, Action<Exception> onError)
	{
		_onError = onError;
		_thread = new Thread(Run) { IsBackground = true, Name = name, Priority = ThreadPriority.AboveNormal };
		_thread.Start();
	}

	public void Post(Action action)
	{
		lock (_gate)
		{
			if (!_completed)
			{
				_queue.Add(action);
			}
		}
	}

	private void Run()
	{
		try
		{
			foreach (var action in _queue.GetConsumingEnumerable())
			{
				try
				{
					action();
				}
				catch (Exception error)
				{
					_onError(error);
				}
			}
		}
		catch (Exception error)
		{
			_completion.TrySetException(error);
		}
		finally
		{
			lock (_gate)
			{
				_completed = true;
				_queue.Dispose();
			}
			_completion.TrySetResult();
		}
	}

	internal void Complete(Action cleanup)
	{
		lock (_gate)
		{
			if (_completed)
			{
				return;
			}
			_queue.Add(() =>
			{
				try
				{
					cleanup();
				}
				catch (Exception error)
				{
					_completion.TrySetException(error);
					throw;
				}
			});
			_completed = true;
			_queue.CompleteAdding();
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (!_completed)
			{
				_completed = true;
				_queue.CompleteAdding();
			}
		}
	}
}
