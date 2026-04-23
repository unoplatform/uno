#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uno.Diagnostics.UI;
using Uno.Foundation.Logging;

#if HAS_UNO_WINUI
using Uno.UI.RemoteControl.HotReload.Messages;
#else
using HotReloadServerOperationData = object;
#pragma warning disable CS0649 // Field _serverState is not used on windows, we need to keep it to default
#endif

namespace Uno.UI.RemoteControl.HotReload;

public partial class ClientHotReloadProcessor
{
	/// <summary>
	/// Raised when the status of the hot-reload engine changes.
	/// </summary>
	public EventHandler<Status>? StatusChanged;

	/// <summary>
	/// The current status of the hot-reload engine.
	/// </summary>
	public Status CurrentStatus => _status.Current;

	private readonly StatusSink _status;

	public enum HotReloadSource
	{
		Runtime,
		DevServer,
		Manual
	}

	public enum HotReloadClientResult
	{
		/// <summary>
		/// Successful hot-reload.
		/// </summary>
		Success = 1,

		/// <summary>
		/// Changes cannot be applied in local app due to handler errors.
		/// </summary>
		Failed = 2,

		/// <summary>
		/// The changes have been ignored.
		/// </summary>
		Ignored = 256,
	}

	/// <summary>
	/// The aggregated status of the hot-reload engine.
	/// </summary>
	/// <param name="State">The global state of the hot-reload engine (combining server and client state).</param>
	/// <param name="Server">State and history of all hot-reload operations detected on the server.</param>
	/// <param name="Local">State and history of all hot-reload operation received by this client.</param>
	public record Status(
		HotReloadState State,
		(HotReloadState State, IImmutableList<HotReloadServerOperationData> Operations) Server,
		(HotReloadState State, IImmutableList<HotReloadClientOperation> Operations) Local);

	private class StatusSink(ClientHotReloadProcessor owner)
	{
		private HotReloadState? _serverState;
		private bool _isFinalServerState;
		private ImmutableDictionary<long, HotReloadServerOperationData> _serverOperations = ImmutableDictionary<long, HotReloadServerOperationData>.Empty;
		private ImmutableList<HotReloadClientOperation> _localOperations = ImmutableList<HotReloadClientOperation>.Empty;
		private HotReloadSource _source;

		public Status Current { get; private set; } = null!;

		public void ReportLocallyDisabledState(string reason)
		{
			_serverState = HotReloadState.Disabled;
			_isFinalServerState = true;
		}

		public void ReportServerState(HotReloadState state)
		{
			if (_isFinalServerState)
			{
				return;
			}

			_serverState = state;
			NotifyStatusChanged();
		}

#if HAS_UNO_WINUI
		public void ReportServerStatus(HotReloadStatusMessage status)
		{
			if (!_isFinalServerState)
			{
				_serverState = status.State; // Do not override the state if it has already been set (debugger attached with dev-server)
			}
			ImmutableInterlocked.Update(ref _serverOperations, UpdateOperations, status.Operations);

			// For tooling purposes, we dump the diagnostics in the log of the application.
			foreach (var serverOp in status.Operations)
			{
				owner.ReportDiagnostics(serverOp.Diagnostics);
			}

			NotifyStatusChanged();

			static ImmutableDictionary<long, HotReloadServerOperationData> UpdateOperations(ImmutableDictionary<long, HotReloadServerOperationData> history, IImmutableList<HotReloadServerOperationData> udpated)
			{
				var updatedHistory = history.ToBuilder();
				foreach (var op in udpated)
				{
					updatedHistory[op.Id] = op;
				}

				return updatedHistory.ToImmutable();
			}
		}
#endif

		public void ConfigureSourceForNextOperation(HotReloadSource source)
			=> _source = source;

		public HotReloadClientOperation StartLocal(Type[] types)
		{
			var op = new HotReloadClientOperation(_source, types, NotifyStatusChanged);
			ImmutableInterlocked.Update(ref _localOperations, static (history, op) => history.Add(op).Sort(CompareBySequenceId), op);
			NotifyStatusChanged();

			return op;
		}

