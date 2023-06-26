namespace Uno.UI.RemoteControl.Messages
{
	public class ProcessorRemoval : IMessage
	{
		public const string Name = nameof(ProcessorRemoval);

		public ProcessorRemoval(string path, string processorScope)
		{
			Path = path;
			ProcessorScope = processorScope;
		}

		public string Scope => "RemoteControlServer";

		string IMessage.Name => Name;

		public string Path { get; }

		public string ProcessorScope { get; }
	}
}
