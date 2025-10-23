#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Uno.UI
{
	/// <summary>
	/// Internal helper to manage weak template-updated subscriptions consistently across controls.
	/// Must remain in Uno namespace to keep public surface stable.
	/// </summary>
	internal static class TemplateUpdateSubscription
	{
		private sealed class Subscription : IDisposable
		{
			public DataTemplate? Template { get; }
			private readonly IDisposable? _inner;

			public Subscription(DataTemplate? template, IDisposable? inner)
			{
				Template = template;
				_inner = inner;
			}
			public void Dispose() => _inner?.Dispose();
		}

		private sealed class OwnerState
		{
			public Dictionary<string, Subscription> Slots { get; } = new(StringComparer.Ordinal);
		}

		private static readonly ConditionalWeakTable<DependencyObject, OwnerState> _byOwner = new();

		/// <summary>
		/// Owner-based attach using a default slot key. Avoids the need for callers to keep a ref subscription.
		/// </summary>
		public static bool Attach(DependencyObject owner, DataTemplate? template, Action onUpdated)
			=> Attach(owner, DefaultSlot, template, onUpdated);

		private const string DefaultSlot = "__default__";

		/// <summary>
		/// Ensure a subscription is set to the given template; returns true if the global update mode is enabled.
		/// Owner-based attach into a named slot, allowing multiple subscriptions per owner.
		/// </summary>
		public static bool Attach(DependencyObject owner, string slotKey, DataTemplate? template, Action onUpdated)
		{
			if (!TemplateManager.IsUpdateSubscriptionsEnabled)
			{
				return false;
			}

			if (owner is null)
			{
				throw new ArgumentNullException(nameof(owner));
			}

			if (slotKey is null)
			{
				throw new ArgumentNullException(nameof(slotKey));
			}

			var state = _byOwner.GetOrCreateValue(owner);

			if (state.Slots.TryGetValue(slotKey, out var current))
			{
				if (ReferenceEquals(template, current.Template))
				{
					return true; // Nothing to change
				}

				current.Dispose();
				state.Slots.Remove(slotKey);
			}

			if (template is not null)
			{
				var inner = template.RegisterTemplateUpdated(onUpdated);
				state.Slots[slotKey] = new Subscription(template, inner);
			}

			return true;
		}


		/// <summary>
		/// Unsubscribe all owner-associated template update subscriptions.
		/// </summary>
		public static void Detach(DependencyObject owner)
		{
			if (owner is null)
			{
				return;
			}

			if (_byOwner.TryGetValue(owner, out var state))
			{
				foreach (var kvp in state.Slots)
				{
					kvp.Value.Dispose();
				}
				state.Slots.Clear();
			}
		}

		public static void Detach(DependencyObject owner, string slotKey)
		{
			if (owner is null || slotKey is null)
			{
				return;
			}

			if (_byOwner.TryGetValue(owner, out var state))
			{
				if (state.Slots.Remove(slotKey, out var sub))
				{
					sub.Dispose();
				}
			}
		}
	}
}