		public HotReloadClientOperation ContinueOrStartLocal(HotReloadSource source, Type[] types)
		{
			// Fast path: re-use the last operation if it's still in-progress or was ignored.
			// ReportIgnored does not set _result, so a reused ignored op is still "live":
			// the upcoming ReportCompleted will complete it normally and the terminal event
			// overrides the earlier Ignored one.
			if (_localOperations is [.., { Result: null or HotReloadClientResult.Ignored } last])
			{
				return last;
			}

			// Slow path: atomically create a new operation if the fast-path check
			// lost a race (another thread completed the last op between the check and here).
			ImmutableInterlocked.Update(ref _localOperations, CreateIfNeeded, (source, types, callback: (Action)NotifyStatusChanged));
			NotifyStatusChanged();
			return _localOperations.Last();

			static ImmutableList<HotReloadClientOperation> CreateIfNeeded(
				ImmutableList<HotReloadClientOperation> history,
				(HotReloadSource source, Type[] types, Action callback) args)
			{
				if (history is [.., { Result: null or HotReloadClientResult.Ignored }])
				{
					return history; // Re-use — no mutation needed.
				}

				return history
					.Add(new HotReloadClientOperation(args.source, args.types, args.callback))
					.Sort(CompareBySequenceId);
			}
		}

		private static int CompareBySequenceId(HotReloadClientOperation left, HotReloadClientOperation right)
			=> Comparer<long>.Default.Compare(left.Id, right.Id);

		private void NotifyStatusChanged()
		{
			var status = BuildStatus();

			Current = status;
			try
			{
				owner.StatusChanged?.Invoke(this, status);
			}
			catch (Exception error)
			{
				this.Log().Error("Failed to notify the status changed.", error);
			}
		}

		private Status BuildStatus()
		{
			var serverState = _serverState ?? (_localOperations.Any() ? HotReloadState.Ready /* no info */ : HotReloadState.Initializing);
			var localState = _localOperations.Any(op => op.Result is null) ? HotReloadState.Processing : HotReloadState.Ready;
			var globalState = _serverState switch
			{
				HotReloadState.Disabled => HotReloadState.Disabled,
				HotReloadState.Initializing => HotReloadState.Initializing,
				_ => (HotReloadState)Math.Max((int)serverState, (int)localState)
			};

			return new(globalState, (serverState, _serverOperations.Values.ToImmutableArray()), (localState, _localOperations));
		}
	}

#if HAS_UNO_WINUI
	private void ReportDiagnostics(IImmutableList<string>? diagnostics)
	{
		if (diagnostics is { Count: > 0 })
		{
			_log.Error("The Hot Reload compilation failed with the following errors:");

			foreach (var operation in diagnostics)
			{
				_log.Error(operation);
			}
		}
	}
#endif

	public class HotReloadClientOperation
	{
		/// <summary>
		/// No-op sentinel used when no real operation is available.
		/// All lifecycle methods are safe to call but do nothing.
		/// </summary>
		internal static readonly HotReloadClientOperation Empty = new();

		#region Current
		[ThreadStatic]
		private static HotReloadClientOperation? _opForCurrentUiThread;

		internal static HotReloadClientOperation? GetForCurrentThread()
			=> _opForCurrentUiThread;

		internal void SetCurrent()
		{
			if (this == Empty) return;
			Debug.Assert(_opForCurrentUiThread == null, "Only one operation should be active at once for a given UI thread.");
			_opForCurrentUiThread = this;
		}

		internal void ResignCurrent()
		{
			if (this == Empty) return;
			Debug.Assert(_opForCurrentUiThread == this, "Another operation has been started for the current UI thread.");
			_opForCurrentUiThread = null;
		}
		#endregion

		private static int _count;

		private readonly Action? _onUpdated;
		private readonly bool _isEmpty;
		private string[]? _curatedTypes;
		private ImmutableList<Exception> _exceptions = ImmutableList<Exception>.Empty;
		private int _result = -1;

		/// <summary>Private constructor for the <see cref="Empty"/> sentinel.</summary>
		private HotReloadClientOperation()
		{
			_isEmpty = true;
			Source = default;
			Types = [];
		}

		internal HotReloadClientOperation(HotReloadSource source, Type[] types, Action onUpdated)
		{
			Source = source;
			Types = types;
			_onUpdated = onUpdated;

			SendEvent();
		}

		public int Id { get; } = Interlocked.Increment(ref _count);

		public DateTimeOffset StartTime { get; } = DateTimeOffset.Now;

