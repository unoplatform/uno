using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.Helpers;

/// <summary>
/// WebSocket-based implementation of <see cref="IFrameTransport"/>.
/// </summary>
internal sealed class WebSocketFrameTransport(WebSocket socket) : IFrameTransport
{
	private readonly WebSocket _socket = socket ?? throw new ArgumentNullException(nameof(socket));

	/// <inheritdoc />
	public bool IsConnected => _socket.State == WebSocketState.Open;

	/// <inheritdoc />
	public Task<Frame?> ReceiveAsync(CancellationToken ct)
		=> WebSocketHelper.ReadFrame(_socket, ct);

	/// <inheritdoc />
	public Task SendAsync(Frame frame, CancellationToken ct)
		=> WebSocketHelper.SendFrame(_socket, frame, ct);

	/// <inheritdoc />
	public async Task CloseAsync()
	{
		if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
		{
			try
			{
				await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
			}
			catch
			{
				// Best-effort close; ignore failures.
			}
		}
	}

	/// <inheritdoc />
	public void Dispose() => _socket.Dispose();
}
