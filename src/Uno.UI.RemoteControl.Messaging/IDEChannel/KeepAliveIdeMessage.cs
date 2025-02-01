#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record KeepAliveIdeMessage(string Source) : IdeMessage(WellKnownScopes.IdeChannel);
