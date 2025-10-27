using System;
using System.Threading;
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
	public bool CanExecute(Command command)
		=> command.Name.Equals(DevelopmentEnvironmentStatusIdeMessage.DevServer.Restart.Name, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc />
	public void Execute(Command command)
	{
		if (CanExecute(command))
		{
			_ = devServerManager.RestartDevServerAsync(CancellationToken.None);
		}
	}
}
