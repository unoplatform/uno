using System;
using System.Linq;
using System.Text;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Uno.UI.RemoteControl.RemoteControlClient;

namespace Uno.UI.RemoteControl;

internal sealed partial class RemoteControlStatusView : Ellipse
{
	public RemoteControlStatusView()
	{
		Fill = new SolidColorBrush(Colors.Gray);
		Width = 16;
		Height = 16;
	}

	public void Update(Status status)
	{
		((SolidColorBrush)Fill).Color = GetStatusColor(status);
		ToolTipService.SetToolTip(this, GetStatusSummary(status));
	}

	private static Color GetStatusColor(Status status)
		=> status.State switch
		{
			ConnectionState.Idle => Colors.Gray,
			ConnectionState.NoServer => Colors.Red,
			ConnectionState.Connecting => Colors.Yellow,
			ConnectionState.ConnectionTimeout => Colors.Red,
			ConnectionState.ConnectionFailed => Colors.Red,
			ConnectionState.Reconnecting => Colors.Yellow,
			ConnectionState.Disconnected => Colors.Red,

			ConnectionState.Connected when status.IsVersionValid is false => Colors.Orange,
			ConnectionState.Connected when status.InvalidFrames.Count is not 0 => Colors.Orange,
			ConnectionState.Connected when status.KeepAlive.State is not KeepAliveState.Ok => Colors.Yellow,
			ConnectionState.Connected => Colors.Green,

			_ => Colors.Gray
		};

	private static string GetStatusSummary(Status status)
	{
		var summary = status.State switch
		{
			ConnectionState.Idle => "Initializing...",
			ConnectionState.NoServer => "No server configured, cannot initialize connection.",
			ConnectionState.Connecting => "Connecting to dev-server.",
			ConnectionState.ConnectionTimeout => "Failed to connect to dev-server (timeout).",
			ConnectionState.ConnectionFailed => "Failed to connect to dev-server (error).",
			ConnectionState.Reconnecting => "Connection to dev-server has been lost, reconnecting.",
			ConnectionState.Disconnected => "Connection to dev-server has been lost, will retry later.",

			ConnectionState.Connected when status.IsVersionValid is false => "Connected to dev-server, but version mis-match with client.",
			ConnectionState.Connected when status.InvalidFrames.Count is not 0 => $"Connected to dev-server, but received {status.InvalidFrames.Count} invalid frames from the server.",
			ConnectionState.Connected when status.MissingRequiredProcessors is { IsEmpty: false } => "Connected to dev-server, but some required processors are missing on server.",
			ConnectionState.Connected when status.KeepAlive.State is KeepAliveState.Late => "Connected to dev-server, but keep-alive messages are taking longer than expected.",
			ConnectionState.Connected when status.KeepAlive.State is KeepAliveState.Lost => "Connected to dev-server, but last keep-alive messages have been lost.",
			ConnectionState.Connected when status.KeepAlive.State is KeepAliveState.Aborted => "Connected to dev-server, but keep-alive has been aborted.",
			ConnectionState.Connected => "Connected to dev-server.",

			_ => status.State.ToString()
		};

		if (status.KeepAlive.RoundTrip >= 0)
		{
			summary += $" (ping {status.KeepAlive.RoundTrip}ms)";
		}

		return summary;
	}

	internal static string GetStatusDetails(Status status)
	{
		var details = new StringBuilder(GetStatusSummary(status));

		if (status.MissingRequiredProcessors is { Count: > 0 } missing)
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

		if (status.InvalidFrames.Types is { Count: > 0 } invalidFrameTypes)
		{
			details.AppendLine();
			details.AppendLine();
			details.AppendLine($"Received {status.InvalidFrames.Count} invalid frames from the server. Failing frame types ({invalidFrameTypes.Count}):");

			foreach (var type in invalidFrameTypes)
			{
				details.AppendLine($"- {type.FullName}");
			}
		}

		return details.ToString();
	}
}
