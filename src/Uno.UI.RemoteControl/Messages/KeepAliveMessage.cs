#nullable disable

namespace Uno.UI.RemoteControl.Messages
{
	public class KeepAliveMessage : IMessage
	{
		public const string Name = nameof(KeepAliveMessage);

		public KeepAliveMessage()
		{
		}

		public string Scope => "RemoteControlServer";

		string IMessage.Name => Name;
	}
}
