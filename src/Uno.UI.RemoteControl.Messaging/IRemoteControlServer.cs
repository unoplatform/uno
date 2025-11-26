using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host
{
	public interface IRemoteControlServer
	{
		string GetServerConfiguration(string key);

		Task SendFrame(IMessage message);

		/// <summary>
		/// DO NOT USE, prefer the TrySendMessageToIDEAsync
		/// </summary>
		Task SendMessageToIDEAsync(IdeMessage message);

		/// <summary>
		/// Attempt to send a message to the IDE, if any connection is established.
		/// </summary>
		/// <param name="message">The message to sent to the IDE.</param>
		/// <returns>
		/// An asynchronous boolean indicating if the message has been sent the IDE or not.
		/// WARNING: This does NOT indicate that the IDE has processed the message, only that an IDE is listening for messages.
		/// </returns>
		Task<bool> TrySendMessageToIDEAsync(IdeMessage message, CancellationToken ct);
	}
}
