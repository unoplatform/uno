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
	internal static class TemplateManager
	{
		/// <summary>
		/// If true, DataTemplate update can be activated.
		/// </summary>
		/// <remarks>
		/// Turned to false by the linker in `Release` build.
		/// </remarks>
		public static bool IsDataTemplateDynamicUpdateEnabled { get; } = true;

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
		public static void EnableUpdateSubscriptions()
		{
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				IsUpdateSubscriptionsEnabled = true;
			}
		}

		/// <summary>
		/// Subscribe to dynamic updates for the specified DataTemplate and associate the subscription with an owner control.
		/// </summary>
		public static bool SubscribeToTemplate(
			DependencyObject owner,
			DataTemplate? template,
			Action onUpdated)
		{
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				return TemplateUpdateSubscription.Attach(owner, template, onUpdated);
			}

			return false;
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
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				return TemplateUpdateSubscription.Attach(owner, slotKey, template, onUpdated);
			}

			return false;
		}


		/// <summary>
		/// Unsubscribe all owner-associated template update subscriptions.
		/// </summary>
		public static void UnsubscribeFromTemplate(DependencyObject owner)
		{
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				TemplateUpdateSubscription.Detach(owner);
			}
		}

		/// <summary>
		/// Unsubscribe a specific named slot subscription for the owner.
		/// </summary>
		public static void UnsubscribeFromTemplate(DependencyObject owner, string slotKey)
		{
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				TemplateUpdateSubscription.Detach(owner, slotKey);
			}
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
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				ArgumentNullException.ThrowIfNull(currentTemplate);
				ArgumentNullException.ThrowIfNull(factoryUpdater);

				// Update and notify
				return currentTemplate.UpdateFactory(factoryUpdater);
			}

			return false;
		}

		/// <summary>
		/// Updates the factory of the provided <see cref="Microsoft.UI.Xaml.DataTemplate"/> and raises an update notification.
		/// </summary>
		/// <returns>
		/// True if the template was updated successfully.
		/// </returns>
		public static bool UpdateDataTemplate(DataTemplate currentTemplate, Func<View?> newViewfactory)
		{
			if (IsDataTemplateDynamicUpdateEnabled)
			{
				ArgumentNullException.ThrowIfNull(currentTemplate);
				ArgumentNullException.ThrowIfNull(newViewfactory);

				return currentTemplate.UpdateFactory(_ => (NewFrameworkTemplateBuilder)((_, _) => newViewfactory()));
			}

			return false;
		}
	}
}
