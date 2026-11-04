#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.HotReload.Tracking;
using Uno.UI.RemoteControl.Messaging.HotReload;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Host.HotReload;

/// <summary>
/// Reports the outcome of each hot-reload operation the dev-server applies from its own workspace, with the
/// event names of the outcomes the IDE reports (<see cref="HotReloadOutcomeEvents"/>) and <c>Source=DevServer</c>.
/// </summary>
internal sealed class HotReloadOutcomeTelemetry(HotReloadTracker tracker, ITelemetry telemetry)
{
	private static readonly object _marker = new();

	private readonly ConditionalWeakTable<HotReloadOperation, object> _reportedByIde = new();

	/// <summary>
	/// Reports the outcomes of the operations that complete until <paramref name="workspaceToken"/> is cancelled:
	/// a pass that ends with its workspace (shutdown, hot reload disabled) is not an outcome the user saw.
	/// </summary>
	public void Attach(CancellationToken workspaceToken)
	{
		EventHandler<HotReloadOperation> handler = (_, operation) =>
		{
			if (!workspaceToken.IsCancellationRequested && !_reportedByIde.TryGetValue(operation, out _))
			{
				Track(operation);
			}
		};

		tracker.OperationCompleted += handler;
		workspaceToken.Register(() => tracker.OperationCompleted -= handler);
	}

	/// <summary>
	/// Leaves out an operation an IDE notification starts or completes: the IDE reports its outcome itself (Source=IDE).
	/// </summary>
	public void SkipReportedByIde(HotReloadOperation operation)
		=> _reportedByIde.AddOrUpdate(operation, _marker);

	private void Track(HotReloadOperation operation)
	{
		// Only the files the solution knows: a batch of files outside it (a log, .DS_Store) is not a hot reload.
		var fileCount = operation.ConsideredFilePaths.Except(operation.IgnoredFilePaths).Count;
		if (fileCount is 0 || GetOutcome(operation) is not { } outcome)
		{
			return;
		}

		var properties = new Dictionary<string, string>
		{
			["Event"] = outcome.Event.ToString(),
			["Source"] = HotReloadEventSource.DevServer.ToString(),
			["Result"] = outcome.Result,
		};

		var measurements = new Dictionary<string, double>
		{
			["FileCount"] = fileCount,
		};
		if (operation.CompletionTime is { } completionTime)
		{
			measurements["DurationMs"] = (completionTime - operation.StartTime).TotalMilliseconds;
		}

		telemetry.TrackEvent(outcome.Name, properties, measurements);
	}

	private static (HotReloadEvent Event, string Name, string Result)? GetOutcome(HotReloadOperation operation)
		=> operation.Result switch
		{
			HotReloadOperationResult.Success => (HotReloadEvent.Completed, HotReloadOutcomeEvents.Completed, "Success"),
			HotReloadOperationResult.NoChanges => (HotReloadEvent.NoChanges, HotReloadOutcomeEvents.NoChanges, "NoChanges"),
			HotReloadOperationResult.RudeEdit => (HotReloadEvent.RudeEdit, HotReloadOutcomeEvents.RudeEdit, "RudeEdit"),
			HotReloadOperationResult.Failed => (HotReloadEvent.Failed, HotReloadOutcomeEvents.Failed, "Failed"),
			HotReloadOperationResult.InternalError => (HotReloadEvent.Failed, HotReloadOutcomeEvents.Failed, "InternalError"),
			HotReloadOperationResult.Aborted when operation.IsTimedOut => (HotReloadEvent.Failed, HotReloadOutcomeEvents.Failed, "Timeout"),
			_ => null, // Aborted by a newer operation: that one carries the outcome.
		};
}
