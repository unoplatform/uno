using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.IDE;

/// <summary>
/// Set of well-known command handlers.
/// </summary>
internal static class CommonCommandHandlers
{
	public static ICommandHandler OpenBrowser { get; } = new OpenBrowserCommandHandler();

	private sealed class OpenBrowserCommandHandler : ICommandHandler
	{
#pragma warning disable CS0067 // Event is never used
		/// <inheritdoc />
		public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

		/// <inheritdoc />
		public Task<bool> CanExecuteAsync(Command command, CancellationToken ct)
			=> Task.FromResult(CanExecute(command, out _));

		private bool CanExecute(Command command, [NotNullWhen(true)] out Uri? target)
		{
			// Note: We validate the URI is valid and scheme is http(s) to avoid potential security issues with Process.Start

			if (string.Equals(command.Name, "ide.open_browser", StringComparison.OrdinalIgnoreCase)
				&& Uri.TryCreate(command.Parameter, UriKind.Absolute, out target)
				&& target.Scheme.ToLowerInvariant() is "http" or "https")
			{
				return true;
			}

			target = null;
			return false;
		}

		/// <inheritdoc />
		public Task ExecuteAsync(Command command, CancellationToken ct)
		{
			if (CanExecute(command, out var target))
			{
				_ = Process.Start(new ProcessStartInfo(target.OriginalString) { UseShellExecute = true });
			}

			return Task.CompletedTask;
		}
	}
}
