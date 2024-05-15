#nullable enable

using System;

namespace Uno.UI.RemoteControl
{
	public class SendMessageFailedEventArgs : EventArgs
	{
		public SendMessageFailedEventArgs(IMessage message)
		{
			Message = message;
		}

		public IMessage Message { get; }
	}
}
