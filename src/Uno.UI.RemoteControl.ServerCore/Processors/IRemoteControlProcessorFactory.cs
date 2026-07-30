using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messages;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Provides an abstraction that translates <see cref="ProcessorsDiscovery"/> messages into server processors without binding <see cref="RemoteControlServer"/> to assembly or file I/O.
/// </summary>
public interface IRemoteControlProcessorFactory
{
	/// <summary>
	/// Discovers, loads, and instantiates the processors required for the provided discovery request.
	/// </summary>
	/// <param name="discovery">Discovery message containing the request metadata.</param>
	/// <param name="ct">Cancellation token used to stop discovery.</param>
	/// <returns>A result describing the loaded processors plus discovery metadata.</returns>
	ValueTask<RemoteControlProcessorDiscoveryResult> DiscoverProcessorsAsync(ProcessorsDiscovery discovery, CancellationToken ct);
}