		public HotReloadSource Source { get; }

		public Type[] Types { get; private set; }

		public string[] CuratedTypes => _curatedTypes ??= GetCuratedTypes();

		private string[] GetCuratedTypes()
		{
			// Build a _pretty list_ of types that are not too verbose.
			// The type "HotReloadException" could be injected by Hot Reload into the app.
			// The type "EmbeddedXamlSourcesProvider" is used by the XamlSourceGenerator to provide the XAML sources,
			// we don't want to surface it to the user.

			// Eventually we should use reflection to remove _generated code_ from the list using attributes discovery.

			return Types
				.Select(GetFriendlyName)
				.Where(name => name is { Length: > 0 } and not "HotReloadException" and not "EmbeddedXamlSourcesProvider" and not "HotReloadInfo")
				.Distinct()
				.OrderBy(n => n, StringComparer.Ordinal) // make the list deterministic
				.ToArray()!;

			static string GetFriendlyName(Type type)
			{
				// Possible incoming names – each should resolve to "Foo":
				// - Application.Foo                  (regular class)
				// - Application.Foo#1                (hot‑reload update)
				// - Application.Foo+Bar              (nested class)
				// - Application.Foo+Bar#1            (nested class, hot‑reload update)
				// - Application.Foo`1                (open generic)
				// - Application.Foo`1#1              (open generic, hot‑reload update)
				// - Application.Foo`1+Bar            (nested open generic)
				// - Application.Foo`1+Bar#1          (nested open generic, hot‑reload update)
				// - Application.Foo`1+Bar`1          (nested generic-with-generic)
				// - Application.Foo`1+Bar`1#1        (nested generic-with-generic, hot‑reload update)
				// - Application.Foo+Bar+_Internal    (deeper nesting)

				var name = (type.FullName ?? type.Name).AsSpan();

				// Strip any “[TArg,…]” generic‑argument list (closed generics).
				// Hot‑Reload never delivers closed generics, but this keeps us future‑proof.
				if (name.IndexOf('[') is var bracketIndex and >= 0)
				{
					name = name[..bracketIndex];
				}

				// Remove Hot‑Reload version suffix (“#n”).
				if (name.IndexOf('#') is var versionIndex and >= 0)
				{
					name = name[..versionIndex];
				}

				// Retain only the outermost class in a nesting chain (“Foo+Bar” → “Foo”).
				if (name.IndexOf('+') is var nestingIndex and >= 0)
				{
					name = name[..nestingIndex];
				}

				// Drop the namespace, keeping just the type name (“Namespace.Foo” → “Foo”).
				if (name.LastIndexOf('.') is var nsIndex and >= 0)
				{
					name = name[(nsIndex + 1)..];
				}

				// Remove the generic arity suffix (“Foo`1” → “Foo”).
				if (name.IndexOf('`') is var genericIndex and >= 0)
				{
					name = name[..genericIndex];
				}

				return name.ToString(); // never null
			}
		}

		public DateTimeOffset? EndTime { get; private set; }

		public DateTimeOffset? IgnoreTime { get; private set; }

		public HotReloadClientResult? Result => _result is -1 ? null : (HotReloadClientResult)_result;

		public ImmutableList<Exception> Exceptions => _exceptions;

		public string? IgnoreReason { get; private set; }

		private int _elementsReplaced;
		private int _elementErrors;

		/// <summary>Number of elements successfully replaced during the UI update.</summary>
		public int ElementsReplaced => _elementsReplaced;

		/// <summary>Number of elements that failed to replace during the UI update.</summary>
		public int ElementErrors => _elementErrors;

		internal void AddType(Type type)
		{
			if (_isEmpty) return;
			if (Types.Contains(type))
			{
				return;
			}

			_curatedTypes = null;
			Types = Types.Append(type).ToArray();
			_onUpdated?.Invoke();
		}

		internal void ReportError(MethodInfo source, Exception error)
			=> ReportError(error); // For now we just ignore the source

		internal void ReportError(Exception error)
		{
			if (_isEmpty) return;
			ImmutableInterlocked.Update(ref _exceptions, static (errors, error) => errors.Add(error), error);
			_onUpdated?.Invoke();
		}

		internal void ReportWarning(Exception exception)
		{
			// This is a warning that does not prevent the HR to complete.
			// For now, we don't even surface it to the user in any way other than logs (by caller).
		}

