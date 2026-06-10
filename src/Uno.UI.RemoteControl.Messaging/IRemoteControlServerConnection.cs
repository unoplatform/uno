using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.Host;

/// <summary>
/// Represents the transport loop that drives a remote control server connection.
/// </summary>
public interface IRemoteControlServerConnection
{
	/// <summary>
	/// Handles a single transport connection until completion.
	/// </summary>
	Task HandleConnectionAsync(IFrameTransport transport, CancellationToken ct);
}
