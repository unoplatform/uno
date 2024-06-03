#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record HotReloadRequestedIdeMessage(long RequestId, Result Result) : IdeMessage(WellKnownScopes.HotReload);
