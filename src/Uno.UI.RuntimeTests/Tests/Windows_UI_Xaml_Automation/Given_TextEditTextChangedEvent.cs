#nullable enable

using System;
using System.Collections.Generic;
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
	/// Runtime tests for <see cref="AutomationPeer.RaiseTextEditTextChangedEvent"/> (W32-02). The Skia
	/// path was a <c>[NotImplemented]</c> stub, so an app calling it (e.g. a custom editor on AutoCorrect
	/// or IME composition) had the UIA TextEdit_TextChanged event dropped. This validates the caller now
	/// routes the change type and data to the accessibility listener.
	/// </summary>
	[TestClass]
	public class Given_TextEditTextChangedEvent
	{
#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RaiseTextEditTextChangedEvent_Then_Listener_Receives_Payload()
		{
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			Assert.IsNotNull(peer);

			var listener = new RecordingTextEditListener();
			var previous = AutomationPeer.TestAutomationPeerListener;
			try
			{
				AutomationPeer.TestAutomationPeerListener = listener;

				peer.RaiseTextEditTextChangedEvent(
					AutomationTextEditChangeType.AutoCorrect,
					new[] { "teh -> the" });

				Assert.AreEqual(1, listener.Count, "The event must reach the listener exactly once");
				Assert.AreEqual(AutomationTextEditChangeType.AutoCorrect, listener.LastChangeType);
				Assert.IsNotNull(listener.LastChangedData);
				Assert.AreEqual(1, listener.LastChangedData.Count);
				Assert.AreEqual("teh -> the", listener.LastChangedData[0]);
			}
			finally
			{
				AutomationPeer.TestAutomationPeerListener = previous;
			}
		}

		private sealed class RecordingTextEditListener : IAutomationPeerListener
		{
			public int Count { get; private set; }
			public AutomationTextEditChangeType LastChangeType { get; private set; }
			public IReadOnlyList<string> LastChangedData { get; private set; } = Array.Empty<string>();

			public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData)
			{
				Count++;
				LastChangeType = changeType;
				LastChangedData = changedData;
			}

			public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) { }
			public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child) { }
			public void NotifyInvalidatePeer(AutomationPeer peer) { }
			public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
			public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public bool ListenerExistsHelper(AutomationEvents eventId) => true;
		}
#endif
	}
}
