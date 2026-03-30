// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBoxAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TextBox types to Microsoft UI Automation.
/// </summary>
public partial class TextBoxAutomationPeer : FrameworkElementAutomationPeer, Provider.IValueProvider
{
	public TextBoxAutomationPeer(TextBox owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(TextBox);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Edit;

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Value)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	// IValueProvider

	public string Value => ((TextBox)Owner).Text ?? string.Empty;

	public bool IsReadOnly => ((TextBox)Owner).IsReadOnly;

	public void SetValue(string value)
	{
		if (IsReadOnly)
		{
			throw new System.InvalidOperationException("Cannot set value on a read-only TextBox.");
		}

		((TextBox)Owner).Text = value;
	}

	/// <summary>
	/// Raises ValueProperty changed event for accessibility listeners.
	/// Called from TextBox when Text property changes.
	/// </summary>
	internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
	{
		if (ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
		}
	}

	protected override string GetNameCore()
	{
		var baseName = base.GetNameCore();
		if (!string.IsNullOrEmpty(baseName))
		{
			return baseName;
		}

		// WinUI3 uses the Header as the accessible name for TextBox when no
		// explicit Name or LabeledBy is set.
		if (Owner is TextBox { Header: { } header })
		{
			var headerText = header.ToString();
			if (!string.IsNullOrEmpty(headerText))
			{
				return headerText;
			}
		}

		// Fall back to PlaceholderText when no Header is available.
		if (Owner is TextBox { PlaceholderText: { } placeholder } && !string.IsNullOrEmpty(placeholder))
		{
			return placeholder;
		}

		return string.Empty;
	}

	protected override string GetHelpTextCore()
	{
		var baseHelp = base.GetHelpTextCore();
		if (!string.IsNullOrEmpty(baseHelp))
		{
			return baseHelp;
		}

		// When Header provides the name, PlaceholderText serves as help text
		// (shown as the Narrator hint). This matches WinUI3 behavior.
		if (Owner is TextBox { Header: { } header, PlaceholderText: { } placeholder }
			&& !string.IsNullOrEmpty(header.ToString())
			&& !string.IsNullOrEmpty(placeholder))
		{
			return placeholder;
		}

		return string.Empty;
	}

	protected override IEnumerable<AutomationPeer> GetDescribedByCore()
	{
		if (Owner is TextBox owner)
		{
			//UNO TODO Implement TextBoxPlaceholderTextHelper
			//IFC_RETURN(TextBoxPlaceholderTextHelper::SetupPlaceholderTextBlockDescribedBy(spOwner));

			return base.GetDescribedByCore();
		}

		return [];
	}
}
