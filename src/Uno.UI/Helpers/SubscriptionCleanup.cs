#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Helpers;

/// <summary>
/// Removes event subscriptions that hold their subscriber weakly, once the subscriber is gone.
/// </summary>
internal static class SubscriptionCleanup
{
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

	/// <summary>
	/// Queues <paramref name="unsubscribe"/> on the UI thread, since an event source isn't required to be thread-safe.
	/// Never throws, so it can be called from a finalizer.
	/// </summary>
	internal static void EnqueueFromFinalizer(Action unsubscribe, Type logOwner, string subscription)
	{
		try
		{
			NativeDispatcher.Main.Enqueue(() => Run(unsubscribe, logOwner, subscription));
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
}
