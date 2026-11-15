#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Helpers;

/// <summary>A subscription whose removal from its source can be queued by <see cref="SubscriptionCleanup.EnqueueFromFinalizer"/>.</summary>
internal interface IQueuedRemoval
{
	Type LogOwner { get; }

	string Description { get; }

	/// <summary>Removes the subscription from <paramref name="source"/>. Must not throw.</summary>
	void Remove(object source);
}

/// <summary>
/// Removes event subscriptions that hold their subscriber weakly, once the subscriber is gone.
/// </summary>
internal static class SubscriptionCleanup
{
	// A collected page can release thousands of subscriptions at once: remove them in batches, at Low priority, so they
	// neither flood the dispatcher with one item each nor hold back rendering. A removal copies the source's whole
	// handler list, so a batch also stops after a few milliseconds on a source with thousands of handlers.
	private const int MaxRemovalsPerBatch = 256;
	private static readonly long MaxTicksPerBatch = Stopwatch.Frequency * 3 / 1000;

	private static readonly ConcurrentQueue<PendingRemoval> _pendingRemovals = new();
	private static int _isBatchScheduled;

	/// <summary>Runs <paramref name="unsubscribe"/>, logging rather than throwing if the event source fails.</summary>
	internal static void Run<TSource, TState>(Action<TSource, TState> unsubscribe, TSource source, TState state, Type logOwner, string subscription)
		where TSource : class
	{
		try
		{
			unsubscribe(source, state);
		}
		catch (Exception e)
		{
			if (logOwner.Log().IsEnabled(LogLevel.Error))
			{
				logOwner.Log().Error($"Failed to remove {subscription} from a {source.GetType()}", e);
			}
		}
	}

	/// <summary>
	/// Queues the removal of <paramref name="removal"/> from <paramref name="source"/> on the UI thread, since an event
	/// source isn't required to be thread-safe. Never throws, so it can be called from a finalizer, which can pass its
	/// own object rather than allocate a callback: the queue keeps it until the removal runs.
	/// </summary>
	internal static void EnqueueFromFinalizer(IQueuedRemoval removal, object source)
	{
		// An exception escaping a finalizer terminates the process, even one thrown by the logger.
		try
		{
			_pendingRemovals.Enqueue(new PendingRemoval(removal, source));
		}
		catch (Exception e)
		{
			try
			{
				if (removal.LogOwner.Log().IsEnabled(LogLevel.Error))
				{
					removal.LogOwner.Log().Error($"Failed to queue the removal of {removal.Description} from a {source.GetType()}", e);
				}
			}
			catch
			{
			}

			return;
		}

		try
		{
			ScheduleBatch();
		}
		catch (Exception e)
		{
			try
			{
				if (typeof(SubscriptionCleanup).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(SubscriptionCleanup).Log().Warn("Failed to schedule the queued subscription removals: they run with the next one scheduled", e);
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

		var removed = 0;
		var start = Stopwatch.GetTimestamp();
		try
		{
			while (removed < MaxRemovalsPerBatch
				&& Stopwatch.GetTimestamp() - start < MaxTicksPerBatch
				&& _pendingRemovals.TryDequeue(out var removal))
			{
				removal.Removal.Remove(removal.Source);
				removed++;
			}
		}
		finally
		{
			if (typeof(SubscriptionCleanup).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(SubscriptionCleanup).Log().Debug($"Removed {removed} subscriptions of collected subscribers, {_pendingRemovals.Count} still queued");
			}

			if (!_pendingRemovals.IsEmpty)
			{
				try
				{
					ScheduleBatch();
				}
				catch (Exception e)
				{
					if (typeof(SubscriptionCleanup).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(SubscriptionCleanup).Log().Warn("Failed to schedule the queued subscription removals: they run with the next one scheduled", e);
					}
				}
			}
		}
	}

	private readonly record struct PendingRemoval(IQueuedRemoval Removal, object Source);
}
