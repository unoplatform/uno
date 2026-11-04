#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
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

		var subscription = new Subscription<TOwner>(command, owner, onCanExecuteChanged);
		command.CanExecuteChanged += subscription.OnCanExecuteChanged;
		return new Handle(subscription);
	}

	// Referenced by the command only: must not reference the owner or the handle.
	private sealed class Subscription<TOwner> : IDisposable
		where TOwner : class
	{
		private readonly WeakReference<TOwner> _owner;
		private readonly Action<TOwner> _onCanExecuteChanged;
		private ICommand? _command;

		public Subscription(ICommand command, TOwner owner, Action<TOwner> onCanExecuteChanged)
		{
			_command = command;
			_owner = new WeakReference<TOwner>(owner);
			_onCanExecuteChanged = onCanExecuteChanged;
		}

		// When the owner is dead, its handle's finalizer removes this subscription: unsubscribing here,
		// in the middle of the command's raise, could break a command that raises over its live handler list.
		public void OnCanExecuteChanged(object? sender, EventArgs args)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				_onCanExecuteChanged(owner);
			}
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _command, null) is { } command)
			{
				SubscriptionCleanup.Run(
					() => command.CanExecuteChanged -= OnCanExecuteChanged,
					typeof(WeakCanExecuteChangedSubscription),
					$"a CanExecuteChanged subscription from {command.GetType()}");
			}
		}
	}

	// Referenced by the owner only: becomes unreachable together with the owner.
	private sealed class Handle : IDisposable
	{
		private readonly IDisposable _subscription;

		public Handle(IDisposable subscription) => _subscription = subscription;

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_subscription.Dispose();
		}

		~Handle()
			=> SubscriptionCleanup.EnqueueFromFinalizer(_subscription.Dispose, typeof(WeakCanExecuteChangedSubscription), "a CanExecuteChanged subscription");
	}
}
