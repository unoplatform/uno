// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutomationPeer.cpp, tag winui3/release/1.8.2

#nullable enable

using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class AutomationPeer
{
	//------------------------------------------------------------------------
	//
	//  Method:   HasKeyboardFocusHelper
	//
	//  Synopsis:
	//      Return if this AP has Focus or not, as SetFocusCore now considers enabling SetFocusCore for core CAutomationPeer
	//      which doesn't have corresponding UIElement, HasKeyBoardFocusHelper reflects that as well.
	//
	//------------------------------------------------------------------------
	private bool HasKeyboardFocusHelper()
	{
		if (!IsKeyboardFocusableCore())
		{
			return false;
		}

		var focusManager = GetFocusManagerNoRef();
		if (focusManager is not null)
		{
			var focusedAP = focusManager.GetFocusedAutomationPeer();
			if (this == focusedAP)
			{
				return true;
			}
		}

		return false;
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

		var currentFocusedAP = focusManager.GetFocusedAutomationPeer();
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
