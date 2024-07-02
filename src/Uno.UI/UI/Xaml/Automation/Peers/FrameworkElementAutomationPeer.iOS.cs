using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;
using Uno;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers;

public partial class FrameworkElementAutomationPeer : AutomationPeer
{
	internal override bool AccessibilityActivate()
	{
		return InvokeAutomationPeer();
	}

	internal override bool UpdateAccessibilityElement()
	{
		if (!AutomationConfiguration.IsAccessibilityEnabled)
		{
			return false;
		}

		var isAccessible = AutomationProperties.GetAccessibilityView(Owner) != AccessibilityView.Raw;
		if (isAccessible && Owner is UIView view)
		{
			view.AccessibilityLabel = GetName();
			view.AccessibilityTraits = GetAccessibilityTraits();

			// TODO
			//view.AccessibilityHint
			//view.AccessibilityValue
			//view.AccessibilityElementsHidden
			//view.AccessibilityNavigationStyle
		}

		return isAccessible;
	}

	private UIAccessibilityTrait GetAccessibilityTraits()
	{
		// TODO: Use GetAutomationControlType() and GetPattern() instead

		UIAccessibilityTrait accessibilityTrait;

		switch (GetAutomationControlType())
		{
			case AutomationControlType.Button:
				accessibilityTrait = UIAccessibilityTrait.Button;
				break;
			case AutomationControlType.CheckBox:
			case AutomationControlType.RadioButton:
			case AutomationControlType.ComboBox: // ?
			case AutomationControlType.ListItem:
				// TODO: Shouldn't announce as button if not clickable/selectable
				accessibilityTrait = UIAccessibilityTrait.Button;
				break;
			case AutomationControlType.Image:
				accessibilityTrait = UIAccessibilityTrait.Image;
				break;
			case AutomationControlType.Text:
				accessibilityTrait = UIAccessibilityTrait.StaticText;
				break;
			case AutomationControlType.Hyperlink:
				accessibilityTrait = UIAccessibilityTrait.Link;
				break;
			case AutomationControlType.Header:
				accessibilityTrait = UIAccessibilityTrait.Header;
				break;
			case AutomationControlType.Slider:
				accessibilityTrait = UIAccessibilityTrait.Adjustable;
				break;
			default:
				accessibilityTrait = UIAccessibilityTrait.None;
				break;
		}

		if (Owner is SelectorItem selectorItem && selectorItem.IsSelected
		|| Owner is ToggleButton toggleButton && toggleButton.IsChecked == true
		|| Owner is ToggleSwitch toggleSwitch && toggleSwitch.IsOn)
		{
			accessibilityTrait |= UIAccessibilityTrait.Selected;
		}

		if (!IsEnabled())
		{
			accessibilityTrait |= UIAccessibilityTrait.NotEnabled;
		}

		return accessibilityTrait;
	}
}