		internal void ReportElementReplaced()
		{
			if (_isEmpty) return;
			Interlocked.Increment(ref _elementsReplaced);
		}

		internal void ReportElementError(Exception error)
		{
			if (_isEmpty) return;
			Interlocked.Increment(ref _elementErrors);
			ReportError(error);
		}

		/// <summary>
		/// Marks the operation as complete and sends a terminal event to the server.
		/// </summary>
		/// <remarks>
		/// <para>This is the single point where <see cref="_result"/> is assigned. It is
		/// intentionally never called from <see cref="ReportIgnored"/>: an Ignored op
		/// is <em>potentially</em> — not necessarily — intermediate. HotDesign may
		/// re-apply the deferred delta and call <see cref="ReportCompleted"/> through
		/// <c>ReloadWithUpdatedTypes</c>, in which case Ignored was indeed a transient
		/// phase; or no one re-applies and Ignored remains the effective final state
		/// (observed downstream as a <c>TimedOut</c> once the UI-feedback window elapses).
		/// Keeping <see cref="_result"/> at <c>-1</c> after <see cref="ReportIgnored"/>
		/// is what lets the first case override the second.</para>
		/// <para><see cref="IgnoreTime"/> and <see cref="IgnoreReason"/> are preserved
		/// across the override — they document that the op went through an Ignored
		/// phase. The emitted event's <c>Kind</c> still surfaces the terminal
		/// Succeeded/Failed because <c>HotReloadClientOperationEvent.Kind</c> gives
		/// <see cref="EndTime"/> priority over <see cref="IgnoreTime"/>.</para>
		/// </remarks>
		internal void ReportCompleted()
		{
			if (_isEmpty) return;

			// IgnoreReason is intentionally NOT consulted here: an explicit ReportCompleted
			// overrides any prior ReportIgnored. Only _exceptions decide Success vs Failed.
			var result = _exceptions.Count > 0
				? HotReloadClientResult.Failed
				: HotReloadClientResult.Success;

			if (Interlocked.CompareExchange(ref _result, (int)result, -1) is -1)
			{
				EndTime = DateTimeOffset.Now;

				// IgnoreTime / IgnoreReason are deliberately preserved: they document
				// that the op went through a transient Ignored phase before completing.
				// The Kind derivation on HotReloadClientOperationEvent gives EndTime
				// priority over IgnoreTime, so the emitted event's Kind is the terminal
				// Succeeded/Failed; consumers that care about the intermediate Ignored
				// state can still inspect IgnoreTime/IgnoreReason on the event.
				_onUpdated?.Invoke();
				SendEvent();
			}
			else
			{
				Debug.Fail("The result should not have already been set.");
			}
		}

		internal void ReportIgnored(string reason)
		{
			if (_isEmpty) return;

			// NOTE: DO NOT call ReportCompleted from here. Ignored is *potentially*
			// intermediate (HotDesign may later re-apply the delta and call
			// ReportCompleted, in which case the terminal state overrides), but it
			// may also remain the effective final state if no one re-applies — the
			// downstream consumer decides how to interpret it, typically by waiting
			// out the UI-feedback window and escalating to TimedOut. Either way the
			// op must stay reopenable, so we leave _result at -1 and emit the
			// Ignored event directly. _result transitions from -1 only through
			// ReportCompleted.
			IgnoreReason = reason;
			IgnoreTime = DateTimeOffset.Now;
			_onUpdated?.Invoke();
			SendEvent();
		}

		private void SendEvent()
		{
#if HAS_UNO_WINUI
			try
			{
				var client = RemoteControlClient.Instance;
				if (client is null) return;

				var errorMessage = _exceptions.Count > 0
					? string.Join("\r\n", _exceptions.Select(e => e.Message))
					: null;

				_ = client.SendMessage(new Messages.HotReloadClientOperationEvent
				{
					OperationSequenceId = Id,
					StartTime = StartTime,
					IgnoreTime = IgnoreTime,
					EndTime = EndTime,
					ErrorMessage = errorMessage,
					FailedElementCount = _elementErrors,
					TotalElementCount = _elementsReplaced + _elementErrors,
				});
			}
			catch
			{
				// Best-effort — never fail the operation because of a notification error.
			}
#endif
		}
	}
}
