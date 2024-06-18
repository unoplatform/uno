using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Uno.Diagnostics.UI;
using Uno.UI.RemoteControl.Messages;
using Frame = Uno.UI.RemoteControl.HotReload.Messages.Frame;

namespace Uno.UI.RemoteControl;

public partial class RemoteControlClient
{
	internal event EventHandler<Status>? StatusChanged;

	#region Status object model
	internal enum ConnectionState
	{
		/// <summary>
		/// Client as not been started yet
		/// </summary>
		Idle,

		/// <summary>
		/// No server information to connect to.
		/// </summary>
		NoServer,

		/// <summary>
		/// Attempting to connect to the server.
		/// </summary>
		Connecting,

		/// <summary>
		/// Reach timeout while trying to connect to the server.
		/// Connection HAS NOT been established.
		/// </summary>
		ConnectionTimeout,

		/// <summary>
		/// Connection to the server failed.
		/// Connection HAS NOT been established.
		/// </summary>
		ConnectionFailed,

		/// <summary>
		/// Connection to the server has been established.
		/// </summary>
		Connected,

		/// <summary>
		/// Reconnecting to the server.
		/// Connection has been established once but lost since then, reconnecting to the SAME server.
		/// </summary>
		Reconnecting,

		/// <summary>
		/// Disconnected from the server.
		/// Connection has been established once but lost since then and cannot be restored for now but will be retried later.
		/// </summary>
		Disconnected
	}

	internal enum KeepAliveState
	{
		Idle,
		Ok, // Got ping/pong in expected delays
		Late, // Sent ping without response within delay
		Lost, // Got an invalid pong response
		Aborted // KeepAlive was aborted
	}

	internal record struct MissingProcessor(string TypeFullName, string Version, string Details, string? Error = null);

	internal record Status(
		ConnectionState State,
		bool? IsVersionValid,
		(KeepAliveState State, long RoundTrip) KeepAlive,
		ImmutableHashSet<MissingProcessor> MissingRequiredProcessors,
		(long Count, ImmutableHashSet<Type> Types) InvalidFrames)
	{
		public (StatusClassification kind, string message) GetSummary()
		{
			var (kind, message) = State switch
			{
				ConnectionState.Idle => (StatusClassification.Info, "Initializing..."),
				ConnectionState.NoServer => (StatusClassification.Error, "No server configured, cannot initialize connection."),
				ConnectionState.Connecting => (StatusClassification.Info, "Connecting to dev-server."),
				ConnectionState.ConnectionTimeout => (StatusClassification.Error, "Failed to connect to dev-server (timeout)."),
				ConnectionState.ConnectionFailed => (StatusClassification.Error, "Failed to connect to dev-server (error)."),
				ConnectionState.Reconnecting => (StatusClassification.Info, "Connection to dev-server has been lost, reconnecting."),
				ConnectionState.Disconnected => (StatusClassification.Error, "Connection to dev-server has been lost, will retry later."),

				ConnectionState.Connected when IsVersionValid is false => (StatusClassification.Warning, "Connected to dev-server, but version mis-match with client."),
				ConnectionState.Connected when InvalidFrames.Count is not 0 => (StatusClassification.Warning, $"Connected to dev-server, but received {InvalidFrames.Count} invalid frames from the server."),
				ConnectionState.Connected when MissingRequiredProcessors is { IsEmpty: false } => (StatusClassification.Warning, "Connected to dev-server, but some required processors are missing on server."),
				ConnectionState.Connected when KeepAlive.State is KeepAliveState.Late => (StatusClassification.Info, "Connected to dev-server, but keep-alive messages are taking longer than expected."),
				ConnectionState.Connected when KeepAlive.State is KeepAliveState.Lost => (StatusClassification.Warning, "Connected to dev-server, but last keep-alive messages have been lost."),
				ConnectionState.Connected when KeepAlive.State is KeepAliveState.Aborted => (StatusClassification.Warning, "Connected to dev-server, but keep-alive has been aborted."),
				ConnectionState.Connected => (StatusClassification.Ok, "Connected to dev-server."),

				_ => (StatusClassification.Warning, State.ToString()),
			};

			if (KeepAlive.RoundTrip >= 0)
			{
				message += $" (ping {KeepAlive.RoundTrip}ms)";
			}

			return (kind, message);
		}

		internal string GetDescription()
		{
			var details = new StringBuilder(GetSummary().message);

			if (MissingRequiredProcessors is { Count: > 0 } missing)
			{
				details.AppendLine();
				details.AppendLine();
				details.AppendLine("Some processor(s) requested by the client are missing on the server:");

				foreach (var m in missing)
				{
					details.AppendLine($"- {m.TypeFullName} v{m.Version}: {m.Details}");
					if (m.Error is not null)
					{
						details.AppendLine($"  {m.Error}");
					}
				}
			}

			if (InvalidFrames.Types is { Count: > 0 } invalidFrameTypes)
			{
				details.AppendLine();
				details.AppendLine();
				details.AppendLine($"Received {InvalidFrames.Count} invalid frames from the server. Failing frame types ({invalidFrameTypes.Count}):");

				foreach (var type in invalidFrameTypes)
				{
					details.AppendLine($"- {type.FullName}");
				}
			}

			return details.ToString();
		}
	}

