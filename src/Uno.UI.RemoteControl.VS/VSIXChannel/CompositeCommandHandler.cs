using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.VS.Helpers;
using _Command = Uno.UI.RemoteControl.Messaging.IdeChannel.Command;

#pragma warning disable CS1031 // Disable "Do not catch general exception types" for this file as it's our expected behavior here.

namespace Uno.IDE;

/// <summary>
/// Composite handler that dispatch the command to the selected handler.
/// </summary>
internal sealed class CompositeCommandHandler(ILogger log, params ImmutableList<(string name, ICommandHandler handler)> registrations) : ICommandHandler, ICommandHandlerRegistry
{
	private sealed record Registration(string Name, ICommandHandler Handler);

	private ImmutableList<Registration> _registrations = [.. registrations.Select(tuple => new Registration(tuple.name, tuple.handler))];

	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged;

	/// <inheritdoc />
	public async Task<bool> CanExecuteAsync(_Command command, CancellationToken ct)
		=> await GetHandlerAsync(command, ct) is not null;

	/// <inheritdoc />
	public async Task ExecuteAsync(_Command command, CancellationToken ct)
	{
		var reg = await GetHandlerAsync(command, ct);
		if (reg is null)
		{
			log.Warn($"No handler found for command '{command.Name}'.");
			return;
		}

		log.Verbose($"Executing command {command.Name} with handler {reg.Name}.");

		try
		{
			await reg.Handler.ExecuteAsync(command, ct);
		}
		catch (Exception error)
		{
			log.Error($"Failed to execute command for command '{command.Name}' ({error.Message}).");
		}
	}

	/// <inheritdoc />
	public void Register(string name, ICommandHandler handler)
	{
		if (ImmutableInterlocked.Update(ref _registrations, static (handlers, handler) => handlers.Add(handler), new Registration(name, handler)))
		{
			handler.CanExecuteChanged += OnHandlerCanExecuteChanged;
		}
	}

	/// <inheritdoc />
	public void Unregister(ICommandHandler handler)
	{
		if (ImmutableInterlocked.Update(ref _registrations, static (handlers, handler) => handlers.RemoveAll(reg => reg.Handler == handler), handler))
		{
			handler.CanExecuteChanged -= OnHandlerCanExecuteChanged;
		}
	}

	private void OnHandlerCanExecuteChanged(object sender, EventArgs e)
		=> CanExecuteChanged?.Invoke(this, EventArgs.Empty);

	private async ValueTask<Registration?> GetHandlerAsync(_Command command, CancellationToken ct)
	{
		foreach (var reg in _registrations)
		{
			try
			{
				if (await reg.Handler.CanExecuteAsync(command, ct))
				{
					return reg;
				}
			}
			catch (Exception error)
			{
				log.Debug($"Command handler {reg.GetType().Name}.CanExecute failed for command '{command.Name}' ({error.Message}).");
			}
		}

		return null;
	}
}
