#nullable enable
namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record IdeMessageWithCorrelationId(long CorrelationId, string Scope) : IdeMessage(Scope);
