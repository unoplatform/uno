using System;
using Uno.UI.RemoteControl;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel
{
	/// <summary>
	/// Message to request a graceful shutdown of the DevServer from the IDE.
	/// </summary>
	public record ShutdownIdeMessage() : IdeMessage(WellKnownScopes.DevServerChannel)
	{
		public string Name => "Shutdown";
	}
}
