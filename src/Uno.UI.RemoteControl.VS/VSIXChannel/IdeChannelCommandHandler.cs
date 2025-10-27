using System;
using System.Threading;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.IdeChannel;

namespace Uno.IDE;

internal class IdeChannelCommandHandler(IdeChannelClient ideChannel) : ICommandHandler, IDisposable
{
	private readonly CancellationTokenSource _ct = new();

#pragma warning disable CS0067 // Event is never used
	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

	/// <inheritdoc />
	public bool CanExecute(Command command)
		=> true; // Dev-server does not support command querying yet

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
