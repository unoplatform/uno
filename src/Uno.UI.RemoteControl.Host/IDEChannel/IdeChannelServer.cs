using System;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Host.IDEChannel;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

internal class IdeChannelServer : IIdeChannelServer
{
	public IdeChannelServer()
	{
	}

	public event EventHandler<IdeMessage>? MessageFromIDE;

	public event EventHandler<IdeMessageEnvelope>? MessageFromDevServer;

	public async Task SendToIdeAsync(IdeMessage message)
	{
		MessageFromDevServer?.Invoke(this, IdeMessageSerializer.Serialize(message));

		await Task.Yield();
	}

	public async Task SendToDevServerAsync(IdeMessageEnvelope message)
	{
		MessageFromIDE?.Invoke(this, IdeMessageSerializer.Deserialize(message));

		await Task.Yield();
	}
}
