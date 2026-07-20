using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.IO;

/// <summary>
/// Decorates an <see cref="IFileUpdater"/> with a gate driven by the hot-reload workspace lifecycle
/// (see <see cref="HotReloadWorkspaceState"/>): update requests received while the workspace is
/// expected but not ready yet are queued and applied — in arrival order — only once the baseline
/// solution has been captured, so an early edit can never be folded into the baseline (nor written
/// to disk before the file-system observer exists).
/// </summary>
/// <remarks>
/// <para>
/// Requests are failed with an explicit <see cref="IUpdateFileResponse.GlobalError"/> (and never
/// applied) when the workspace will not become available on this connection
/// (<see cref="HotReloadWorkspaceState.Failed"/> / <see cref="HotReloadWorkspaceState.Disposed"/>),
/// or when a queued request out-lives <see cref="QueueTimeout"/>.
/// </para>
/// <para>
/// When hot reload is IDE-driven (<see cref="HotReloadWorkspaceState.NoWorkspace"/>, e.g. Visual
/// Studio), requests pass through untouched.
/// </para>
/// </remarks>
public sealed class WorkspaceGatedFileUpdater(
	IFileUpdater inner,
	TimeSpan? queueTimeout = null,
	IReporter? reporter = null,
	Action<WorkspaceGateEvent>? onEvent = null) : IFileUpdater
{
	/// <summary>
	/// Default max delay a request is kept queued while waiting for the workspace to become ready.
	/// </summary>
	public static readonly TimeSpan DefaultQueueTimeout = TimeSpan.FromSeconds(30);

	private readonly object _gate = new();
	private readonly Queue<PendingUpdate> _queue = new();
	// Read on the drain task's thread, written from the message-processing thread via the Inner setter
	// (called on each ConfigureServer). volatile guarantees the drain loop sees the updated reference
	// promptly on weakly-ordered targets (ARM Skia: iOS, Android, M-series Mac).
	private volatile IFileUpdater _inner = inner ?? throw new ArgumentNullException(nameof(inner));
	private HotReloadWorkspaceState _state = HotReloadWorkspaceState.NotConfigured;
	private bool _draining;

	/// <summary>
	/// Max delay a request is kept queued while waiting for the workspace to become ready.
	/// On expiry the request is failed and is never applied afterwards.
	/// </summary>
	public TimeSpan QueueTimeout { get; } = queueTimeout ?? DefaultQueueTimeout;

	/// <summary>
	/// The decorated updater. Replaceable: the processor refines the editor as configuration
	/// arrives (e.g. switching to the IDE editor once Visual Studio is detected) without
	/// resetting the gate (queued requests and lifecycle state are preserved).
	/// </summary>
	public IFileUpdater Inner
	{
		get => _inner;
		set => _inner = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Current workspace lifecycle state driving the gate.
	/// </summary>
	public HotReloadWorkspaceState State
	{
		get
		{
			lock (_gate)
			{
				return _state;
			}
		}
	}

	/// <summary>
	/// Reports a workspace lifecycle transition. Terminal states
	/// (<see cref="HotReloadWorkspaceState.Failed"/>, <see cref="HotReloadWorkspaceState.Disposed"/>)
	/// fail all queued requests; pass-through states (<see cref="HotReloadWorkspaceState.Ready"/>,
	/// <see cref="HotReloadWorkspaceState.NoWorkspace"/>) flush them in arrival order.
	/// </summary>
	public void ReportWorkspaceState(HotReloadWorkspaceState state)
	{
		List<PendingUpdate>? toFail = null;
		var drain = false;

		lock (_gate)
		{
			if (_state == state
				|| _state is HotReloadWorkspaceState.Disposed
				|| (_state is HotReloadWorkspaceState.Failed && state is not HotReloadWorkspaceState.Disposed))
			{
				return; // Terminal states are sticky for the lifetime of the connection.
			}

			_state = state;

			switch (state)
			{
				case HotReloadWorkspaceState.Ready:
				case HotReloadWorkspaceState.NoWorkspace:
					if (!_draining && _queue.Count > 0)
					{
						_draining = drain = true;
					}
					break;

				case HotReloadWorkspaceState.Failed:
				case HotReloadWorkspaceState.Disposed:
					if (_queue.Count > 0)
					{
						toFail = [.. _queue];
						_queue.Clear();
					}
					break;
			}
		}

		if (toFail is not null)
		{
			var error = GetTerminalError(state);
			foreach (var entry in toFail)
			{
				if (entry.TryTake())
				{
					// Resolve the awaiter before any external callback: a throwing reporter/onEvent must
					// neither orphan this entry's TaskCompletionSource nor abort the loop over the rest.
					entry.Complete(Reject(entry.Request, error));
					try { reporter?.Warn($"Hot-reload update request '{entry.Request.RequestId}' rejected after {entry.Age.Elapsed.TotalSeconds:F1}s in queue: {error}"); } catch { /* diagnostics are best-effort */ }
					try { onEvent?.Invoke(new WorkspaceGateEvent("rejected", 0, entry.Age.Elapsed)); } catch { /* diagnostics are best-effort */ }
				}
			}
		}

		if (drain)
		{
			_ = DrainAsync();
		}
	}

	/// <inheritdoc />
	public Task<IUpdateFileResponse> UpdateAsync(IUpdateFileRequest request, CancellationToken ct)
	{
		PendingUpdate entry;
		int queueLength;

		lock (_gate)
		{
			switch (_state)
			{
				case HotReloadWorkspaceState.Failed:
				case HotReloadWorkspaceState.Disposed:
					return Task.FromResult(Reject(request, GetTerminalError(_state)));

				case HotReloadWorkspaceState.Ready:
				case HotReloadWorkspaceState.NoWorkspace:
					if (!_draining && _queue.Count is 0)
					{
						break; // Pass-through, invoked below outside the lock.
					}
					goto default; // A flush is in progress: queue behind it to preserve ordering.

				default:
					entry = new PendingUpdate(request, ct);
					_queue.Enqueue(entry);
					queueLength = _queue.Count;

					// Schedule the TTL outside the state machine: whichever of flush /
					// terminal-rejection / timeout / cancellation takes the entry first wins.
					_ = WatchTimeoutAsync(entry);

					// Guard the diagnostics: a throwing reporter/onEvent must not leave the just-enqueued
					// entry orphaned (the caller would see the exception instead of the awaiter).
					try { reporter?.Verbose($"Hot-reload workspace not ready ({_state}), queuing update request '{request.RequestId}' ({queueLength} pending)."); } catch { /* diagnostics are best-effort */ }
					try { onEvent?.Invoke(new WorkspaceGateEvent("queued", queueLength)); } catch { /* diagnostics are best-effort */ }

					return entry.Task;
			}
		}

		return _inner.UpdateAsync(request, ct);
	}

	private async Task DrainAsync()
	{
		// Launched fire-and-forget: an unexpected escape must never leave _draining stuck true (which
		// would permanently divert every future Ready request into the queue with no drain to flush it),
		// and the error must be logged rather than silently dropped into TaskScheduler.
		try
		{
			while (true)
			{
				PendingUpdate entry;
				lock (_gate)
				{
					// Terminal transitions clear the queue themselves; only stop on empty.
					if (_queue.Count is 0)
					{
						return; // _draining is reset in the finally below.
					}

					entry = _queue.Dequeue();
				}

				if (!entry.TryTake())
				{
					continue; // Already expired or cancelled while queued.
				}

				IUpdateFileResponse response;
				try
				{
					response = await _inner.UpdateAsync(entry.Request, entry.Ct).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					response = Reject(entry.Request, ex.Message);
				}

				// Resolve the awaiter before any external callback: a throwing reporter/onEvent must
				// neither orphan this entry's TaskCompletionSource nor escape the loop.
				entry.Complete(response);
				try { reporter?.Verbose($"Hot-reload workspace ready, applied queued update request '{entry.Request.RequestId}' (waited {entry.Age.Elapsed.TotalSeconds:F1}s)."); } catch { /* diagnostics are best-effort */ }
				try { onEvent?.Invoke(new WorkspaceGateEvent("flushed", GetQueueLength(), entry.Age.Elapsed)); } catch { /* diagnostics are best-effort */ }
			}
		}
		catch (Exception ex)
		{
			try { reporter?.Error($"Hot-reload queue drain faulted unexpectedly and was aborted: {ex}"); } catch { /* diagnostics are best-effort */ }
		}
		finally
		{
			lock (_gate)
			{
				_draining = false;
			}
		}
	}

	private async Task WatchTimeoutAsync(PendingUpdate entry)
	{
		// Fire-and-forget: any escape here would silently orphan the caller's awaiter, so every path
		// must resolve the entry. A claimed entry is then physically dropped from the queue (see finally).
		var taken = false;
		try
		{
			try
			{
				await Task.Delay(QueueTimeout, entry.Ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				taken = entry.TryTake();
				if (taken)
				{
					entry.Complete(Reject(entry.Request, "The update request has been cancelled while waiting for the hot-reload workspace to initialize."));
				}
				return;
			}

			taken = entry.TryTake();
			if (taken)
			{
				var error = $"The hot-reload workspace was not ready within {QueueTimeout.TotalSeconds:F0}s; the request was not applied.";
				// Resolve the awaiter before any external callback so a throwing reporter/onEvent can't
				// orphan the request.
				entry.Complete(Reject(entry.Request, error));
				try { reporter?.Warn($"Hot-reload update request '{entry.Request.RequestId}' expired: {error}"); } catch { /* diagnostics are best-effort */ }
				try { onEvent?.Invoke(new WorkspaceGateEvent("expired", GetQueueLength(), entry.Age.Elapsed)); } catch { /* diagnostics are best-effort */ }
			}
		}
		catch (Exception ex)
		{
			// Unexpected fault (e.g. a throwing dependency): still guarantee the awaiter is resolved.
			taken = entry.TryTake();
			if (taken)
			{
				entry.Complete(Reject(entry.Request, ex.Message));
			}
		}
		finally
		{
			if (taken)
			{
				CompactQueue();
			}
		}
	}

	/// <summary>
	/// Removes entries already claimed (by timeout / cancellation) from the queue, preserving FIFO order
	/// of the survivors. Claimed entries are otherwise only discarded lazily on the next flush; without
	/// this a workspace that never reaches a flushing or terminal state (stuck <c>Initializing</c>) would
	/// let dead entries — and the queue-length telemetry — grow unbounded.
	/// </summary>
	private void CompactQueue()
	{
		lock (_gate)
		{
			if (_queue.Count is 0)
			{
				return;
			}

			var live = _queue.Where(static entry => !entry.IsTaken).ToArray();
			if (live.Length == _queue.Count)
			{
				return; // Nothing claimed — avoid the rebuild.
			}

			_queue.Clear();
			foreach (var entry in live)
			{
				_queue.Enqueue(entry);
			}
		}
	}

	private int GetQueueLength()
	{
		lock (_gate)
		{
			return _queue.Count;
		}
	}

	private static string GetTerminalError(HotReloadWorkspaceState state)
		=> state is HotReloadWorkspaceState.Disposed
			? "The hot-reload connection has been closed; the request was not applied."
			: "The hot-reload workspace failed to initialize and will not recover on this connection; the request was not applied. Reconnect to retry.";

	private static IUpdateFileResponse Reject(IUpdateFileRequest request, string error)
		// Per-edit results are populated (not just the global error) so clients that scan
		// Results do not mistake the rejection for an empty "no changes" success.
		=> new GatedUpdateResponse(
			request.RequestId,
			error,
			[.. request.Edits.Select(edit => new FileEditResult(edit.FilePath, FileUpdateResult.NotAvailable, error))]);

	private sealed class PendingUpdate(IUpdateFileRequest request, CancellationToken ct)
	{
		private readonly TaskCompletionSource<IUpdateFileResponse> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _taken;

		public IUpdateFileRequest Request => request;

		public CancellationToken Ct => ct;

		public Stopwatch Age { get; } = Stopwatch.StartNew();

		public Task<IUpdateFileResponse> Task => _result.Task;

		/// <summary>
		/// Claims the entry for completion. Exactly one of flush / timeout / cancellation /
		/// terminal-rejection wins; an expired entry left in the queue is skipped by the flush.
		/// </summary>
		public bool TryTake()
			=> Interlocked.CompareExchange(ref _taken, 1, 0) is 0;

		/// <summary>
		/// Whether the entry has already been claimed (by flush / timeout / cancellation / terminal
		/// rejection). Used to compact claimed-but-still-queued entries out of the queue.
		/// </summary>
		public bool IsTaken => Volatile.Read(ref _taken) is 1;

		public void Complete(IUpdateFileResponse response)
			=> _result.TrySetResult(response);
	}

	private sealed record GatedUpdateResponse(
		string RequestId,
		string? GlobalError,
		ImmutableArray<FileEditResult> Results,
		long? HotReloadCorrelationId = null) : IUpdateFileResponse;
}

/// <summary>
/// Diagnostic event raised by <see cref="WorkspaceGatedFileUpdater"/> when a request is
/// queued, flushed, expired or rejected — intended for telemetry relaying.
/// </summary>
/// <param name="Kind">One of <c>queued</c>, <c>flushed</c>, <c>expired</c>, <c>rejected</c>.</param>
/// <param name="QueueLength">Number of requests still pending after this event.</param>
/// <param name="WaitDuration">Time the request spent in the queue, when applicable.</param>
public sealed record WorkspaceGateEvent(string Kind, int QueueLength, TimeSpan? WaitDuration = null);
