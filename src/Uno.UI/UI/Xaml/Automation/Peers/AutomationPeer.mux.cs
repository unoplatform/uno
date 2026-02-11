// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutomationPeer.cpp, tag winui3/release/1.8.2

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class AutomationPeer
{
	// Cached events source for ListItem/TabItem/TreeItem control types
	private AutomationPeer? _eventsSource;

	//------------------------------------------------------------------------
	//
	//  Method:   GetAPEventsSource
	//
	//  Synopsis:
	//      Returns the EventsSource corresponding to current AutomationPeer.
	//      EventsSource is the real peer which goes to client and should be
	//      considered source of events raised on this peer.
	//      This is particularly important for ListViewItem/TabItem/TreeItem.
	//
	//------------------------------------------------------------------------
	internal AutomationPeer? GetAPEventsSource()
	{
		var controlType = GetAutomationControlType();

		// Only generate EventsSource for ListItem, TabItem, and TreeItem control types
		if (controlType == AutomationControlType.ListItem ||
			controlType == AutomationControlType.TabItem ||
			controlType == AutomationControlType.TreeItem)
		{
			// Navigate to parent to generate EventsSource
			var parent = Navigate(AutomationNavigationDirection.Parent) as AutomationPeer;

			if (parent is not null && this is FrameworkElementAutomationPeer)
			{
				// Generate the events source using the parent
				GenerateAutomationPeerEventsSource(parent);

				if (_eventsSource is not null)
				{
					// This code path exists for one reason: for ListViewItems we need to make sure that
					// their DataAutomationPeers, which are the EventsSource for the actual FrameworkElement
					// derived AutomationPeers, return the correct parent. In List/ListItem peer implementations
					// we hide all the controls between the ListViewItem and the ListView control.
					//
					// We make sure here that if m_pAPParent is already set, we leave it alone and don't override it.
					// That indicates external code is trying to set the parent on a data automation peer.
					// If not we perform this call, which does the same thing, but for internal automation peers.
					if (!_eventsSource.HasParent())
					{
						_eventsSource.SetParent(parent);
					}
				}
			}
		}

		// Return the cached EventsSource, or EventsSource property if set
		return _eventsSource ?? EventsSource;
	}

	/// <summary>
	/// Sets the events source for this automation peer.
	/// Used internally when generating EventsSource for list-like items.
	/// </summary>
	internal void SetAPEventsSource(AutomationPeer? eventsSource)
	{
		_eventsSource = eventsSource;
	}

	/// <summary>
	/// Returns whether this AutomationPeer has a parent set.
	/// </summary>
	internal bool HasParent() => _parent is not null;

	//------------------------------------------------------------------------
	//
	//  Method:   GetLogicalAPParent
	//
	//  Synopsis:
	//      Returns the logical automation peer parent. If we don't currently
	//      have an m_pAPParent set, then we will attempt to walk the logical
	//      tree to get the parent.
	//
	//------------------------------------------------------------------------
	internal AutomationPeer? GetLogicalAPParent()
	{
		// If we already have a parent set, return it
		if (_parent is not null)
		{
			return _parent;
		}

		// Get the owner element if this is a FrameworkElementAutomationPeer
		DependencyObject? ownerDO = null;
		if (this is FrameworkElementAutomationPeer feap)
		{
			ownerDO = feap.Owner as DependencyObject;
		}

		if (ownerDO is null)
		{
			return null;
		}

		AutomationPeer? foundPeer = null;
		DependencyObject? current = ownerDO;

		// Keep finding the logical AP parent
		while (foundPeer is null && current is not null)
		{
			// Check if parent is a Popup - needs special handling
			if (current is UIElement uiElement)
			{
				foundPeer = GetPopupAssociatedAutomationPeer(uiElement);
			}

			if (foundPeer is null)
			{
				// Try to get the logical parent first (handles cases like ItemsControl items)
				DependencyObject? logicalParent = GetLogicalParentForAP(current);

				if (logicalParent is not null)
				{
					current = logicalParent;
				}
				else
				{
					// Fall back to visual tree parent
					current = VisualTreeHelper.GetParent(current);
				}

				// If we found a parent, try to get its AutomationPeer
				if (current is UIElement parentElement)
				{
					foundPeer = parentElement.GetOrCreateAutomationPeer();
				}
			}
		}

		return foundPeer;
	}

	/// <summary>
	/// Gets the automation peer associated with a Popup's logical parent.
	/// Handles the special case where the element is inside a Popup's visual tree.
	/// </summary>
	private static AutomationPeer? GetPopupAssociatedAutomationPeer(UIElement element)
	{
		// Walk up the visual tree to find if we're inside a Popup's panel
		DependencyObject? current = element;
		while (current is not null)
		{
			var parent = VisualTreeHelper.GetParent(current);

			// If parent is a PopupPanel, get the Popup and find its logical parent
			if (parent is PopupPanel popupPanel)
			{
				var popup = popupPanel.Popup;
				if (popup is not null)
				{
					// Get the parent of the Popup in the logical tree
					var popupParent = VisualTreeHelper.GetParent(popup) as UIElement;
					if (popupParent is not null)
					{
						return popupParent.GetOrCreateAutomationPeer();
					}
				}
				return null;
			}

			// If we hit the root, stop
			if (parent is PopupRoot)
			{
				return null;
			}

			current = parent;
		}

		return null;
	}

	/// <summary>
	/// Gets the logical parent for automation purposes.
	/// This handles special cases like ItemsControl where the logical parent
	/// differs from the visual parent.
	/// </summary>
	private static DependencyObject? GetLogicalParentForAP(DependencyObject element)
	{
		// Check if the element has an ItemsControl parent (for items in lists)
		if (element is FrameworkElement fe)
		{
			// For items in an ItemsControl, the logical parent is the ItemsControl itself
			var itemsOwner = ItemsControl.ItemsControlFromItemContainer(fe);
			if (itemsOwner is not null)
			{
				return itemsOwner;
			}
		}

		return null;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   HasKeyboardFocusImpl
	//
	//  Synopsis:
	//      Implementation that checks if this AP has Focus.
	//      Used by HasKeyboardFocusCore to provide the actual logic.
	//
	//------------------------------------------------------------------------
	private bool HasKeyboardFocusImpl()
	{
		if (!IsKeyboardFocusableCore())
		{
			return false;
		}

		var focusManager = GetFocusManagerNoRef();
		if (focusManager is not null)
		{
			var focusedAP = FocusManager.GetFocusedAutomationPeer();
			if (this == focusedAP)
			{
				return true;
			}
		}

		return false;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   IsKeyboardFocusableImpl
	//
	//  Synopsis:
	//      Returns whether this element can receive keyboard focus.
	//      For FrameworkElementAutomationPeer, this checks the Control's
	//      IsTabStop and IsEnabled properties.
	//
	//------------------------------------------------------------------------
	private bool IsKeyboardFocusableImpl()
	{
		// Get the owner element if this is a FrameworkElementAutomationPeer
		if (this is FrameworkElementAutomationPeer feap && feap.Owner is UIElement owner)
		{
			// Check if the element is visible
			if (owner.Visibility != Visibility.Visible)
			{
				return false;
			}

			// For Controls, check IsTabStop and IsEnabled
			if (owner is Control control)
			{
				return control.IsTabStop && control.IsEnabled;
			}

			// For other focusable elements, check if they can receive focus
			// AllowFocusOnInteraction is on FrameworkElement, not UIElement
			if (owner is FrameworkElement fe && fe.AllowFocusOnInteraction)
			{
				return true;
			}
		}

		return false;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   IsOffscreenImpl
	//
	//  Synopsis:
	//      Returns whether this element is offscreen (not visible to the user).
	//      This considers visibility, opacity, and clipping.
	//
	//------------------------------------------------------------------------
	private bool IsOffscreenImpl(bool ignoreClippingOnScrollContentPresenters)
	{
		//TODO (DOTI): ActualWidth/Heigh doesn't seem to be supported on some platforms
#if __SKIA__
		// Get the owner element if this is a FrameworkElementAutomationPeer
		if (this is FrameworkElementAutomationPeer feap && feap.Owner is UIElement owner)
		{
			// Check visibility
			if (owner.Visibility != Visibility.Visible)
			{
				return true;
			}

			// Check opacity
			if (owner.Opacity == 0)
			{
				return true;
			}

			// Check if element has zero size
			if (owner is FrameworkElement fe)
			{
				if (fe.ActualWidth == 0 || fe.ActualHeight == 0)
				{
					return true;
				}
			}

			// Check if element is clipped out of view
			// Get the bounding rectangle - if it's empty, the element is offscreen
			var bounds = owner.GetGlobalBoundsWithOptions(
				ignoreClipping: false,
				ignoreClippingOnScrollContentPresenters: ignoreClippingOnScrollContentPresenters,
				useTargetInformation: false);

			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return true;
			}

			return false;
		}
#endif

		return false;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   GetAutomationIdHelper
	//
	//  Synopsis:
	//      If AutomationId is not set, FrameworkElementAP uses the element's
	//      Name as AutomationID as a fallback.
	//
	//------------------------------------------------------------------------
	internal string? GetAutomationIdHelper()
	{
		//TODO (DOTI): ActualWidth/Heigh doesn't seem to be supported on some platforms
#if __SKIA__
		// Get the owner element if this is a FrameworkElementAutomationPeer
		if (this is FrameworkElementAutomationPeer feap && feap.Owner is FrameworkElement owner)
		{
			// First check if AutomationId is explicitly set
			var automationId = AutomationProperties.GetAutomationId(owner);
			if (!string.IsNullOrEmpty(automationId))
			{
				return automationId;
			}

			// Fall back to the element's Name property
			if (!string.IsNullOrEmpty(owner.Name.ToString()))
			{
				return owner.Name.ToString();
			}
		}
#endif

		return null;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   SetFocusHelper
	//
	//  Synopsis:
	//      If this is Focusable and not the current focused element, then we should be able to treat it as focused.
	//      It basically supports the cases for Application Devs while they create Custom AutomationPeer for non-UIElement controls.
	//      It also enables showing Soft keyboard during IHM scenarios in such cases as needed.
	//
	//------------------------------------------------------------------------
	private void SetFocusHelper()
	{
		if (!IsKeyboardFocusableCore())
		{
			return;
		}

		var focusManager = GetFocusManagerNoRef();
		if (focusManager is null)
		{
			return;
		}

		var currentFocusedAP = FocusManager.GetFocusedAutomationPeer();
		if (this == currentFocusedAP)
		{
			return;
		}

#if HAS_UNO
		// TODO Uno: Implement UIAClientsAreListening check when UIA infrastructure is available.
		// In WinUI this checks: if (S_OK == GetContext()->UIAClientsAreListening(UIAXcp::AEAutomationFocusChanged))
		// For now, we proceed unconditionally.

		var apToRaiseEvent = EventsSource ?? this;

		focusManager.SetFocusedAutomationPeer(this);
		apToRaiseEvent.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
#endif
	}

	//------------------------------------------------------------------------
	//
	//  Method:   SetAutomationFocusHelper
	//
	//  Synopsis:
	//     We want this method to set automation focus, but not keyboard focus, to the element that this is an automation peer of.
	//     AutomationPeer's override of SetFocusHelper() happens to do exactly this, so we'll just use its implementation.
	//     (We can't just call SetFocusHelper(), because would likely be overridden by CFrameworkElementAutomationPeer.)
	//     The motivation behind this is, for example, Pivot, where keyboard focus is given to the header panel to enable
	//     keyboarding scenarios, but we want to act as though individual PivotItems have focus for the purposes of what we
	//     report to UIA clients, since they require automation focus to be given to elements before they can read them
	//     and report their contents.
	//
	//------------------------------------------------------------------------
	private void SetAutomationFocusHelper()
	{
		// Call the base AutomationPeer's SetFocusHelper directly to avoid
		// subclass overrides that might set actual keyboard focus.
		SetFocusHelper();
	}

	/// <summary>
	/// Helper method to check if any UIA listeners exist for the specified event.
	/// </summary>
	internal static bool ListenerExistsHelper(AutomationEvents eventId)
#if __SKIA__
		=> AutomationPeerListener?.ListenerExistsHelper(eventId) == true;
#else
		=> true;
#endif

	/// <summary>
	/// Removes the leading and trailing spaces in the provided string and returns the trimmed version
	/// or an empty string when no characters are left.
	/// Because it is recommended to set an AppBarButton, AppBarToggleButton, MenuFlyoutItem or ToggleMenuFlyoutItem's
	/// KeyboardAcceleratorTextOverride to a single space to hide their keyboard accelerator UI, this trimming method
	/// prevents automation tools like Narrator from emitting a space when navigating to such an element.
	/// </summary>
	internal static string GetTrimmedKeyboardAcceleratorTextOverride(string? keyboardAcceleratorTextOverride)
		=> keyboardAcceleratorTextOverride?.Trim() ?? string.Empty;

	private FocusManager? GetFocusManagerNoRef()
	{
		// TODO Uno: In WinUI this uses GetContext() and GetRootNoRef() pattern.
		// For Uno, we use the VisualTree helper.
		if (this is FrameworkElementAutomationPeer feap && feap.Owner is DependencyObject owner)
		{
			return VisualTree.GetFocusManagerForElement(owner);
		}

		return null;
	}

	internal DependencyObject GetRootNoRef()
	{
		// Minimal implementation: return the owner element if available.
		if (this is FrameworkElementAutomationPeer feap && feap.Owner is DependencyObject owner)
		{
			return owner;
		}

		// Fallback: return a concrete lightweight element so callers can query the visual tree safely.
		// Border is a simple FrameworkElement implementation available in the tree.
		return new Microsoft.UI.Xaml.Controls.Border();
	}

	//------------------------------------------------------------------------
	//
	//  Method:   RaiseAutomaticPropertyChanges
	//
	//  Synopsis:
	//      Raises events for each of the properties that are automagic
	//
	//------------------------------------------------------------------------
	internal void RaiseAutomaticPropertyChanges(bool firePropertyChangedEvents)
	{
#if HAS_UNO
		var isEnabled = IsEnabledCore();
		var isOffscreen = IsOffscreenCore();
		var name = GetNameCore();
		var itemStatus = GetItemStatusCore();

		if (firePropertyChangedEvents)
		{
			if (_currentIsEnabled != isEnabled)
			{
				RaisePropertyChangedEvent(
					AutomationElementIdentifiers.IsEnabledProperty,
					_currentIsEnabled,
					isEnabled);
				_currentIsEnabled = isEnabled;
			}

			if (_currentIsOffscreen != isOffscreen)
			{
				RaisePropertyChangedEvent(
					AutomationElementIdentifiers.IsOffscreenProperty,
					_currentIsOffscreen,
					isOffscreen);
				_currentIsOffscreen = isOffscreen;
			}

			if (_currentName != name && (name is not null || _currentName is not null))
			{
				RaisePropertyChangedEvent(
					AutomationElementIdentifiers.NameProperty,
					_currentName ?? string.Empty,
					name ?? string.Empty);
				_currentName = name;
			}

			if (_currentItemStatus != itemStatus && (itemStatus is not null || _currentItemStatus is not null))
			{
				RaisePropertyChangedEvent(
					AutomationElementIdentifiers.ItemStatusProperty,
					_currentItemStatus ?? string.Empty,
					itemStatus ?? string.Empty);
				_currentItemStatus = itemStatus;
			}
		}
		else
		{
			_currentIsEnabled = isEnabled;
			_currentIsOffscreen = isOffscreen;
			_currentName = name;
			_currentItemStatus = itemStatus;
		}
#endif
	}
}
