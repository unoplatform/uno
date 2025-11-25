#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel
{
	/// <summary>
	/// Interface for the IDE to communicate with the dev-server.
	/// </summary>
	internal interface IIdeChannelServer // This is internal as it should be used only by IDE extensions
	{
		// Note: This interface define the contract of the SHARED OBJECT for the communication channel between the IDE and the dev-server.
		// 		 It is not meant to be used by the user code, and uses Envelopes (while public IIdeChannel uses IdeMessage).
		//		 Envelopes are needed for JsonRpc as it is designed for strongly typed communication, not generic like we have.

		/// <summary>
		/// Fired when a message is received from the dev-server.
		/// </summary>
		event EventHandler<IdeMessageEnvelope>? MessageFromDevServer;

		/// <summary>
		/// Sends a message to the dev-server.
		/// </summary>
		/// <param name="envelope">The message to send to the dev-server.</param>
		/// <param name="ct">CancellationToken to cancel the async operation.</param>
		/// <returns>An async operation representing the fact that the message has been sent to the dev-server (does not include any form of processing by dev-server).</returns>
		Task SendToDevServerAsync(IdeMessageEnvelope envelope, CancellationToken ct);
	}
}

namespace Uno.UI.RemoteControl.Services
{
	// Unlike the IIdeChannelServer, this is to be used by the user code.
	// Its abstract the notion of sending messages to the IDE, a service offered by the dev-server.

	// Note: We put those interfaces in the same file to make it easier to understand the (non-)relationship between them.

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
		/// Sends a message to the IDE.
		/// </summary>
		/// <param name="message">The message to send to the IDE.</param>
		/// <param name="ct">CancellationToken to cancel the async operation.</param>
		/// <returns>An async operation representing the fact that the message has been sent to the IDE (does not include any form of processing by IDE).</returns>
		Task<bool> SendToIdeAsync(IdeMessage message, CancellationToken ct);
	}
}
