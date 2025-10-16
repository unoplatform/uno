using System;
using System.Threading;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.IdeChannel;

namespace Uno.IDE;

internal class DevServerCommandHandler(IdeChannelClient ideChannel) : ICommandHandler, IDisposable
{
	private CancellationTokenSource _ct = new();
	
	/// <inheritdoc />
	public void Execute(Command command)
	{
		if (_ct.IsCancellationRequested)
		{
			return;
		}

		_ = ideChannel.SendToDevServerAsync(new CommandRequestIdeMessage(System.Diagnostics.Process.GetCurrentProcess().Id, command.Name, command.Parameter), _ct.Token);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_ct.Cancel();
		_ct.Dispose();
	}
}
