using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Tests.Enterprise
{
	internal class Event
	{
		internal int FiredCount { get; private set; }

		private TaskCompletionSource<bool> _tcs;

		internal async Task<bool> WaitForDefault(int timeout = 5000, CancellationToken ct = default)
		{
			var tcs = EnsureTcs();

			var timeoutTask = Task.Delay(timeout, ct);

			var winningTask = await Task.WhenAny(timeoutTask, tcs.Task);

			if (winningTask == timeoutTask)
			{
				return false;
			}

			return await tcs.Task;
		}

		private TaskCompletionSource<bool> EnsureTcs()
		{
			var newTcs = new TaskCompletionSource<bool>();
			var tcs = Interlocked.CompareExchange(ref _tcs, newTcs, null) ?? newTcs;
			return tcs;
		}

		internal void Set()
		{
			EnsureTcs().TrySetResult(true);
			FiredCount++;
		}

		internal bool HasFired()
		{
			var tcs = _tcs;
			return tcs != null && tcs.Task.IsCompleted;
		}

		public void Reset() => Interlocked.Exchange(ref _tcs, null)?.TrySetCanceled();

		public Task WaitFor(TimeSpan timeout, CancellationToken ct = default)
		{
			return WaitForDefault((int)timeout.TotalMilliseconds, ct);
		}

		internal Task WaitFor(TimeSpan timeout, bool enforceUnderDebugger, CancellationToken ct = default)
		{
			if (!enforceUnderDebugger && Debugger.IsAttached)
			{
				return Task.CompletedTask;
			}

			return WaitForDefault((int)timeout.TotalMilliseconds, ct);
		}
	}
}
