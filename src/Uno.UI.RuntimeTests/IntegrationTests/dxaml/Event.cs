using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Tests.Enterprise
{
	internal class Event
	{
		private readonly ManualResetEventSlim _event = new ManualResetEventSlim();

		internal int FiredCount { get; private set; }

		internal async Task<bool> WaitForDefault(int timeout = 5000, CancellationToken ct = default)
		{
			var h = _event.WaitHandle;
			RegisteredWaitHandle registration = default;
			try
			{
				if (h.WaitOne(0))
				{
					return true;
				}

				if (timeout == 0)
				{
					return false;
				}

				var tcs = new TaskCompletionSource<bool>();

				using var _ = ct.Register(() => tcs.TrySetCanceled());

				registration = ThreadPool
					.RegisterWaitForSingleObject(
						h,
						(_, __) =>
						{
							tcs.TrySetResult(true);
						},
						null,
						timeout,
						executeOnlyOnce: true);

				return await tcs.Task;
			}
			finally
			{
				registration?.Unregister(h);
			}
		}

		internal void Set()
		{
			FiredCount++;
			_event.Set();
		}

		internal bool HasFired() => _event.IsSet;

		public void Reset()
		{
			_event.Reset();
		}

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
