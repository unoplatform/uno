using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.Diagnostics.UI;
using Uno.UI.RemoteControl.Messages;
using Frame = Uno.UI.RemoteControl.HotReload.Messages.Frame;

namespace Uno.UI.RemoteControl;

public partial class RemoteControlClient
{
	private interface IDiagnosticsSink : DevServerDiagnostics.ISink
	{
		void ReportActiveConnection(Connection? connection);

		void Report(ConnectionStatus status);

		void ReportPing(KeepAliveMessage ping);

		void ReportPong(KeepAliveMessage? pong);

		void ReportKeepAliveAborted(Exception error);

		void RegisterRequiredServerProcessor(string typeFullName, string version);

		void ReportServerProcessors(ProcessorsDiscoveryResponse response);
	}

	private enum ConnectionStatus
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

	private enum KeepAliveStatus
	{
		Idle,
		Ok, // Got ping/pong in expected delays
		Late, // Sent ping without response within delay
		Lost, // Got an invalid pong response
		Aborted // KeepAlive was aborted
	}

	private class DiagnosticsView : IDiagnosticsSink, IDiagnosticViewProvider
	{
		private ConnectionStatus _status = ConnectionStatus.Idle;
		private readonly DiagnosticViewHelper<Ellipse> _statusView;

		public DiagnosticsView()
		{
			_statusView = new(
				() => new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(Colors.Gray) },
				ellipse =>
				{
					((SolidColorBrush)ellipse.Fill).Color = GetStatusColor();
					ToolTipService.SetToolTip(ellipse, GetStatusSummary());
				});

			DiagnosticViewRegistry.Register(this); // Only register, do not make visible
		}

		#region Connection status
		public void ReportActiveConnection(Connection? connection)
			=> Report(connection switch
			{
				null when _status < ConnectionStatus.Connected => ConnectionStatus.ConnectionFailed,
				null => ConnectionStatus.Disconnected,
				_ => ConnectionStatus.Connected,
			});

		public void Report(ConnectionStatus status)
		{
			_status = status;
			_statusView.NotifyChanged();
		}
		#endregion

		#region KeepAlive (ping / pong)
		private const int _pongLateDelay = 100;
		private const int _pongTimeoutDelay = 1000;
		private KeepAliveMessage? _lastPing;
		private Stopwatch? _sinceLastPing;
		private KeepAliveStatus _keepAliveStatus = KeepAliveStatus.Ok; // We assume Ok as startup to not wait for the first ping to turn green.
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
			var that = (DiagnosticsView)state!;

