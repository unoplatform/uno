using System;
using System.Threading;
using System.Threading.Tasks;
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
	public Task<bool> CanExecuteAsync(Command command, CancellationToken ct)
		=> Task.FromResult(true); // Dev-server does not support command querying yet

	/// <inheritdoc />
	public async Task ExecuteAsync(Command command, CancellationToken ct)
	{
		ct = CancellationTokenSource.CreateLinkedTokenSource(ct, _ct.Token).Token;
		if (ct.IsCancellationRequested)
		{
			return;
		}

		await ideChannel.SendToDevServerAsync(new CommandRequestIdeMessage(System.Diagnostics.Process.GetCurrentProcess().Id, command.Name, command.Parameter), ct);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_ct.Cancel();
		_ct.Dispose();
	}
}
