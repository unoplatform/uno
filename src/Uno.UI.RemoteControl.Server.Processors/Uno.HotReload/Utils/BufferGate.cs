using System;
using System.Collections.Immutable;
using System.Threading;

namespace Uno.HotReload.Utils;

/// <summary>
/// Provides a gate mechanism that buffers actions while held and executes them when all holders are released.
/// </summary>
/// <remarks>BufferGate is intended for scenarios where actions should be deferred until a resource or state is no
/// longer in use. While the gate is held, actions passed to RunOrPlan are queued and executed only after all holders
/// have been disposed. This class is not thread-safe for concurrent acquisition and release by multiple threads unless
/// external synchronization is used.</remarks>
internal class BufferGate
{
	private int _holders;
	private ImmutableHashSet<Action> _onRelease = ImmutableHashSet<Action>.Empty;

	public IDisposable Acquire()
	{
		Interlocked.Increment(ref _holders);
		return new Holder(this);
	}

	private class Holder(BufferGate owner) : IDisposable
	{
		private int _disposed;
		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) is 0
				&& Interlocked.Decrement(ref owner._holders) is 0)
			{
				owner.ProcessCallbacks();
			}
		}
	}

	public void RunOrPlan(Action action)
	{
		if (_holders is 0)
		{
			action();
		}
		else
		{
			ImmutableInterlocked.Update(ref _onRelease, static (set, action) => set.Add(action), action);
			if (_holders is 0) // The gate has been released while we were adding the callback, process it now
			{
				ProcessCallbacks();
			}
		}
	}

	private void ProcessCallbacks()
	{
		foreach (var callback in Interlocked.Exchange(ref _onRelease, ImmutableHashSet<Action>.Empty))
		{
			try
			{
				callback();
			}
			catch (Exception) { }
		}
	}
}
