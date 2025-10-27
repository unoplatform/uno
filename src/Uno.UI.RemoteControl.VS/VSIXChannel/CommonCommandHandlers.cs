using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
		public bool CanExecute(Command command)
			=> CanExecute(command, out _);

		private bool CanExecute(Command command, [NotNullWhen(true)] out Uri? target)
		{
			// Note: We validate the URI is valid and scheme is http(s) to avoid potential security issues with Process.Start

			if (!string.Equals(command.Name, "ide.open_browser", StringComparison.OrdinalIgnoreCase)
				|| !Uri.TryCreate(command.Parameter, UriKind.Absolute, out target)
				|| target.Scheme.ToLowerInvariant() is not "http" or "https")
			{
				target = null;
				return false;
			}

			return true;
		}

		/// <inheritdoc />
		public void Execute(Command command)
		{
			if (CanExecute(command, out var target))
			{
				_ = Process.Start(new ProcessStartInfo(target.OriginalString) { UseShellExecute = true });
			}
		}
	}
}
