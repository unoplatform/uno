namespace Microsoft.UI.Xaml.Automation.Peers;

internal interface IAutomationPeerListener
{
	void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue);
	void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId);
	void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId);
	bool ListenerExistsHelper(AutomationEvents eventId);
}