			if (that._keepAliveStatus is KeepAliveStatus.Late)
			{
				that.ReportPong(null);
			}
			else
			{
				that._keepAliveStatus = KeepAliveStatus.Late;
				that._statusView.NotifyChanged();
			}
		}

		public void ReportPong(KeepAliveMessage? pong)
		{
			var ping = _lastPing;
			if (ping is null || pong is null)
			{
				_sinceLastPing?.Stop();
				_pongTimeout?.Change(Timeout.Infinite, Timeout.Infinite);
				_keepAliveStatus = KeepAliveStatus.Lost;
				_statusView.NotifyChanged();
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
				_statusView.NotifyChanged();
			}

			if (_keepAliveStatus != KeepAliveStatus.Ok)
			{
				_keepAliveStatus = KeepAliveStatus.Ok;
				_statusView.NotifyChanged();
			}
		}

		public void ReportKeepAliveAborted(Exception error)
		{
			Interlocked.Exchange(ref _pongTimeout, null)?.Dispose();
			_sinceLastPing?.Stop();
			_keepAliveStatus = KeepAliveStatus.Aborted;
			_statusView.NotifyChanged();
		}
		#endregion

		#region Server status
		private record ProcessorInfo(string TypeFullName, string Version);
		private record struct MissingProcessor(string TypeFullName, string Version, string Details, string? Error = null);
		private ImmutableHashSet<ProcessorInfo> _requiredProcessors = ImmutableHashSet<ProcessorInfo>.Empty;
		private ProcessorsDiscoveryResponse? _loadedProcessors;
		private bool? _isMissingRequiredProcessor;

		public void RegisterRequiredServerProcessor(string typeFullName, string version)
			=> ImmutableInterlocked.Update(ref _requiredProcessors, static (set, info) => set.Add(info), new ProcessorInfo(typeFullName, version));

		public void ReportServerProcessors(ProcessorsDiscoveryResponse response)
		{
			_loadedProcessors = response;

			var isMissing = GetMissingServerProcessors().Any();
			if (_isMissingRequiredProcessor != isMissing)
			{
				_isMissingRequiredProcessor = isMissing;
				_statusView.NotifyChanged();
			}
		}

		private IEnumerable<MissingProcessor> GetMissingServerProcessors()
		{
			var response = _loadedProcessors;
			if (response is null)
			{
				yield break;
			}

			var loaded = response.Processors.ToDictionary(p => p.Type, StringComparer.OrdinalIgnoreCase);
			foreach (var required in _requiredProcessors)
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
		#endregion

		#region Processors status
		private long _invalidFrames;
		private ImmutableHashSet<Type> _invalidFrameTypes = ImmutableHashSet<Type>.Empty;

		public void ReportInvalidFrame<TContent>(Frame frame)
		{
			Interlocked.Increment(ref _invalidFrames);
			ImmutableInterlocked.Update(ref _invalidFrameTypes, static (set, type) => set.Add(type), typeof(TContent));
		}
		#endregion

		/// <inheritdoc />
		string IDiagnosticViewProvider.Name => "Dev-server";

		/// <inheritdoc />
		object IDiagnosticViewProvider.GetPreview(IDiagnosticViewContext context)
			=> _statusView.GetView(context);

		/// <inheritdoc />
		ValueTask<object?> IDiagnosticViewProvider.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
			=> ValueTask.FromResult<object?>(GetStatusDetails());

		private Color GetStatusColor()
			=> _status switch
			{
				ConnectionStatus.Idle => Colors.Gray,
				ConnectionStatus.NoServer => Colors.Red,
				ConnectionStatus.Connecting => Colors.Yellow,
				ConnectionStatus.ConnectionTimeout => Colors.Red,
				ConnectionStatus.ConnectionFailed => Colors.Red,
				ConnectionStatus.Reconnecting => Colors.Yellow,
				ConnectionStatus.Disconnected => Colors.Red,

				ConnectionStatus.Connected when _isVersionValid is false => Colors.Orange,
				ConnectionStatus.Connected when _invalidFrames is not 0 => Colors.Orange,
				ConnectionStatus.Connected when _keepAliveStatus is not KeepAliveStatus.Ok => Colors.Yellow,
				ConnectionStatus.Connected => Colors.Green,

				_ => Colors.Gray
			};

		private string GetStatusSummary()
		{
			var status = _status switch
			{
				ConnectionStatus.Idle => "Initializing...",
				ConnectionStatus.NoServer => "No server configured, cannot initialize connection.",
				ConnectionStatus.Connecting => "Connecting to dev-server.",
				ConnectionStatus.ConnectionTimeout => "Failed to connect to dev-server (timeout).",
				ConnectionStatus.ConnectionFailed => "Failed to connect to dev-server (error).",
				ConnectionStatus.Reconnecting => "Connection to dev-server has been lost, reconnecting.",
				ConnectionStatus.Disconnected => "Connection to dev-server has been lost, will retry later.",

				ConnectionStatus.Connected when _isVersionValid is false => "Connected to dev-server, but version mis-match with client.",
				ConnectionStatus.Connected when _invalidFrames is not 0 => $"Connected to dev-server, but received {_invalidFrames} invalid frames from the server.",
				ConnectionStatus.Connected when _isMissingRequiredProcessor is true => "Connected to dev-server, but some required processors are missing on server.",
				ConnectionStatus.Connected when _keepAliveStatus is KeepAliveStatus.Late => "Connected to dev-server, but keep-alive messages are taking longer than expected.",
				ConnectionStatus.Connected when _keepAliveStatus is KeepAliveStatus.Lost => "Connected to dev-server, but last keep-alive messages have been lost.",
				ConnectionStatus.Connected when _keepAliveStatus is KeepAliveStatus.Aborted => "Connected to dev-server, but keep-alive has been aborted.",
				ConnectionStatus.Connected => "Connected to dev-server.",

				_ => _status.ToString()
			};

			if (_roundTrip >= 0)
			{
				status += $" (ping {_roundTrip}ms)";
			}

			return status;
		}

		private string GetStatusDetails()
		{
			var details = new StringBuilder(GetStatusSummary());

			if (GetMissingServerProcessors().ToList() is { Count: > 0 } missing)
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

			if (_invalidFrameTypes is { Count: > 0 } invalidFrameTypes)
			{
				details.AppendLine();
				details.AppendLine();
				details.AppendLine($"Received {_invalidFrames} invalid frames from the server. Failing frame types ({invalidFrameTypes.Count}):");

				foreach (var type in invalidFrameTypes)
				{
					details.AppendLine($"- {type.FullName}");
				}
			}

			return details.ToString();
		}
	}
}
