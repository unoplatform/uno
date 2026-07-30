#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Messaging;

/// <summary>
/// Represents the transport mechanism used to move frames between the client runtime and the dev server.
/// </summary>
/// <remarks>
/// This abstraction exists to decouple the dev-server protocol from its physical transport.
/// It enables multiple transports (e.g., WebSocket, in-process, test harnesses) while keeping
/// the message protocol and client/server processing logic unchanged.
///
/// Typical use-cases:
/// - Default WebSocket transport for IDE-driven hot reload sessions
/// - In-process transport for embedded dev-server hosting (ALC)
/// - Test transports for integration or regression testing
/// </remarks>
public interface IFrameTransport : IDisposable
{
	/// <summary>
	/// Gets a value indicating whether the transport is currently connected.
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Receives the next frame from the transport
	/// </summary>
	/// <param name="ct">Cancellation token used to cancel the receive operation</param>
	/// <returns>
	/// The received frame, or <see langword="null"/> if the transport has been closed.
	/// This method should throw <see cref="OperationCanceledException"/> when <paramref name="ct"/> is canceled.
	/// </returns>
	Task<Frame?> ReceiveAsync(CancellationToken ct);

	/// <summary>
	/// Sends a frame over the transport.
	/// </summary>
	/// <param name="frame">The frame to send</param>
	/// <param name="ct">Cancellation token used to cancel the send operation.</param>
	Task SendAsync(Frame frame, CancellationToken ct);

	/// <summary>
	/// Closes the transport gracefully, if possible.
	/// </summary>
	Task CloseAsync();
}
