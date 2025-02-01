#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record ForceHotReloadIdeMessage(long CorrelationId) : IdeMessage(WellKnownScopes.HotReload);
