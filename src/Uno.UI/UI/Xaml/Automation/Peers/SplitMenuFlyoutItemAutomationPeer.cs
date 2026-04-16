// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\SplitMenuFlyoutItemAutomationPeer_Partial.cpp, commit 5f9e85113

using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class SplitMenuFlyoutItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
{
	private const string c_primaryButtonAutomationId = "SplitMenuFlyoutItemPrimaryButton";

	public SplitMenuFlyoutItemAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke || patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(SplitMenuFlyoutItem);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

	protected override string GetAcceleratorKeyCore()
	{
		var returnValue = base.GetAcceleratorKeyCore();
		if (string.IsNullOrEmpty(returnValue))
		{
			var owner = Owner as SplitMenuFlyoutItem;
			if (owner is not null)
			{
				var keyboardAcceleratorTextOverride = owner.KeyboardAcceleratorTextOverride;
				returnValue = GetTrimmedKeyboardAcceleratorTextOverride(keyboardAcceleratorTextOverride);
			}
		}

		return returnValue;
	}

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			returnValue = MenuFlyoutPresenter.GetPositionInSetHelper((MenuFlyoutItemBase)Owner);
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
			returnValue = MenuFlyoutPresenter.GetSizeOfSetHelper((MenuFlyoutItemBase)Owner);
		}

		return returnValue;
	}

	protected override bool HasKeyboardFocusCore()
	{
		var primaryButtonPeer = GetPrimaryButtonPeer();
		if (primaryButtonPeer is not null)
		{
			return primaryButtonPeer.HasKeyboardFocus();
		}

		return base.HasKeyboardFocusCore();
	}

	protected override bool IsKeyboardFocusableCore()
	{
		var primaryButtonPeer = GetPrimaryButtonPeer();
		if (primaryButtonPeer is not null)
		{
			return primaryButtonPeer.IsKeyboardFocusable();
		}

		return base.IsKeyboardFocusableCore();
	}

	protected override AutomationPeer GetPeerFromPointCore(Point point)
	{
		var primaryButtonPeer = GetPrimaryButtonPeer();
		if (primaryButtonPeer is not null)
		{
			// Also set focus on the primary button programmatically.
			var owner = (SplitMenuFlyoutItem)Owner;
			owner.PrimaryButton?.Focus(FocusState.Programmatic);

			return primaryButtonPeer;
		}

		return base.GetPeerFromPointCore(point);
	}

	protected override Rect GetBoundingRectangleCore()
	{
		var primaryButtonPeer = GetPrimaryButtonPeer();
		if (primaryButtonPeer is not null)
		{
			return primaryButtonPeer.GetBoundingRectangle();
		}

		return base.GetBoundingRectangleCore();
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var children = base.GetChildrenCore();
		if (children is null)
		{
			return null;
		}

		// Filter out the primary button peer, identified by its AutomationId.
		var filteredChildren = new List<AutomationPeer>();
		foreach (var child in children)
		{
			if (child.GetAutomationId() != c_primaryButtonAutomationId)
			{
				filteredChildren.Add(child);
			}
		}

		return filteredChildren;
	}

	// IInvokeProvider
	public void Invoke()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		((SplitMenuFlyoutItem)Owner).Invoke();
	}

	// IExpandCollapseProvider
	void IExpandCollapseProvider.Expand() => ((SplitMenuFlyoutItem)Owner).Open();

	void IExpandCollapseProvider.Collapse() => ((SplitMenuFlyoutItem)Owner).Close();

	ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState => ((SplitMenuFlyoutItem)Owner).IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

	internal void RaiseExpandCollapseAutomationEvent(bool isOpen)
	{
		ExpandCollapseState oldValue;
		ExpandCollapseState newValue;
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

	private AutomationPeer GetPrimaryButtonPeer()
	{
		var owner = (SplitMenuFlyoutItem)Owner;
		var primaryButton = owner.PrimaryButton;
		if (primaryButton is not null)
		{
			return FrameworkElementAutomationPeer.CreatePeerForElement(primaryButton);
		}

		return null;
	}
}
