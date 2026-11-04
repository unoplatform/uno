#nullable enable

using System;
using System.Collections.Generic;
using Uno.HotReload.Tracking;
using Uno.UI.RemoteControl.Messaging.HotReload;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Host.HotReload;

/// <summary>
/// Reports the outcome of each hot-reload operation the dev-server applies from its own workspace, with the
/// event names of the outcomes the IDE reports (see <c>ServerHotReloadProcessor.Notify</c>) and <c>Source=DevServer</c>.
/// </summary>
internal static class HotReloadOutcomeTelemetry
{
	/// <returns>The subscription to the <paramref name="tracker"/>: dispose it to stop reporting.</returns>
	public static IDisposable Attach(HotReloadTracker tracker, ITelemetry telemetry)
	{
		EventHandler<HotReloadOperation> handler = (_, operation) => Track(telemetry, operation);
		tracker.OperationCompleted += handler;

		return new Subscription(tracker, handler);
	}

	private static void Track(ITelemetry telemetry, HotReloadOperation operation)
	{
		// An operation without files was started by an IDE notification, and the IDE reports its outcome (Source=IDE).
		if (operation.ConsideredFilePaths.IsEmpty
			|| operation.Result is not { } result
			|| GetOutcomeEvent(result) is not { } outcome)
		{
			return;
		}

		var properties = new Dictionary<string, string>
		{
			["Event"] = outcome.Event.ToString(),
			["Source"] = "DevServer",
			["Result"] = result.ToString(),
		};

		var measurements = new Dictionary<string, double>
		{
			["FileCount"] = operation.ConsideredFilePaths.Count,
		};
		if (operation.CompletionTime is { } completionTime)
		{
			measurements["DurationMs"] = (completionTime - operation.StartTime).TotalMilliseconds;
		}

		telemetry.TrackEvent(outcome.Name, properties, measurements);
	}

	private static (HotReloadEvent Event, string Name)? GetOutcomeEvent(HotReloadOperationResult result)
		=> result switch
		{
			HotReloadOperationResult.Success => (HotReloadEvent.Completed, "notify-completed"),
			HotReloadOperationResult.NoChanges => (HotReloadEvent.NoChanges, "notify-no-changes"),
			HotReloadOperationResult.RudeEdit => (HotReloadEvent.RudeEdit, "notify-rude-edit"),
			HotReloadOperationResult.Failed or HotReloadOperationResult.InternalError => (HotReloadEvent.Failed, "notify-failed"),
			_ => null, // Aborted: superseded by a newer operation, or no result in time.
		};

	private sealed class Subscription(HotReloadTracker tracker, EventHandler<HotReloadOperation> handler) : IDisposable
	{
		public void Dispose()
			=> tracker.OperationCompleted -= handler;
	}
}
