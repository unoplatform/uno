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

		public void ReportInvalidRuntime()
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

		public HotReloadClientOperation ReportLocalStarting(Type[] types)
		{
			var op = new HotReloadClientOperation(_source, types, NotifyStatusChanged);
			ImmutableInterlocked.Update(ref _localOperations, static (history, op) => history.Add(op).Sort(Compare), op);
			NotifyStatusChanged();

			return op;

			static int Compare(HotReloadClientOperation left, HotReloadClientOperation right)
				=> Comparer<long>.Default.Compare(left.Id, right.Id);
		}

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
		#region Current
		[ThreadStatic]
		private static HotReloadClientOperation? _opForCurrentUiThread;

		internal static HotReloadClientOperation? GetForCurrentThread()
			=> _opForCurrentUiThread;

		internal void SetCurrent()
		{
			Debug.Assert(_opForCurrentUiThread == null, "Only one operation should be active at once for a given UI thread.");
			_opForCurrentUiThread = this;
		}

		internal void ResignCurrent()
		{
			Debug.Assert(_opForCurrentUiThread == this, "Another operation has been started for the current UI thread.");
			_opForCurrentUiThread = null;
		}
		#endregion

		private static int _count;

		private readonly Action _onUpdated;
		private string[]? _curatedTypes;
		private ImmutableList<Exception> _exceptions = ImmutableList<Exception>.Empty;
		private int _result = -1;

		internal HotReloadClientOperation(HotReloadSource source, Type[] types, Action onUpdated)
		{
			Source = source;
			Types = types;

			_onUpdated = onUpdated;
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
				.Where(name => name is { Length: > 0 } and not "HotReloadException" and not "EmbeddedXamlSourcesProvider")
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

		public HotReloadClientResult? Result => _result is -1 ? null : (HotReloadClientResult)_result;

		public ImmutableList<Exception> Exceptions => _exceptions;

		public string? IgnoreReason { get; private set; }

		internal void AddType(Type type)
		{
			if (Types.Contains(type))
			{
				return;
			}

			_curatedTypes = null;
			Types = Types.Append(type).ToArray();
			_onUpdated();
		}

		internal void ReportError(MethodInfo source, Exception error)
			=> ReportError(error); // For now we just ignore the source

		internal void ReportError(Exception error)
		{
			ImmutableInterlocked.Update(ref _exceptions, static (errors, error) => errors.Add(error), error);
			_onUpdated();
		}

		internal void ReportWarning(Exception exception)
		{
			// This is a warning that does not prevent the HR to complete.
			// For now, we don't even surface it to the user in any way other than logs (by caller).
		}

		internal void ReportCompleted()
		{
			var result = (_exceptions, IgnoreReason) switch
			{
				({ Count: > 0 }, _) => HotReloadClientResult.Failed,
				(_, not null) => HotReloadClientResult.Ignored,
				_ => HotReloadClientResult.Success
			};

			if (Interlocked.CompareExchange(ref _result, (int)result, -1) is -1)
			{
				EndTime = DateTimeOffset.Now;
				_onUpdated();
			}
			else if (_result != (int)HotReloadClientResult.Ignored) // ReportIgnored auto completes but caller usually does not expect it (use ReportCompleted in finally)
			{
				Debug.Fail("The result should not have already been set.");
			}
		}

		internal void ReportIgnored(string reason)
		{
			IgnoreReason = reason;
			ReportCompleted();
		}
	}
}
