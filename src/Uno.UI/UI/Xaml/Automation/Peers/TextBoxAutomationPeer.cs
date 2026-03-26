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
public partial class TextBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
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

	/// <summary>
	/// Gets the text value of the TextBox.
	/// </summary>
	public string Value => (Owner as TextBox)?.Text ?? string.Empty;

	/// <summary>
	/// Gets whether the TextBox is read-only.
	/// </summary>
	public bool IsReadOnly => (Owner as TextBox)?.IsReadOnly ?? false;

	/// <summary>
	/// Sets the text value of the TextBox.
	/// </summary>
	public void SetValue(string value)
	{
		if (!IsEnabled())
		{
			throw new InvalidOperationException("Element not enabled");
		}

		if (IsReadOnly)
		{
			throw new InvalidOperationException("Element is read-only");
		}

		if (Owner is TextBox textBox)
		{
			textBox.Text = value;
		}
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
