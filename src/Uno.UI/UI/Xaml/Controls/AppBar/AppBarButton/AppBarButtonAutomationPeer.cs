// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AppBarButtonAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AppBarButton types to Microsoft UI Automation.
/// </summary>
public partial class AppBarButtonAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
{
	public AppBarButtonAutomationPeer(AppBarButton owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		var owner = GetOwningAppBarButton();
		object ppReturnValue = null;

		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			// We specifically want to *not* report that we support the expand/collapse pattern when we don't have an attached flyout,
			// because then we have nothing to expand or collapse.  So we unconditionally enter this block even if owner->m_menuHelper
			// is null so we'll get the default null return value in that case.
			if (owner.m_menuHelper is { } menuHelper)
			{
				ppReturnValue = this;
			}
		}
		else
		{
			ppReturnValue = base.GetPatternCore(patternInterface);
		}

		return ppReturnValue;
	}

	protected override string GetClassNameCore() => nameof(AppBarButton);

	protected override string GetNameCore()
	{
		// Note: We are calling FrameworkElementAutomationPeer::GetNameCore here, rather than going through
		// any of our own immediate superclasses, to avoid the logic in ButtonBaseAutomationPeer that will
		// substitute Content for the automation name if the latter is unset -- we want to either get back
		// the actual value of AutomationProperties.Name if it has been set, or null if it hasn't.

		//UNO ONLY: ButtonBaseAutomationPeer doesn't substitue Content for automation name if it is unset
		var returnValue = base.GetNameCore();

		if (string.IsNullOrWhiteSpace(returnValue))
		{
			var owner = GetOwningAppBarButton();
			returnValue = owner.Label;
		}

		return returnValue;
	}

	protected override string GetLocalizedControlTypeCore() =>
		DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_APPBAR_BUTTON");

	protected override string GetAcceleratorKeyCore()
	{
		var returnValue = base.GetAcceleratorKeyCore();

		if (!string.IsNullOrWhiteSpace(returnValue))
		{
			// If AutomationProperties.AcceleratorKey hasn't been set, then return the value of our
			// KeyboardAcceleratorTextOverride property.
			var owner = GetOwningAppBarButton();
			var keyboardAcceleratorTextOverride = owner.KeyboardAcceleratorTextOverride;
			returnValue = AutomationPeer.GetTrimmedKeyboardAcceleratorTextOverride(keyboardAcceleratorTextOverride);
		}

		return returnValue;
	}

	protected override bool IsKeyboardFocusableCore()
	{
		var owner = GetOwningAppBarButton();

		var parentCommandBar = CommandBar.FindParentCommandBarForElement(owner);

		if (parentCommandBar != null)
		{
			return AppBarButtonHelpers<AppBarButton>.IsKeyboardFocusable(owner);
		}
		else
		{
			return base.IsKeyboardFocusableCore();
		}
	}

	/// <summary>
	/// Displays all child nodes, controls, or content of the control.
	/// </summary>
	public void Expand()
	{
		var owner = GetOwningAppBarButton();
		if (owner.m_menuHelper is { } menuHelper)
		{
			menuHelper.OpenSubMenu();
		}
	}

	/// <summary>
	/// Hides all nodes, controls, or content that are descendants of the control.
	/// </summary>
	public void Collapse()
	{
		var owner = GetOwningAppBarButton();
		if (owner.m_menuHelper is { } menuHelper)
		{
			menuHelper.CloseSubMenu();
		}
	}

	/// <summary>
	/// Gets the state, expanded or collapsed, of the control.
	/// </summary>
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var isOpen = false;
			var owner = GetOwningAppBarButton();
			if (owner.m_menuHelper is { })
			{
				isOpen = ((ISubMenuOwner)owner).IsSubMenuOpen;
			}

			return isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
		}
	}

	// Raise events for ExpandCollapseState changes to UIAutomation Clients.
	internal void RaiseExpandCollapseAutomationEvent(bool isOpen)
	{
		ExpandCollapseState oldValue;
		ExpandCollapseState newValue;

		// Converting isOpen to appropriate enumerations
		if (isOpen)
		{
			oldValue = ExpandCollapseState.Collapsed;
			newValue = ExpandCollapseState.Expanded;
		}
		else
		{
			oldValue = ExpandCollapseState.Expanded;
			newValue = ExpandCollapseState.Collapsed;
		}

		RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldValue, newValue);
	}

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			var owner = GetOwningAppBarButton();
			returnValue = CommandBar.GetPositionInSetStatic(owner);
		}

		return returnValue;
	}

	protected override int GetSizeOfSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetSizeOfSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			var owner = GetOwningAppBarButton();
			returnValue = CommandBar.GetSizeOfSetStatic(owner);
		}

		return returnValue;
	}

	private AppBarButton GetOwningAppBarButton() => Owner as AppBarButton;
}
