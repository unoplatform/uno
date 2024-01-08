using System;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

internal class IdeChannelServer : IIdeChannelServer
{
	private IServiceProvider _serviceProvider;

	public IdeChannelServer(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public event EventHandler<IdeMessage>? MessageFromIDE;

	public event EventHandler<IdeMessage>? MessageFromDevServer;

	public async Task SendToIdeAsync(IdeMessage message)
	{
		MessageFromDevServer?.Invoke(this, message);

		await Task.Yield();
	}

	public async Task SendToDevServerAsync(IdeMessage message)
	{
		MessageFromIDE?.Invoke(this, message);

		await Task.Yield();
	}
}
