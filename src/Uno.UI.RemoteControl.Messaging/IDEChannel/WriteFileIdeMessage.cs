#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Request to write (i.e. replace all content) a file in the IDE.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="FilePath"></param>
/// <param name="FileContent">The new text to write.</param>
/// <param name="ForceSaveOnDisk"></param>
public record WriteFileIdeMessage(
	long CorrelationId,
	string FilePath,
	string FileContent,
	bool ForceSaveOnDisk) : IdeMessageWithCorrelationId(CorrelationId, WellKnownScopes.HotReload);
