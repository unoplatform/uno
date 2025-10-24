using System;
using System.Diagnostics;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.IDE;

/// <summary>
/// Basic command handler that natively only supports opening links in the browser (ide.open_browser).
/// Other commands are forwarded to a remote handler that can be registered via <see cref="SetRemoteHandler(ICommandHandler)"/>.
/// The "remote handler" is resolved from the VS.RC package and will typically send commands to the dev-server.
/// </summary>
internal sealed class IdeCommandHandler(ILogger log) : ICommandHandler
{
	private ICommandHandler? _handler;

	public void SetRemoteHandler(ICommandHandler execute)
		=> _handler = execute ?? throw new ArgumentNullException(nameof(execute));

	public void Execute(Command command)
	{
		switch (command.Name.ToLowerInvariant())
		{
			case "ide.open_browser" when command.Parameter is not null && Uri.TryCreate(command.Parameter, UriKind.Absolute, out var target):
				// Note: We validate the URI is valid to avoid potential security issues with Process.Start
				_ = Process.Start(new ProcessStartInfo(target.OriginalString) { UseShellExecute = true });
				break;

			case "ide.open_browser":
				log.Error($"'{command.Parameter}' is not a valid absolute URI.");
				break;

			default:
				if (_handler is null)
				{
					log.Error($"No handler registered for command '{command.Text}' ({command.Name}).");
				}
				else
				{
					try
					{
						_handler.Execute(command);
					}
					catch (Exception error)
					{
						log.Error($"Failed to execute command '{command.Text}' ({command.Name}).\r\n{error.Message}");
					}
				}
				break;
		}
	}
}