	internal enum StatusClassification
	{
		Ok,
		Info,
		Warning,
		Error
	}
	#endregion

	private class StatusSink(RemoteControlClient owner) : DevServerDiagnostics.ISink
	{
		private ConnectionState _state = ConnectionState.Idle;
#if HAS_UNO_WINUI
		private readonly DiagnosticView<RemoteControlStatusView, Status> _view = DiagnosticView.Register<RemoteControlStatusView, Status>(
			"Dev-server",
			(view, status) => view.Update(status),
			status => status.GetDescription());
#endif

		public Status BuildStatus()
			=> new(_state, _isVersionValid, (_keepAliveState, _roundTrip), _missingRequiredProcessors, (_invalidFrames, _invalidFrameTypes));

		private void NotifyStatusChanged()
		{
			var status = BuildStatus();
#if HAS_UNO_WINUI
			_view.Update(status);
#endif
			owner.StatusChanged?.Invoke(owner, status);
		}

		#region Connection status
		public void ReportActiveConnection(Connection? connection)
			=> Report(connection switch
			{
				null when _state < ConnectionState.Connected => ConnectionState.ConnectionFailed,
				null => ConnectionState.Disconnected,
				_ => ConnectionState.Connected,
			});

		public void Report(ConnectionState state)
		{
			_state = state;
			NotifyStatusChanged();
		}
		#endregion

		#region KeepAlive (ping / pong)
		private const int _pongLateDelay = 100;
		private const int _pongTimeoutDelay = 1000;
		private KeepAliveMessage? _lastPing;
		private Stopwatch? _sinceLastPing;
		private KeepAliveState _keepAliveState = KeepAliveState.Ok; // We assume Ok as startup to not wait for the first ping to turn green.
		private bool? _isVersionValid;
		private Timer? _pongTimeout;
		private long _roundTrip = -1;

		public void ReportPing(KeepAliveMessage ping)
		{
			(_sinceLastPing ??= Stopwatch.StartNew()).Restart();
			_lastPing = ping;

			if (_pongTimeout is null)
			{
				Interlocked.CompareExchange(ref _pongTimeout, new Timer(OnPongLateOrTimeout, this, _pongLateDelay, _pongTimeoutDelay), null);
			}

			_pongTimeout.Change(_pongLateDelay, _pongTimeoutDelay);
		}

		private static void OnPongLateOrTimeout(object? state)
		{
			var that = (StatusSink)state!;

			if (that._keepAliveState is KeepAliveState.Late)
			{
				that.ReportPong(null);
			}
			else
			{
				that._keepAliveState = KeepAliveState.Late;
				that.NotifyStatusChanged();
			}
		}

