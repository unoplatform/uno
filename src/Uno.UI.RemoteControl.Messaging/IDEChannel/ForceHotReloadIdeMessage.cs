#nullable enable

using System.Collections.Generic;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record ForceHotReloadIdeMessage(
	long CorrelationId,
	IReadOnlyDictionary<string, string>? OptionalUpdatedFilesContent,
	bool ForceFileSave = false) : IdeMessage(WellKnownScopes.HotReload);
