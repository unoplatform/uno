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
	// InvalidatePeer now routes through the automation listener as a StructureChanged notification.
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/492")]
	public void When_InvalidatePeer_Then_Raises_StructureChanged()
	{
		try
		{
			var listener = new CapturingListener();
			AutomationPeer.TestAutomationPeerListener = listener;

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new Button());
			Assert.IsNotNull(peer);

			peer.InvalidatePeer();

			CollectionAssert.Contains(listener.Events, AutomationEvents.StructureChanged);
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
		}
	}

	private sealed class CapturingListener : IAutomationPeerListener
	{
		public List<AutomationEvents> Events { get; } = new();

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => Events.Add(eventId);

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => Events.Add(eventId);

		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) { }

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
	}
#endif
}
