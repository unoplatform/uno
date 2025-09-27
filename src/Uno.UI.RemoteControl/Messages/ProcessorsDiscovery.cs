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

	/// <summary>
	/// The dll of the processor(s) to load
	/// </summary>
	/// <remarks>
	/// If you specify simply a _path_, it will automatically search for Uno.*.Processors.dll in this path
	/// to load default Uno processors.
	/// </remarks>
	public string BasePath { get; }

	public string AppInstanceId { get; }
}
