namespace Microsoft.UI.Xaml.Automation.Peers;

internal interface IAutomationPeerListener
{
	void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue);
	bool ListenerExistsHelper(AutomationEvents eventId);
}
