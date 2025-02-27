namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record HotReloadRequestedIdeMessage(long IdeCorrelationId, Result Result) : IdeMessage(WellKnownScopes.HotReload);
