namespace Uno.UI.RemoteControl.Messages
{
	public class ProcessorsDiscovery : IMessage
	{
		public const string Name = nameof(ProcessorsDiscovery);

		public ProcessorsDiscovery(string basePath)
		{
			BasePath = basePath;
		}

		public string Scope => RemoteControlServerMessages.Scope;

		string IMessage.Name => Name;

		public string BasePath { get; }
	}
}
