using System;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class AutomationPeer
{
	internal event Action<UIElement, AutomationProperty, object> OnPropertyChanged;

	public void RaisePropertyChangedEvent(AutomationProperty automationProperty, object oldValue, object newValue)
	{
		OnPropertyChanged?.Invoke((this as FrameworkElementAutomationPeer)?.Owner, automationProperty, newValue);
	}
}
