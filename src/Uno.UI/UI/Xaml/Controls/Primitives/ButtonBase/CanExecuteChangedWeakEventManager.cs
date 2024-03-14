#nullable enable

using System;
using System.Windows.Input;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal static class CanExecuteChangedWeakEventManager
{
	private static Dictionary<ICommand, List<Subscription>> _subscriptions = new();

	private struct Subscription
	{
		public ICommand Command { get; private set; }
		public WeakReference Target { get; private set; }
		public MethodInfo HandlerMethod { get; private set; }
		private EventHandler _onCanExecuteChanged;

		public Subscription(ICommand command, WeakReference target, MethodInfo handlerMethod)
		{
			Command = command;
			Target = target;
			HandlerMethod = handlerMethod;
			var @this = this;
			_onCanExecuteChanged = (object? _, EventArgs _) =>
			{
				@this.HandlerMethod.Invoke(@this.Target.Target, null);
			};
			command.CanExecuteChanged += _onCanExecuteChanged;
		}

		public void Cleanup()
		{
			Command.CanExecuteChanged -= _onCanExecuteChanged;
			Command = null!;
			Target = null!;
			HandlerMethod = null!;
			_onCanExecuteChanged = null!;
		}
	}

	public static void AddHandler(ICommand source, EventHandler<EventArgs> handler)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		if (!_subscriptions.TryGetValue(source, out var commandSubscriptions))
		{
			commandSubscriptions = new List<Subscription>();
			_subscriptions.Add(source, commandSubscriptions);
		}
		else
		{
			for (int i = commandSubscriptions.Count - 1; i >= 0; i--)
			{
				if (!commandSubscriptions[i].Target.IsAlive)
				{
					commandSubscriptions[i].Cleanup();
					commandSubscriptions.RemoveAt(i);
				}
			}
		}

		var subscription = new Subscription(source, new WeakReference(handler.Target), handler.Method);
		commandSubscriptions.Add(subscription);
	}

	public static void RemoveHandler(ICommand source, EventHandler<EventArgs> handler)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		if (_subscriptions.TryGetValue(source, out var subscriptions))
		{
			for (int n = subscriptions.Count - 1; n >= 0; n--)
			{
				Subscription current = subscriptions[n];

				if (!current.Target.IsAlive)
				{
					subscriptions.RemoveAt(n);
					continue;
				}

				if (current.Target.Target == handler.Target && current.HandlerMethod == handler.Method)
				{
					// Found the match, we can break
					subscriptions.RemoveAt(n);
					break;
				}
			}
		}
	}

}
