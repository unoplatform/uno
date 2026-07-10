using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

#if __SKIA__
using Microsoft.UI.Xaml.Automation;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for <see cref="AutomationPeer.RaiseNotificationEvent"/> (XP-04). The Skia path
	/// was previously commented out (only warned NotImplemented), so built-in controls that raise
	/// notifications (InfoBar, TeachingTip, CalendarView, TabView) had their AT announcements
	/// dropped. This validates the caller now routes to the accessibility listener.
	/// </summary>
	[TestClass]
	public class Given_NotificationEvent
	{
#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RaiseNotificationEvent_Then_Listener_Receives_Payload()
		{
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			Assert.IsNotNull(peer);

			var listener = new RecordingAutomationPeerListener();
			var previous = AutomationPeer.TestAutomationPeerListener;
			try
			{
				AutomationPeer.TestAutomationPeerListener = listener;

				peer.RaiseNotificationEvent(
					AutomationNotificationKind.ActionCompleted,
					AutomationNotificationProcessing.ImportantMostRecent,
					"Item saved",
					"activity-1");

				Assert.AreEqual(1, listener.NotificationCount, "The notification must reach the listener exactly once");
				Assert.AreEqual("Item saved", listener.LastDisplayString);
				Assert.AreEqual("activity-1", listener.LastActivityId);
				Assert.AreEqual(AutomationNotificationKind.ActionCompleted, listener.LastKind);
				Assert.AreEqual(AutomationNotificationProcessing.ImportantMostRecent, listener.LastProcessing);
			}
			finally
			{
				AutomationPeer.TestAutomationPeerListener = previous;
			}
		}

		private sealed class RecordingAutomationPeerListener : IAutomationPeerListener
		{
			public int NotificationCount { get; private set; }
			public string LastDisplayString { get; private set; }
			public string LastActivityId { get; private set; }
			public AutomationNotificationKind LastKind { get; private set; }
			public AutomationNotificationProcessing LastProcessing { get; private set; }

			public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
			{
				NotificationCount++;
				LastKind = notificationKind;
				LastProcessing = notificationProcessing;
				LastDisplayString = displayString;
				LastActivityId = activityId;
			}

			public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) { }
			public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public void NotifyInvalidatePeer(AutomationPeer peer) { }
			public void NotifyTextEditTextChangedEvent(AutomationPeer peer, Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, System.Collections.Generic.IReadOnlyList<string> changedData) { }
			public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public bool ListenerExistsHelper(AutomationEvents eventId) => true;
		}
#endif
	}
}
