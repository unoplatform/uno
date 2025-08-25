#nullable enable

using System;

namespace Uno
{
	/// <summary>
	/// Internal helper to manage weak template-updated subscriptions consistently across controls.
	/// Must remain in Uno namespace to keep public surface stable.
	/// </summary>
	internal static class TemplateUpdateSubscription
	{
		private sealed class Subscription : IDisposable
		{
			public Microsoft.UI.Xaml.DataTemplate? Template { get; }
			private readonly IDisposable? _inner;
			public Subscription(Microsoft.UI.Xaml.DataTemplate? template, IDisposable? inner)
			{
				Template = template;
				_inner = inner;
			}
			public void Dispose() => _inner?.Dispose();
		}

		/// <summary>
		/// Ensure a subscription is set to the given template; returns true if the global update mode is enabled.
		/// </summary>
		public static bool Attach(
			Microsoft.UI.Xaml.DataTemplate? template,
			ref IDisposable? subscription,
			Action onUpdated)
		{
			if (!DataTemplateHelper.IsUpdateSubscriptionsEnabled)
			{
				return false;
			}

			var current = subscription as Subscription;

			if (!ReferenceEquals(template, current?.Template))
			{
				subscription?.Dispose();
				subscription = null;

				if (template is not null)
				{
					var inner = template.RegisterTemplateUpdated(onUpdated);
					subscription = new Subscription(template, inner);
				}
			}

			return true;
		}

		public static void Detach(ref IDisposable? subscription)
		{
			subscription?.Dispose();
			subscription = null;
		}
	}
}
