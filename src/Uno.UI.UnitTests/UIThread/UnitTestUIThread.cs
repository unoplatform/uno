#nullable enable

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Uno.UI.Tests;

/// <summary>
/// The UI thread of the unit tests: the tests of <see cref="UITestClassAttribute"/> classes run on it, and work
/// dispatched from any other thread (timer ticks, finalizers) is queued to it, as it would be on a real host.
/// </summary>
internal static class UnitTestUIThread
{
	private static readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
	private static readonly Thread _thread;

	static UnitTestUIThread()
	{
		_thread = new Thread(Run)
		{
			IsBackground = true,
			Name = "Unit tests UI thread",
		};
		_thread.Start();
	}

	public static SynchronizationContext Context { get; } = new UIThreadSynchronizationContext();

	public static bool IsCurrent => Thread.CurrentThread == _thread;

	public static void Post(SendOrPostCallback callback, object? state) => _queue.Add((callback, state));

	public static SwitchToAwaitable SwitchTo() => default;

	private static void Run()
	{
		// All tests run under the same culture context.
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		SynchronizationContext.SetSynchronizationContext(Context);

		foreach (var (callback, state) in _queue.GetConsumingEnumerable())
		{
			try
			{
				callback(state);
			}
			catch (Exception error)
			{
				// An async void or a fire-and-forget continuation must not take the UI thread down.
				Console.Error.WriteLine($"Unhandled exception on the unit tests UI thread: {error}");
			}
		}
	}

	internal readonly struct SwitchToAwaitable : INotifyCompletion
	{
		public SwitchToAwaitable GetAwaiter() => this;

		public bool IsCompleted => IsCurrent;

		public void OnCompleted(Action continuation) => Post(static state => ((Action)state!)(), continuation);

		public void GetResult()
		{
		}
	}

	private sealed class UIThreadSynchronizationContext : SynchronizationContext
	{
		public override void Post(SendOrPostCallback d, object? state) => UnitTestUIThread.Post(d, state);

		public override void Send(SendOrPostCallback d, object? state)
		{
			if (IsCurrent)
			{
				d(state);
				return;
			}

			using ManualResetEventSlim done = new();
			ExceptionDispatchInfo? error = null;
			UnitTestUIThread.Post(_ =>
			{
				try
				{
					d(state);
				}
				catch (Exception e)
				{
					error = ExceptionDispatchInfo.Capture(e);
				}
				finally
				{
					done.Set();
				}
			}, null);
			done.Wait();
			error?.Throw();
		}

		public override SynchronizationContext CreateCopy() => this;
	}
}
