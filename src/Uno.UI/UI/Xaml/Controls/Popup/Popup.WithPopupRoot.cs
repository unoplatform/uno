using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation.Collections;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private readonly SerialDisposable _closePopup = new();

#if __ANDROID__
	private bool _useNativePopup = FeatureConfiguration.Popup.UseNativePopup;
	internal bool UseNativePopup => _useNativePopup;
#endif

	partial void InitializePartial()
	{
#if __ANDROID__
		if (_useNativePopup)
		{
			InitializeNativePartial();
		}
#endif

		PopupPanel = new PopupPanel(this);
	}

#if __ANDROID__
	partial void InitializeNativePartial();
#endif

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
#if __ANDROID__
		if (_useNativePopup)
		{
			OnIsLightDismissEnabledChangedNative(oldIsLightDismissEnabled, newIsLightDismissEnabled);
		}
		else
#endif
		{
			if (PopupPanel != null)
			{
				PopupPanel.Background = GetPanelBackground();
			}
		}
	}

#if __ANDROID__
	partial void OnIsLightDismissEnabledChangedNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled);
#endif

	partial void OnIsOpenChangedPartialNative(bool oldIsOpen, bool newIsOpen)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Popup.IsOpenChanged({oldIsOpen}, {newIsOpen})");
		}

#if __ANDROID__
		if (_useNativePopup)
		{
			OnIsOpenChangedNative(oldIsOpen, newIsOpen);
		}
		else
#endif
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

#if UNO_HAS_ENHANCED_LIFECYCLE
				// MUX Reference: Popup.cpp CPopupRoot::CompleteAdditionToOpenPopupList (lines 4289-4302)
				// After the popup is added to PopupRoot, propagate this popup's theme
				// to its child. The child was just reparented under PopupRoot (which has
				// Theme.None), so it needs the theme from its logical parent (this Popup).
				//
				// If this Popup itself doesn't yet have an inherited theme (it was created
				// outside the visual tree, e.g. a TextCommandBarFlyout materialised on
				// first long-press/double-tap and not yet visited by a theme walk), fall
				// back to the logical anchor's theme — PlacementTarget for ShowAt-style
				// flyouts, or AssociatedFlyout.Target as a secondary fallback. Without
				// this, the child measures and renders one frame against Themes.Active
				// (the application/OS theme), producing the visible mobile-context-menu
				// flash on first open before the theme walk catches up.
				var popupTheme = GetTheme();
				if (popupTheme == Theme.None)
				{
					if (PlacementTarget is FrameworkElement placementTarget)
					{
						popupTheme = placementTarget.GetTheme();
					}
					else if (AssociatedFlyout?.Target is FrameworkElement flyoutAnchor)
					{
						popupTheme = flyoutAnchor.GetTheme();
					}
				}
				if (popupTheme != Theme.None && Child is FrameworkElement feChild)
				{
					feChild.NotifyThemeChanged(popupTheme);

					// Some flyouts host their items in logical-only collections (Items,
					// PrimaryCommands, SecondaryCommands) rather than as visual children
					// of the popup.Child. Those items are still in the parse-time
					// resource scope and have {ThemeResource} bindings on Foreground,
					// Icon, etc., but they're NOT under the presenter's visual tree at
					// this point — the presenter has not yet applied its template, and
					// container materialisation happens during the upcoming layout pass.
					// The feChild.NotifyThemeChanged above walks visual descendants only,
					// so it never reaches them, and they render the first frame with the
					// brush they resolved against the application's global active theme
					// at XAML parse time (e.g. Dark while the popup itself is Light).
					// Notify each logical item directly so its theme bindings re-resolve
					// against the popup's theme before the first measure / render.
					// NotifyThemeChanged honours each item's own RequestedTheme override
					// and short-circuits on no-op, so this is safe for items that are
					// already correct, and the items still participate normally in the
					// subsequent template-driven theme walk once they enter the visual
					// tree.
					NotifyLogicalFlyoutItems(AssociatedFlyout, popupTheme);
				}
#endif

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

#if __ANDROID__
	partial void OnIsOpenChangedNative(bool oldIsOpen, bool newIsOpen);
#endif

	partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
	{
#if __ANDROID__
		if (_useNativePopup)
		{
			OnPopupPanelChangedPartialNative(previousPanel, newPanel);
		}
		else
#endif
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

#if __ANDROID__
	partial void OnPopupPanelChangedPartialNative(PopupPanel previousPanel, PopupPanel newPanel);
#endif

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

#if UNO_HAS_ENHANCED_LIFECYCLE
	// Notify flyout items that live in logical-only collections (MenuFlyout.Items,
	// CommandBarFlyout.PrimaryCommands / SecondaryCommands) of the popup's theme so
	// they re-resolve their {ThemeResource} bindings before the first measure.
	// See the long-form comment at the call site for the full rationale.
	private static void NotifyLogicalFlyoutItems(FlyoutBase flyout, Theme popupTheme)
	{
		switch (flyout)
		{
			case MenuFlyout menuFlyout when menuFlyout.Items is { } menuItems:
				foreach (var item in menuItems)
				{
					if (item is FrameworkElement feItem)
					{
						feItem.NotifyThemeChanged(popupTheme);
					}
				}
				break;
			case CommandBarFlyout commandBarFlyout:
				NotifyCommandBarElements(commandBarFlyout.PrimaryCommands, popupTheme);
				NotifyCommandBarElements(commandBarFlyout.SecondaryCommands, popupTheme);
				break;
		}
	}

	private static void NotifyCommandBarElements(IObservableVector<ICommandBarElement> commands, Theme popupTheme)
	{
		if (commands is null)
		{
			return;
		}

		foreach (var command in commands)
		{
			if (command is FrameworkElement feCommand)
			{
				feCommand.NotifyThemeChanged(popupTheme);
			}
		}
	}
#endif
}
