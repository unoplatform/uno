#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

// Mostly from https://github.com/dotnet/maui/blob/c92d44c68fe81c57ef8caec2506e6788309b8ff4/src/Core/src/WeakEventManager.cs
// But adjusted a little bit

namespace Uno.UI.Helpers
{
	internal sealed class WeakEventManager
	{
		private readonly Dictionary<string, List<Subscription>> _eventHandlers = new(StringComparer.Ordinal);

		public void AddEventHandler(Action? handler, [CallerMemberName] string eventName = "")
		{
			if (string.IsNullOrEmpty(eventName))
				throw new ArgumentNullException(nameof(eventName));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			AddEventHandler(eventName, handler.Target, handler.GetMethodInfo());
		}

		public void HandleEvent(string eventName)
		{
			if (_eventHandlers.TryGetValue(eventName, out List<Subscription>? target))
			{
				// clone the target array just in case one of the subscriptions calls RemoveEventHandler
				var targetClone = ArrayPool<Subscription>.Shared.Rent(target.Count);
				target.CopyTo(targetClone, 0);
				var count = target.Count;
				for (int i = 0; i < count; i++)
				{
					Subscription subscription = targetClone[i];
					bool isStatic = subscription.Subscriber == null;
					if (isStatic)
					{
						// For a static method, we'll just pass null as the first parameter of MethodInfo.Invoke
						subscription.Handler.Invoke(null, null);
						continue;
					}

					object? subscriber = subscription.Subscriber?.Target;

					if (subscriber == null)
					{
						// The subscriber was collected, so there's no need to keep this subscription around
						target.Remove(subscription);
					}
					else
					{
						subscription.Handler.Invoke(subscriber, null);
					}
				}

				ArrayPool<Subscription>.Shared.Return(targetClone);
			}
		}

		public void RemoveEventHandler(Action? handler, [CallerMemberName] string eventName = "")
		{
			if (string.IsNullOrEmpty(eventName))
				throw new ArgumentNullException(nameof(eventName));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			RemoveEventHandler(eventName, handler.Target, handler.GetMethodInfo());
		}

		private void AddEventHandler(string eventName, object? handlerTarget, MethodInfo methodInfo)
		{
			if (!_eventHandlers.TryGetValue(eventName, out List<Subscription>? targets))
			{
				targets = new List<Subscription>();
				_eventHandlers.Add(eventName, targets);
			}
			else
			{
				targets.RemoveAll(subscription => subscription.Subscriber is { IsAlive: false });
			}

			if (handlerTarget == null)
			{
				// This event handler is a static method
				targets.Add(new Subscription(null, methodInfo));
				return;
			}

			targets.Add(new Subscription(new WeakReference(handlerTarget), methodInfo));
		}

		private void RemoveEventHandler(string eventName, object? handlerTarget, MethodInfo methodInfo)
		{
			if (!_eventHandlers.TryGetValue(eventName, out List<Subscription>? subscriptions))
				return;

			for (int n = subscriptions.Count - 1; n >= 0; n--)
			{
				Subscription current = subscriptions[n];

				if (current.Subscriber != null && !current.Subscriber.IsAlive)
				{
					// If not alive, remove and continue
					subscriptions.RemoveAt(n);
					continue;
				}

				if (current.Subscriber?.Target == handlerTarget && current.Handler == methodInfo)
				{
					// Found the match, we can break
					subscriptions.RemoveAt(n);
					break;
				}
			}
		}

		private readonly struct Subscription : IEquatable<Subscription>
		{
			public Subscription(WeakReference? subscriber, MethodInfo handler)
			{
				Subscriber = subscriber;
				Handler = handler ?? throw new ArgumentNullException(nameof(handler));
			}

			public readonly WeakReference? Subscriber;
			public readonly MethodInfo Handler;

			public bool Equals(Subscription other) => Subscriber == other.Subscriber && Handler == other.Handler;

			public override bool Equals(object? obj) => obj is Subscription other && Equals(other);

			public override int GetHashCode() => Subscriber?.GetHashCode() ?? 0 ^ Handler.GetHashCode();
		}
	}
}
