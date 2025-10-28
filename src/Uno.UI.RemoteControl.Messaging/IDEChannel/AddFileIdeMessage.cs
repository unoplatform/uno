#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record AddFileIdeMessage(
	long CorrelationId,
	string FullName,
	string FileContent) : IdeMessageWithCorrelationId(CorrelationId, WellKnownScopes.HotReload);
