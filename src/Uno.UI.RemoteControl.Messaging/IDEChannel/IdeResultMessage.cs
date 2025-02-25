#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record IdeResultMessage(long IdeCorrelationId, Result Result) : IdeMessage(WellKnownScopes.HotReload);
