#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Internal message used to manage the lack of serialization inheritance support from JsonRPC
/// </summary>
internal class IdeMessageEnvelope(string messageType, string messageBody)
{
	public string MessageType { get; } = messageType;

	public string MessageBody { get; } = messageBody;
}
