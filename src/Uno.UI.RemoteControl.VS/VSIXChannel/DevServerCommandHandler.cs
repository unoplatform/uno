using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS;

namespace Uno.IDE;

internal sealed class DevServerCommandHandler(EntryPoint devServerManager) : ICommandHandler
{
#pragma warning disable CS0067 // Event is never used
	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

	/// <inheritdoc />
	public Task<bool> CanExecuteAsync(Command command, CancellationToken ct)
		=> Task.FromResult(CanExecute(command));

	/// <inheritdoc />
	public async Task ExecuteAsync(Command command, CancellationToken ct)
	{
		if (CanExecute(command))
		{
			await devServerManager.RestartDevServerAsync(CancellationToken.None);
		}
	}

	private bool CanExecute(Command command)
		=> command.Name.Equals(DevelopmentEnvironmentStatusIdeMessage.DevServer.Restart.Name, StringComparison.OrdinalIgnoreCase);
}
