using System;
using System.Linq;
using System.Text;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl;

internal sealed partial class RemoteControlStatusView : Ellipse
{
	public RemoteControlStatusView()
	{
		Fill = new SolidColorBrush(Colors.Gray);
		Width = 16;
		Height = 16;
	}

	public void Update(RemoteControlClient.Status status)
	{
		((SolidColorBrush)Fill).Color = GetStatusColor(status);
		ToolTipService.SetToolTip(this, GetStatusSummary(status));
	}

	private Color GetStatusColor(RemoteControlClient.Status status)
		=> status.State switch
		{
			RemoteControlClient.ConnectionState.Idle => Colors.Gray,
			RemoteControlClient.ConnectionState.NoServer => Colors.Red,
			RemoteControlClient.ConnectionState.Connecting => Colors.Yellow,
			RemoteControlClient.ConnectionState.ConnectionTimeout => Colors.Red,
			RemoteControlClient.ConnectionState.ConnectionFailed => Colors.Red,
			RemoteControlClient.ConnectionState.Reconnecting => Colors.Yellow,
			RemoteControlClient.ConnectionState.Disconnected => Colors.Red,

			RemoteControlClient.ConnectionState.Connected when status.IsVersionValid is false => Colors.Orange,
			RemoteControlClient.ConnectionState.Connected when status.InvalidFrames.Count is not 0 => Colors.Orange,
			RemoteControlClient.ConnectionState.Connected when status.KeepAlive.State is not RemoteControlClient.KeepAliveState.Ok => Colors.Yellow,
			RemoteControlClient.ConnectionState.Connected => Colors.Green,

			_ => Colors.Gray
		};

	private static string GetStatusSummary(RemoteControlClient.Status status)
	{
		var summary = status.State switch
		{
			RemoteControlClient.ConnectionState.Idle => "Initializing...",
			RemoteControlClient.ConnectionState.NoServer => "No server configured, cannot initialize connection.",
			RemoteControlClient.ConnectionState.Connecting => "Connecting to dev-server.",
			RemoteControlClient.ConnectionState.ConnectionTimeout => "Failed to connect to dev-server (timeout).",
			RemoteControlClient.ConnectionState.ConnectionFailed => "Failed to connect to dev-server (error).",
			RemoteControlClient.ConnectionState.Reconnecting => "Connection to dev-server has been lost, reconnecting.",
			RemoteControlClient.ConnectionState.Disconnected => "Connection to dev-server has been lost, will retry later.",

			RemoteControlClient.ConnectionState.Connected when status.IsVersionValid is false => "Connected to dev-server, but version mis-match with client.",
			RemoteControlClient.ConnectionState.Connected when status.InvalidFrames.Count is not 0 => $"Connected to dev-server, but received {status.InvalidFrames.Count} invalid frames from the server.",
			RemoteControlClient.ConnectionState.Connected when status.MissingRequiredProcessors is { IsEmpty: false } => "Connected to dev-server, but some required processors are missing on server.",
			RemoteControlClient.ConnectionState.Connected when status.KeepAlive.State is RemoteControlClient.KeepAliveState.Late => "Connected to dev-server, but keep-alive messages are taking longer than expected.",
			RemoteControlClient.ConnectionState.Connected when status.KeepAlive.State is RemoteControlClient.KeepAliveState.Lost => "Connected to dev-server, but last keep-alive messages have been lost.",
			RemoteControlClient.ConnectionState.Connected when status.KeepAlive.State is RemoteControlClient.KeepAliveState.Aborted => "Connected to dev-server, but keep-alive has been aborted.",
			RemoteControlClient.ConnectionState.Connected => "Connected to dev-server.",

			_ => status.State.ToString()
		};

		if (status.KeepAlive.RoundTrip >= 0)
		{
			summary += $" (ping {status.KeepAlive.RoundTrip}ms)";
		}

		return summary;
	}

	internal static string GetStatusDetails(RemoteControlClient.Status status)
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
