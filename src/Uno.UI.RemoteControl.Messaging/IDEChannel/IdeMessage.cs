#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Base class to message exchanged between the IDE and teh dev-server.
/// </summary>
public record IdeMessage(string Scope);
