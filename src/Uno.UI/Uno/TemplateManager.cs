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

namespace Uno.UI
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
		/// Subscribe to dynamic updates for the specified DataTemplate and associate the subscription with an owner control.
		/// </summary>
		public static bool SubscribeToTemplate(
			DependencyObject owner,
			DataTemplate? template,
			Action onUpdated)
		{
			return TemplateUpdateSubscription.Attach(owner, template, onUpdated);
		}

		/// <summary>
		/// Subscribe to dynamic updates for the specified DataTemplate using a named slot. Allows multiple subscriptions per owner.
		/// </summary>
		public static bool SubscribeToTemplate(
			DependencyObject owner,
			string slotKey,
			DataTemplate? template,
			Action onUpdated)
		{
			return TemplateUpdateSubscription.Attach(owner, slotKey, template, onUpdated);
		}


		/// <summary>
		/// Unsubscribe all owner-associated template update subscriptions.
		/// </summary>
		public static void UnsubscribeFromTemplate(DependencyObject owner)
		{
			TemplateUpdateSubscription.Detach(owner);
		}

		/// <summary>
		/// Unsubscribe a specific named slot subscription for the owner.
		/// </summary>
		public static void UnsubscribeFromTemplate(DependencyObject owner, string slotKey)
		{
			TemplateUpdateSubscription.Detach(owner, slotKey);
		}

		/// <summary>
		/// Updates the factory of the provided <see cref="Microsoft.UI.Xaml.DataTemplate"/> and raises an update notification.
		/// </summary>
		/// <returns>
		/// True if the template was updated successfully.
		/// </returns>
		public static bool UpdateDataTemplate(DataTemplate currentTemplate,
			Func<NewFrameworkTemplateBuilder?, NewFrameworkTemplateBuilder?> factoryUpdater)
		{
			ArgumentNullException.ThrowIfNull(currentTemplate);
			ArgumentNullException.ThrowIfNull(factoryUpdater);

			// Update and notify
			return currentTemplate.UpdateFactory(factoryUpdater);
		}

		/// <summary>
		/// Updates the factory of the provided <see cref="Microsoft.UI.Xaml.DataTemplate"/> and raises an update notification.
		/// </summary>
		/// <returns>
		/// True if the template was updated successfully.
		/// </returns>
		public static bool UpdateDataTemplate(DataTemplate currentTemplate, Func<View?> newViewfactory)
		{
			ArgumentNullException.ThrowIfNull(currentTemplate);
			ArgumentNullException.ThrowIfNull(newViewfactory);

			return currentTemplate.UpdateFactory(_ => (NewFrameworkTemplateBuilder)((_, _) => newViewfactory()));
		}
	}
}
