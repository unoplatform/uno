namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record UpdateFileIdeMessage(
	long CorrelationId,
	string FileFullName,
	string FileContent,
	bool ForceSaveOnDisk) : IdeMessageWithCorrelationId(CorrelationId, WellKnownScopes.HotReload);
