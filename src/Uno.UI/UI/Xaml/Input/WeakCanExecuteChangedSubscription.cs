#nullable enable

using System;
using System.Diagnostics;
using System.Windows.Input;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Subscribes to <see cref="ICommand.CanExecuteChanged"/> without the command keeping the subscriber alive,
/// like WinUI's ctl::WeakEventPtr in ButtonBase and MenuFlyoutItem.
/// </summary>
internal static class WeakCanExecuteChangedSubscription
{
	/// <param name="onCanExecuteChanged">Must not capture <paramref name="owner"/>.</param>
	/// <returns>A disposable that removes the subscription. Only <paramref name="owner"/> may reference it.</returns>
	internal static IDisposable Create<TOwner>(ICommand command, TOwner owner, Action<TOwner> onCanExecuteChanged)
		where TOwner : class
	{
		Debug.Assert(!ReferenceEquals(onCanExecuteChanged.Target, owner), "The callback must not capture the owner, or the command keeps it alive.");

		var forwarder = new Forwarder<TOwner>(owner, onCanExecuteChanged);
		command.CanExecuteChanged += forwarder.OnCanExecuteChanged;

		// Holds the command weakly, and its finalizer removes the forwarder once the owner is collected.
		return new WeakSourceSubscription<ICommand, Forwarder<TOwner>>(
			command,
			forwarder,
			static (c, f) => c.CanExecuteChanged -= f.OnCanExecuteChanged,
			onDisposed: null,
			typeof(WeakCanExecuteChangedSubscription),
			"a CanExecuteChanged subscription");
	}

	// Referenced by the command: must not reference the owner strongly, nor the subscription.
	private sealed class Forwarder<TOwner>
		where TOwner : class
	{
		private readonly WeakReference<TOwner> _owner;
		private readonly Action<TOwner> _onCanExecuteChanged;

		public Forwarder(TOwner owner, Action<TOwner> onCanExecuteChanged)
		{
			_owner = new WeakReference<TOwner>(owner);
			_onCanExecuteChanged = onCanExecuteChanged;
		}

		// When the owner is dead, the subscription's finalizer removes this forwarder: unsubscribing here,
		// in the middle of the command's raise, could break a command that raises over its live handler list.
		public void OnCanExecuteChanged(object? sender, EventArgs args)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				_onCanExecuteChanged(owner);
			}
		}
	}
}
