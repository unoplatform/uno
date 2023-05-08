#nullable enable

using System;

namespace Uno.UI.RemoteControl
{
	public class ClientEventEventArgs : EventArgs
	{
		public ClientEventEventArgs(string eventName, string eventDetails)
		{
			EventName = eventName;
			EventDetails = eventDetails;
		}

		public string EventName { get; }
		public string EventDetails { get; }
	}
}
