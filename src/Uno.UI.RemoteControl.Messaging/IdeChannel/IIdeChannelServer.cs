#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Services
{
	// Unlike the IIdeChannelServer, this is to be used by the user code.
	// Its abstract the notion of sending messages to the IDE, a service offered by the dev-server.

	/// <summary>
	/// Interface to communicate with the IDE.
	/// </summary>
	public interface IIdeChannel
	{
		/// <summary>
		/// Fired when a message is received from the IDE.
		/// </summary>
		event EventHandler<IdeMessage>? MessageFromIde;

		/// <summary>
		/// DO NOT USE - Prefer the TrySendToIdeAsync instead
		/// Sends a message to the IDE.
		/// </summary>
		/// <param name="message">The message to send to the IDE.</param>
		/// <param name="ct">CancellationToken to cancel the async operation.</param>
		/// <returns>An async operation representing the fact that the message has been sent to the IDE (does not include any form of processing by IDE).</returns>
		Task SendToIdeAsync(IdeMessage message, CancellationToken ct);

		/// <summary>
		/// Sends a message to the IDE.
		/// </summary>
		/// <param name="message">The message to send to the IDE.</param>
		/// <param name="ct">CancellationToken to cancel the async operation.</param>
		/// <returns>An async bool representing the fact that the message has been sent to the IDE or not (does not include any form of processing by IDE).</returns>
		Task<bool> TrySendToIdeAsync(IdeMessage message, CancellationToken ct);

		/// <summary>
		/// Waits for the channel to become ready for sending/receiving messages.
		/// </summary>
		/// <param name="ct">Cancellation token used to observe cancellation.</param>
		/// <returns>True when the channel is ready.</returns>
		Task<bool> WaitForReady(CancellationToken ct = default);
	}
}
