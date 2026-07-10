using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AutomationPeer_InvalidatePeer
{
#if __SKIA__
	// AutomationPeer.InvalidatePeer() used to be a no-op stub. WCT DataGrid relies on it
	// (DataGridItemAutomationPeer.GetChildrenCore and DataGridAutomationPeer.RaiseAutomationInvokeEvents
	// both call it) to drop a peer's cached UIA node so cells refresh after a data change. With the
	// no-op, the Skia/Win32 provider kept serving stale children → DataGridRow reported empty cells (#492).
	//
	// Faithful to WinUI's CAutomationPeer::InvalidatePeer: it routes through the listener's
	// NotifyInvalidatePeer (which re-evaluates automatic properties via RaiseAutomaticPropertyChanges
	// and, on the Win32 bridge, drops the provider children cache). It must NOT raise StructureChanged —
	// WinUI never raises StructureChanged from this path (see CCoreServices::CallbackEventListener).
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/492")]
	public void When_InvalidatePeer_Then_Notifies_Listener_Without_StructureChanged()
	{
		try
		{
			var listener = new CapturingListener();
			AutomationPeer.TestAutomationPeerListener = listener;

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new Button());
			Assert.IsNotNull(peer);

			peer.InvalidatePeer();

			// InvalidatePeer must route through NotifyInvalidatePeer for exactly this peer…
			CollectionAssert.Contains(listener.InvalidatedPeers, peer);
			// …and must not raise a StructureChanged event (WinUI parity).
			CollectionAssert.DoesNotContain(listener.Events, AutomationEvents.StructureChanged);
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
		}
	}

	private sealed class CapturingListener : IAutomationPeerListener
	{
		public List<AutomationEvents> Events { get; } = new();
		public List<AutomationPeer> InvalidatedPeers { get; } = new();

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => Events.Add(eventId);

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => Events.Add(eventId);

		public void NotifyInvalidatePeer(AutomationPeer peer) => InvalidatedPeers.Add(peer);

		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) { }

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, System.Collections.Generic.IReadOnlyList<string> changedData) { }
	}
#endif
}
