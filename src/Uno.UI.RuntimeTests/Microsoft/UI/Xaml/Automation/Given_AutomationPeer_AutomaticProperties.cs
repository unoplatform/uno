#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AutomationPeer_AutomaticProperties
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Peer_Created_While_Owner_Disabled_Then_Reenabling_Raises_IsEnabled()
	{
		var button = new Button { Content = "Export", IsEnabled = false };
		await UITestHelper.Load(button);

		var listener = new CapturingListener();
		AutomationPeer.TestAutomationPeerListener = listener;
		try
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			Assert.IsNotNull(peer);

			button.IsEnabled = true;

			CollectionAssert.Contains(listener.Changes, (peer, AutomationElementIdentifiers.IsEnabledProperty, (object)false, (object)true),
				"A peer created while its owner is disabled must report the owner being re-enabled.");
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
		}
	}

	private sealed class CapturingListener : IAutomationPeerListener
	{
		public List<(AutomationPeer Peer, AutomationProperty Property, object OldValue, object NewValue)> Changes { get; } = new();

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }

		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child) { }

		public void NotifyInvalidatePeer(AutomationPeer peer) { }

		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
			=> Changes.Add((peer, automationProperty, oldValue, newValue));

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }

		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData) { }
	}
#endif
}
