using static Uno.UI.RemoteControl.Messages.ProcessorsDiscoveryResponse;

namespace Uno.UI.RemoteControl.Messages;

public class ProcessorsDiscovery : IMessage
{
	public const string Name = nameof(ProcessorsDiscovery);

	public ProcessorsDiscovery(string basePath, string appInstanceId = "")
	{
		BasePath = basePath;
		AppInstanceId = appInstanceId;
	}

	public string Scope => WellKnownScopes.DevServerChannel;

	string IMessage.Name => Name;

	public string BasePath { get; }

	public string AppInstanceId { get; }
}
