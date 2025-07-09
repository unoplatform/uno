namespace Uno.UI.RemoteControl.Messaging.Messages
{
	/// <summary>
	/// Message to request a graceful shutdown of the DevServer from a client (not IDE).
	/// </summary>
	public record ShutdownClientMessage() : IMessage
	{
		public string Name => "Shutdown";

		public string Scope => WellKnownScopes.DevServerChannel;
	}
}
