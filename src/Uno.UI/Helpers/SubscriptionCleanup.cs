#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Helpers;

/// <summary>
/// Removes event subscriptions that hold their subscriber weakly, once the subscriber is gone.
/// </summary>
internal static class SubscriptionCleanup
{
	// A collected page can release thousands of subscriptions at once: remove them in batches, at Low priority, so they
	// neither flood the dispatcher with one item each nor hold back rendering.
	private const int MaxRemovalsPerBatch = 256;

	private static readonly ConcurrentQueue<PendingRemoval> _pendingRemovals = new();
	private static int _isBatchScheduled;

	/// <summary>Runs <paramref name="unsubscribe"/>, logging rather than throwing if the event source fails.</summary>
	internal static void Run(Action unsubscribe, Type logOwner, string subscription)
	{
		try
		{
			unsubscribe();
		}
		catch (Exception e)
		{
			if (logOwner.Log().IsEnabled(LogLevel.Error))
			{
				logOwner.Log().Error($"Failed to remove {subscription}", e);
			}
		}
	}

	/// <summary>Runs <paramref name="unsubscribe"/>, logging rather than throwing if the event source fails.</summary>
	internal static void Run<TSource, TState>(Action<TSource, TState> unsubscribe, TSource source, TState state, Type logOwner, string subscription)
	{
		try
		{
			unsubscribe(source, state);
		}
		catch (Exception e)
		{
			if (logOwner.Log().IsEnabled(LogLevel.Error))
			{
				logOwner.Log().Error($"Failed to remove {subscription}", e);
			}
		}
	}

	/// <summary>
	/// Queues <paramref name="unsubscribe"/> on the UI thread, since an event source isn't required to be thread-safe.
	/// Never throws, so it can be called from a finalizer.
	/// </summary>
	internal static void EnqueueFromFinalizer(Action unsubscribe, Type logOwner, string subscription)
	{
		try
		{
			_pendingRemovals.Enqueue(new PendingRemoval(unsubscribe, logOwner, subscription));
			ScheduleBatch();
		}
		catch (Exception e)
		{
			// An exception escaping a finalizer terminates the process, even one thrown by the logger.
			try
			{
				if (logOwner.Log().IsEnabled(LogLevel.Error))
				{
					logOwner.Log().Error($"Failed to schedule the removal of {subscription}", e);
				}
			}
			catch
			{
			}
		}
	}

	private static void ScheduleBatch()
	{
		if (Interlocked.Exchange(ref _isBatchScheduled, 1) == 0)
		{
			try
			{
				NativeDispatcher.Main.Enqueue(RunBatch, NativeDispatcherPriority.Low);
			}
			catch
			{
				Volatile.Write(ref _isBatchScheduled, 0);
				throw;
			}
		}
	}

	private static void RunBatch()
	{
		// Cleared before dequeuing, so that a removal queued meanwhile schedules another batch instead of being missed.
		Volatile.Write(ref _isBatchScheduled, 0);

		for (var i = 0; i < MaxRemovalsPerBatch && _pendingRemovals.TryDequeue(out var removal); i++)
		{
			Run(removal.Unsubscribe, removal.LogOwner, removal.Subscription);
		}

		if (!_pendingRemovals.IsEmpty)
		{
			try
			{
				ScheduleBatch();
			}
			catch (Exception e)
			{
				if (typeof(SubscriptionCleanup).Log().IsEnabled(LogLevel.Error))
				{
					typeof(SubscriptionCleanup).Log().Error("Failed to schedule the next batch of subscription removals", e);
				}
			}
		}
	}

	private readonly record struct PendingRemoval(Action Unsubscribe, Type LogOwner, string Subscription);
}