		public void ReportPong(KeepAliveMessage? pong)
		{
			var ping = _lastPing;
			if (ping is null || pong is null)
			{
				_sinceLastPing?.Stop();
				_pongTimeout?.Change(Timeout.Infinite, Timeout.Infinite);
				_keepAliveState = KeepAliveState.Lost;
				NotifyStatusChanged();
				return;
			}

			if (pong.SequenceId < ping.SequenceId)
			{
				// Late pong, ignore it
				return;
			}

			_pongTimeout?.Change(Timeout.Infinite, Timeout.Infinite);
			_roundTrip = _sinceLastPing!.ElapsedMilliseconds;
			var isVersionValid = pong.AssemblyVersion == ping.AssemblyVersion;
			if (_isVersionValid != isVersionValid)
			{
				_isVersionValid = isVersionValid;
				NotifyStatusChanged();
			}

			if (_keepAliveState != KeepAliveState.Ok)
			{
				_keepAliveState = KeepAliveState.Ok;
				NotifyStatusChanged();
			}
		}

		public void ReportKeepAliveAborted(Exception error)
		{
			Interlocked.Exchange(ref _pongTimeout, null)?.Dispose();
			_sinceLastPing?.Stop();
			_keepAliveState = KeepAliveState.Aborted;
			NotifyStatusChanged();
		}
		#endregion

		#region Server status
		private record ProcessorInfo(string TypeFullName, string Version);

		private ImmutableHashSet<ProcessorInfo> _requiredProcessors = ImmutableHashSet<ProcessorInfo>.Empty;
		private ImmutableHashSet<MissingProcessor> _missingRequiredProcessors = ImmutableHashSet<MissingProcessor>.Empty;

		public void RegisterRequiredServerProcessor(string typeFullName, string version)
			=> ImmutableInterlocked.Update(ref _requiredProcessors, static (set, info) => set.Add(info), new ProcessorInfo(typeFullName, version));

		public void ReportServerProcessors(ProcessorsDiscoveryResponse response)
		{
			_missingRequiredProcessors = GetMissingServerProcessors(_requiredProcessors, response).ToImmutableHashSet();

			NotifyStatusChanged();

			static IEnumerable<MissingProcessor> GetMissingServerProcessors(ImmutableHashSet<ProcessorInfo> requiredProcessors, ProcessorsDiscoveryResponse response)
			{
				var loaded = response.Processors.ToDictionary(p => p.Type, StringComparer.OrdinalIgnoreCase);
				foreach (var required in requiredProcessors)
				{
					if (!loaded.TryGetValue(required.TypeFullName, out var actual))
					{
						yield return new MissingProcessor(required.TypeFullName, required.Version, "Processor not found by dev-server.");
						continue;
					}

					if (actual.LoadError is not null)
					{
						yield return new MissingProcessor(required.TypeFullName, required.Version, "Dev-server failed to create an instance.", actual.LoadError);
						continue;
					}

					if (!actual.IsLoaded)
					{
						yield return new MissingProcessor(required.TypeFullName, required.Version, "Type is not a valid server processor.");
						continue;
					}

					if (actual.Version != required.Version)
					{
						yield return new MissingProcessor(required.TypeFullName, required.Version, $"Version mismatch, client expected it to be {required.Version} but server loaded version {actual.Version}.");
					}
				}
			}
		}
		#endregion

		#region Processors status
		private long _invalidFrames;
		private ImmutableHashSet<Type> _invalidFrameTypes = ImmutableHashSet<Type>.Empty;

		public void ReportInvalidFrame<TContent>(Frame frame)
		{
			Interlocked.Increment(ref _invalidFrames);
			ImmutableInterlocked.Update(ref _invalidFrameTypes, static (set, type) => set.Add(type), typeof(TContent));

			NotifyStatusChanged();
		}
		#endregion
	}
}
