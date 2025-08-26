#nullable enable

using System;
using Microsoft.UI.Xaml;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Uno
{
	/// <summary>
	/// Manager for runtime DataTemplate updates and notification system.
	/// Also exposes a global flag to enable template-update subscriptions in controls.
	/// </summary>
	public static class TemplateManager
	{
		/// <summary>
		/// When true, controls will subscribe to DataTemplate update notifications and refresh their materialized content.
		/// Default is false to opt-in explicitly.
		/// </summary>
		public static bool IsUpdateSubscriptionsEnabled { get; private set; }

		/// <summary>
		/// [DEV TOOLING] Enables the DataTemplate update subscriptions.
		/// </summary>
		/// <remarks>
		/// This should be called at application startup, before any DataTemplate is created or used.
		/// DEV TOOLING: SHOULD NOT BE USED IN PRODUCTION APPLICATION CODE.
		/// </remarks>
		public static void EnableUpdateSubscriptions() => IsUpdateSubscriptionsEnabled = true;

		/// <summary>
		/// Subscribe to dynamic updates for the specified DataTemplate
		/// </summary>
		/// <param name="template">The DataTemplate to monitor for updates</param>
		/// <param name="subscription">Reference to store the subscription (will be disposed/replaced as needed)</param>
		/// <param name="onUpdated">Callback to invoke when the template is updated</param>
		/// <returns>True if the subscription was created (when dynamic updates are enabled)</returns>
		/// <remarks>
		/// Use this method in custom controls that materialize DataTemplate content and want to 
		/// automatically refresh when the template is updated at runtime
		/// </remarks>
		public static bool SubscribeToTemplate(
			DataTemplate? template,
			ref IDisposable? subscription,
			Action onUpdated)
		{
			return TemplateUpdateSubscription.Attach(template, ref subscription, onUpdated);
		}

		/// <summary>
		/// Unsubscribe from DataTemplate updates
		/// </summary>
		/// <param name="subscription">The subscription to dispose</param>
		public static void UnsubscribeFromTemplate(ref IDisposable? subscription)
		{
			subscription?.Dispose();
			subscription = null;
		}

		/// <summary>
		/// Updates the factory of the provided <see cref="Microsoft.UI.Xaml.DataTemplate"/> and raises an update notification.
		/// Returns the previous factory so callers can reuse it if needed for special cases.
		/// </summary>
		public static bool UpdateDataTemplate(DataTemplate currentTemplate,
			Func<NewFrameworkTemplateBuilder?, NewFrameworkTemplateBuilder?> factoryUpdater)
		{
			ArgumentNullException.ThrowIfNull(currentTemplate);
			ArgumentNullException.ThrowIfNull(factoryUpdater);

			// Update and notify
			return currentTemplate.UpdateFactory(factoryUpdater);
		}

		public static bool UpdateDataTemplate(DataTemplate currentTemplate, Func<View?> newViewfactory)
		{
			ArgumentNullException.ThrowIfNull(currentTemplate);
			ArgumentNullException.ThrowIfNull(newViewfactory);

			return currentTemplate.UpdateFactory(_ => (NewFrameworkTemplateBuilder)((_, _) => newViewfactory()));
		}
	}
}
