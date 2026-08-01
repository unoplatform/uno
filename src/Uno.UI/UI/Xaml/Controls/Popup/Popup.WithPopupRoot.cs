using Uno.Extensions;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private readonly SerialDisposable _closePopup = new();

	partial void InitializePartial()
	{
		PopupPanel = new PopupPanel(this);
	}

	partial void OnChildChangedPartialNative(UIElement oldChild, UIElement newChild)
	{
		PopupPanel.Children.Remove(oldChild);

		if (newChild != null)
		{
			PopupPanel.Children.Add(newChild);
		}
	}

	partial void OnIsLightDismissEnabledChangedPartialNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
	{
		{
			if (PopupPanel != null)
			{
				PopupPanel.Background = GetPanelBackground();
			}
		}
	}

	partial void OnIsOpenChangedPartialNative(bool oldIsOpen, bool newIsOpen)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Popup.IsOpenChanged({oldIsOpen}, {newIsOpen})");
		}

		{
			if (newIsOpen)
			{
				// It's important for PopupPanel to be visible before the popup is opened so that
				// child controls can be IsFocusable, which depends on all ancestors (including PopupPanel)
				// being visible
				PopupPanel.Visibility = Visibility.Visible;

				var currentXamlRoot = XamlRoot ?? Child?.XamlRoot ?? WinUICoreServices.Instance.ContentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot?.XamlRoot;

				// When a popup is reused across different windows (e.g., shared
				// TextCommandBarFlyout), the PopupPanel and its children may retain
				// stale VisualTreeCache entries from the previous window. Propagate
				// the correct VisualTree before opening so that layout, hit-testing,
				// and XamlRoot queries all use the correct window context.
				// WinUI does this in CDependencyObject::EnterImpl (SetVisualTree on
				// live-tree enter), but Uno hasn't ported that path yet.
				if (currentXamlRoot?.VisualTree is { } targetTree)
				{
					EnsureVisualTreeOnSubtree(PopupPanel, targetTree);
				}

				_closePopup.Disposable = currentXamlRoot?.OpenPopup(this);
			}
			else
			{
				_closePopup.Disposable = null;
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		if (newIsOpen)
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			// TODO: Add EventManager.RaiseEvent method and use it here.
			NativeDispatcher.Main.Enqueue(() => Opened?.Invoke(this, newIsOpen), NativeDispatcherPriority.Normal);
#else
			Opened?.Invoke(this, newIsOpen);
#endif
		}
		else
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			NativeDispatcher.Main.Enqueue(() => Closed?.Invoke(this, newIsOpen), NativeDispatcherPriority.Normal);
#else
			Closed?.Invoke(this, newIsOpen);
#endif
		}
	}

	partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
	{
		{
			previousPanel?.Children.Clear();

			if (newPanel != null)
			{
				if (Child != null)
				{
					newPanel.Children.Add(Child);
				}
				newPanel.Background = GetPanelBackground();
			}
		}
	}

	/// <summary>
	/// Recursively ensures the <paramref name="element"/> and all its visual children
	/// have their <see cref="UIElement.VisualTreeCache"/> set to <paramref name="targetTree"/>.
	/// Short-circuits when a subtree already has the correct value.
	/// </summary>
	private static void EnsureVisualTreeOnSubtree(UIElement element, VisualTree targetTree)
	{
		if (element.GetVisualTree() == targetTree)
		{
			return;
		}

		element.SetVisualTree(targetTree);

		var count = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < count; i++)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				EnsureVisualTreeOnSubtree(child, targetTree);
			}
		}
	}
}
