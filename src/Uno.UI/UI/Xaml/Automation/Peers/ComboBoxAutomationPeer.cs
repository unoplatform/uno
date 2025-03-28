// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ComboBoxAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ComboBox types to Microsoft UI Automation.
/// </summary>
public partial class ComboBoxAutomationPeer : SelectorAutomationPeer, Provider.IExpandCollapseProvider, Provider.IValueProvider, Provider.IWindowProvider
{
	public ComboBoxAutomationPeer(ComboBox owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		var pOwner = (ComboBox)Owner;

		switch (patternInterface)
		{
			case PatternInterface.Value:
				if (pOwner.IsEditable)
				{
					return this;
				}
				break;
			case PatternInterface.ExpandCollapse:
				return this;

			case PatternInterface.Window:
				if (pOwner.IsDropDownOpen)
				{
					return this;
				}
				break;
			default:
				return base.GetPatternCore(patternInterface);
		}

		return null;
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		return base.GetChildrenCore();

		//UNO TODO: Implement GetLightDismissElement on ComboBox
		//UIElement spOwner = Owner as UIElement;
		//ComboBox spComboBox = spOwner as ComboBox;

		//if (spComboBox == null || !spComboBox.IsDropDownOpen)
		//{
		//	return base.GetChildrenCore();
		//}

		//IList<AutomationPeer> apChildren = new DirectUI.TrackerCollection<AutomationPeer>();

		//var spLightDismissElement;
		//spComboBox.GetLightDismissElement(out spLightDismissElement);

		//if (spLightDismissElement != null)
		//{
		//	var ap = spLightDismissElement.GetOrCreateAutomationPeer();
		//	apChildren.Add(ap);
		//}

		//UNO TODO: Implement GetEditableTextPart and IsEditable on ComboBox

		//bool isEditable = spComboBox.IsEditable;
		//UIElement spEditableTextElement;

		//if (isEditable && spComboBox.GetEditableTextPart(out spEditableTextElement))
		//{
		//	IAutomationPeer ap = spEditableTextElement.GetOrCreateAutomationPeer();
		//	apChildren.Insert(0, ap);
		//}

		//return apChildren;
	}

	protected override string GetClassNameCore() => nameof(ComboBox);

	protected override string GetNameCore()
	{
		var returnValue = base.GetNameCore();

		// If the Name property wasn't explicitly set on this AP, then we will
		// return the combo header.
		if (string.IsNullOrEmpty(returnValue))
		{
			return ((ComboBox)Owner).Header?.ToString();
		}

		return returnValue;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ComboBox;

	protected override string GetHelpTextCore()
	{
		var returnValue = base.GetHelpTextCore();

		// If the HelpText property wasn't explicitly set on this AP and no item is selected,
		// then we'll return the placeholder text.
		if (string.IsNullOrEmpty(returnValue))
		{
			var selectedIndex = ((ComboBox)Owner).SelectedIndex;
			if (selectedIndex < 0)
			{
				return ((ComboBox)Owner).PlaceholderText;
			}
		}

		return returnValue;
	}

	public bool IsReadOnly => !((ComboBox)Owner).IsEnabled || !((ComboBox)Owner).IsEditable;

	public string Value => FrameworkElement.GetStringFromObject(((ComboBox)Owner).SelectedItem); // TODO Uno: this is different from WinUI

	public void SetValue(string value)
	{
		// UIA_E_INVALIDOPERATION
		throw new InvalidOperationException();
	}

	public void Expand()
	{
		if (!IsEnabled())
		{
			// UIA_E_ELEMENTNOTENABLED;
			throw new ElementNotEnabledException();
		}

		var pOwner = Owner as ComboBox;
		pOwner.IsDropDownOpen = true;
	}

	public void Collapse()
	{
		if (!IsEnabled())
		{
			// UIA_E_ELEMENTNOTENABLED
			throw new ElementNotEnabledException();
		}

		var pOwner = Owner as ComboBox;
		pOwner.IsDropDownOpen = false;
	}

	public ExpandCollapseState ExpandCollapseState
		=> ((ComboBox)Owner).IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

	// Raise events for ExpandCollapseState changes to UIAutomation Clients.
	public void RaiseExpandCollapseAutomationEvent(bool isOpen)
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

	public new bool IsSelectionRequired => true;

	public bool IsModal => true;

	public bool IsTopmost => true;

	public bool Maximizable => false;

	public bool Minimizable => false;

	public WindowInteractionState InteractionState => WindowInteractionState.Running;

	public WindowVisualState VisualState => WindowVisualState.Normal;

	public void SetVisualState(WindowVisualState state)
	{

	}

	public void Close()
	{

	}

	public bool WaitForInputIdle(int milliseconds) => false;
}
