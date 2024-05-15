using System;
using Newtonsoft.Json;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host.IDEChannel;

internal static class IdeMessageSerializer
{
	public static IdeMessage Deserialize(IdeMessageEnvelope envelope)
	{
		if (Type.GetType(envelope.MessageType) is { } messageType)
		{
			if (JsonConvert.DeserializeObject(envelope.MessageBody, messageType) is IdeMessage instance)
			{
				return instance;
			}
			else
			{
				throw new InvalidOperationException($"The message could not be deserialized for type {envelope.MessageType}, the message is not of type IdeMessage");
			}
		}
		else
		{
			throw new InvalidOperationException($"The message could not be deserialized for type {envelope.MessageType}, the type could not be found.");
		}
	}

	public static IdeMessageEnvelope Serialize(IdeMessage message)
		=> new(message.GetType().AssemblyQualifiedName!, JsonConvert.SerializeObject(message));
}
