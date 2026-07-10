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
	/// Runtime tests for SH-03: <see cref="AutomationPeer.RaiseStructureChangedEvent"/> was a no-op
	/// stub, so custom peers overriding GetChildrenCore had no way to signal structure changes. It now
	/// routes through the accessibility listener into the same per-backend structure path used by
	/// framework-driven tree mutations (Win32 raises UIA StructureChanged; macOS posts children-changed).
	/// </summary>
	[TestClass]
	public class Given_StructureChangedEvent
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_ChildRemoved_With_Null_Child_Then_Throws()
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new Button { Content = "X" });
			Assert.IsNotNull(peer);
			Assert.ThrowsExactly<System.ArgumentNullException>(
				() => peer.RaiseStructureChangedEvent(AutomationStructureChangeType.ChildRemoved, null!));
		}

#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RaiseStructureChangedEvent_Then_Listener_Gets_StructureChanged()
		{
			var button = new Button { Content = "X" };
			await UITestHelper.Load(button);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			Assert.IsNotNull(peer);

			var listener = new RecordingListener();
			var previous = AutomationPeer.TestAutomationPeerListener;
			try
			{
				AutomationPeer.TestAutomationPeerListener = listener;

				peer.RaiseStructureChangedEvent(AutomationStructureChangeType.ChildrenInvalidated, null!);

				Assert.AreEqual(1, listener.EventCount, "The structure change must reach the listener");
				Assert.AreEqual(AutomationEvents.StructureChanged, listener.LastEventId);
			}
			finally
			{
				AutomationPeer.TestAutomationPeerListener = previous;
			}
		}

		private sealed class RecordingListener : IAutomationPeerListener
		{
			public int EventCount { get; private set; }
			public AutomationEvents LastEventId { get; private set; }

			public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			{
				EventCount++;
				LastEventId = eventId;
			}

			public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) { }
			public void NotifyInvalidatePeer(AutomationPeer peer) { }
			public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
			public void NotifyTextEditTextChangedEvent(AutomationPeer peer, Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, System.Collections.Generic.IReadOnlyList<string> changedData) { }
			public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public bool ListenerExistsHelper(AutomationEvents eventId) => true;
		}
#endif
	}
}
