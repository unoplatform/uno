#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uno.Diagnostics.UI;

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
	internal EventHandler<Status>? StatusChanged;

	private readonly StatusSink _status;

	internal enum HotReloadSource
	{
		Runtime,
		DevServer,
		Manual
	}

	internal enum HotReloadClientResult
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
	internal record Status(
		HotReloadState State,
		(HotReloadState State, IImmutableList<HotReloadServerOperationData> Operations) Server,
		(HotReloadState State, IImmutableList<HotReloadClientOperation> Operations) Local);

	private class StatusSink(ClientHotReloadProcessor owner)
	{
#if HAS_UNO_WINUI
		private readonly DiagnosticView<HotReloadStatusView, Status> _view = DiagnosticView.Register<HotReloadStatusView, Status>("Hot reload", ctx => new HotReloadStatusView(ctx), static (view, status) => view.OnHotReloadStatusChanged(status));
#endif

		private HotReloadState? _serverState;
		private ImmutableDictionary<long, HotReloadServerOperationData> _serverOperations = ImmutableDictionary<long, HotReloadServerOperationData>.Empty;
		private ImmutableList<HotReloadClientOperation> _localOperations = ImmutableList<HotReloadClientOperation>.Empty;
		private HotReloadSource _source;

		public void ReportServerState(HotReloadState state)
		{
			_serverState = state;
			NotifyStatusChanged();
		}

#if HAS_UNO_WINUI
		public void ReportServerStatus(HotReloadStatusMessage status)
		{
			_serverState ??= status.State; // Do not override the state if it has already been set (debugger attached with dev-server)
			ImmutableInterlocked.Update(ref _serverOperations, UpdateOperations, status.Operations);
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
#if HAS_UNO_WINUI
			_view.Update(status);
#endif
			owner.StatusChanged?.Invoke(this, status);
		}

		private Status BuildStatus()
		{
			var serverState = _serverState ?? (_localOperations.Any() ? HotReloadState.Ready /* no info */ : HotReloadState.Initializing);
			var localState = _localOperations.Any(op => op.Result is null) ? HotReloadState.Processing : HotReloadState.Ready;
			var globalState = _serverState is HotReloadState.Disabled ? HotReloadState.Disabled : (HotReloadState)Math.Max((int)serverState, (int)localState);

			return new(globalState, (serverState, _serverOperations.Values.ToImmutableArray()), (localState, _localOperations));
		}
	}

	internal class HotReloadClientOperation
	{
		#region Current
		[ThreadStatic]
		private static HotReloadClientOperation? _opForCurrentUiThread;

		public static HotReloadClientOperation? GetForCurrentThread()
			=> _opForCurrentUiThread;

		public void SetCurrent()
		{
			Debug.Assert(_opForCurrentUiThread == null, "Only one operation should be active at once for a given UI thread.");
			_opForCurrentUiThread = this;
		}

		public void ResignCurrent()
		{
			Debug.Assert(_opForCurrentUiThread == this, "Another operation has been started for teh current UI thread.");
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

		public Type[] Types { get; }

		internal string[] CuratedTypes => _curatedTypes ??= GetCuratedTypes();

		private string[] GetCuratedTypes()
		{
			return Types
				.Select(GetFriendlyName)
				.Where(name => name is { Length: > 0 })
				.Distinct()
				.ToArray()!;

			string? GetFriendlyName(Type type)
			{
				var name = type.FullName ?? type.Name;

				var versionIndex = name.IndexOf('#');
				if (versionIndex >= 0)
				{
					name = name[..versionIndex];
				}

				var nestingIndex = name.IndexOf('+');
				if (nestingIndex >= 0)
				{
					name = name[..nestingIndex];
				}

				var endOfNamespaceIndex = name.LastIndexOf('.');
				if (endOfNamespaceIndex >= 0)
				{
					name = name[(endOfNamespaceIndex + 1)..];
				}

				return name;
			}
		}

		public DateTimeOffset? EndTime { get; private set; }

		public HotReloadClientResult? Result => _result is -1 ? null : (HotReloadClientResult)_result;

		public ImmutableList<Exception> Exceptions => _exceptions;

		public string? IgnoreReason { get; private set; }

		internal void ReportError(MethodInfo source, Exception error)
			=> ReportError(error); // For now we just ignore the source

		internal void ReportError(Exception error)
		{
			ImmutableInterlocked.Update(ref _exceptions, static (errors, error) => errors.Add(error), error);
			_onUpdated();
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
			else
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
