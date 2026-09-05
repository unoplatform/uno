#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class TextBoxAutomationPeer
{
	internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
	{
		if (ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
		}
	}

	internal void RaisePlaceholderTextChangedEvents(string oldPlaceholder, string newPlaceholder)
	{
		if (!ListenerExistsHelper(AutomationEvents.PropertyChanged) || Owner is not TextBox owner)
		{
			return;
		}

		var headerText = owner.Header?.ToString();
		if (string.IsNullOrEmpty(AutomationProperties.GetName(owner)) &&
			AutomationProperties.GetLabeledBy(owner) is null &&
			string.IsNullOrEmpty(headerText))
		{
			RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldPlaceholder, newPlaceholder);
		}

		if (string.IsNullOrEmpty(AutomationProperties.GetHelpText(owner)) && !string.IsNullOrEmpty(headerText))
		{
			RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, oldPlaceholder, newPlaceholder);
		}
	}
}
