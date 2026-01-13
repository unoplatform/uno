using System;
using System.Collections.Immutable;
using System.Threading;

namespace Uno.UI.RemoteControl.Server.Processors.Helpers;

internal class BufferGate
{
	private int _holders;
	private ImmutableHashSet<Action> _onRelease = ImmutableHashSet<Action>.Empty;

	public IDisposable Acquire()
	{
		Interlocked.Increment(ref _holders);
		return new Holder(this);
	}

	public class Holder(BufferGate owner) : IDisposable
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
