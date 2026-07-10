namespace Microsoft.UI.Xaml.Automation.Peers;

internal interface IAutomationPeerListener
{
	void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue);
	void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId);

	/// <summary>
	/// Mirrors WinUI's <c>CAutomationPeer::InvalidatePeer</c>: re-evaluate the peer's
	/// automatic properties (IsEnabled/IsOffscreen/Name/ItemStatus) and raise
	/// PropertyChanged for any that changed. Unlike WinUI, platform implementations that
	/// keep their own provider-level children cache (the Win32 UIA bridge) also drop that
	/// cache here so virtual peers (e.g. WCT DataGrid rows) rebuild after a data change.
	/// WinUI does NOT raise StructureChanged from this path, so implementations must not either.
	/// </summary>
	void NotifyInvalidatePeer(AutomationPeer peer);
	void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId);
	void NotifyTextEditTextChangedEvent(AutomationPeer peer, global::Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, global::System.Collections.Generic.IReadOnlyList<string> changedData);
	void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId);
	bool ListenerExistsHelper(AutomationEvents eventId);
}
