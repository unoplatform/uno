#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_MobileAccessibilityEvents
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Dynamic_AutomationProperties_Change_Then_Listener_Receives_Identifiers()
	{
		var button = new Button { Content = "Target" };
		var controlled = new TextBox();
		var panel = new StackPanel { Children = { button, controlled } };
		await UITestHelper.Load(panel);

		var listener = new RecordingListener();
		var previous = AutomationPeer.TestAutomationPeerListener;
		try
		{
			AutomationPeer.TestAutomationPeerListener = listener;

			AutomationProperties.SetAutomationId(button, "target-id");
			AutomationProperties.SetFullDescription(button, "Description");
			AutomationProperties.SetIsRequiredForForm(button, true);
			AutomationProperties.SetLiveSetting(button, AutomationLiveSetting.Polite);
			AutomationProperties.SetPositionInSet(button, 1);
			AutomationProperties.SetSizeOfSet(button, 2);
			AutomationProperties.SetLevel(button, 3);
			AutomationProperties.SetIsDialog(button, true);
			AutomationProperties.GetControlledPeers(button).Add(controlled);
			AutomationProperties.GetDescribedBy(button).Add(controlled);
			AutomationProperties.GetFlowsFrom(button).Add(controlled);
			AutomationProperties.GetFlowsTo(button).Add(controlled);
			AutomationProperties.SetLocalizedControlType(button, "action");
			AutomationProperties.SetAccessibilityView(button, AccessibilityView.Raw);

			var properties = listener.Properties.ToHashSet();
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.AutomationIdProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.FullDescriptionProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.IsRequiredForFormProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.LiveSettingProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.PositionInSetProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.SizeOfSetProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.LevelProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.IsDialogProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.ControlledPeersProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.DescribedByProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.FlowsFromProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.FlowsToProperty));
			Assert.IsTrue(properties.Contains(AutomationElementIdentifiers.LocalizedControlTypeProperty));
			Assert.IsTrue(listener.Events.Contains(AutomationEvents.StructureChanged));
			Assert.AreEqual(
				1,
				listener.Properties.Count(property => property == AutomationElementIdentifiers.ControlledPeersProperty));
			Assert.AreEqual(
				1,
				listener.Properties.Count(property => property == AutomationElementIdentifiers.DescribedByProperty));
			Assert.AreEqual(
				1,
				listener.Properties.Count(property => property == AutomationElementIdentifiers.FlowsFromProperty));
			Assert.AreEqual(
				1,
				listener.Properties.Count(property => property == AutomationElementIdentifiers.FlowsToProperty));
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = previous;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Orientation_Changes_Then_Listener_Receives_Orientation_Property()
	{
		var slider = new Slider();
		var scrollBar = new ScrollBar();
		var panel = new StackPanel { Children = { slider, scrollBar } };
		await UITestHelper.Load(panel);

		var listener = new RecordingListener();
		var previous = AutomationPeer.TestAutomationPeerListener;
		try
		{
			AutomationPeer.TestAutomationPeerListener = listener;

			slider.Orientation = Orientation.Vertical;
			scrollBar.Orientation = Orientation.Horizontal;

			Assert.AreEqual(
				2,
				listener.Properties.Count(property => property == AutomationElementIdentifiers.OrientationProperty));
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = previous;
		}
	}

	private sealed class RecordingListener : IAutomationPeerListener
	{
		public List<AutomationProperty> Properties { get; } = new();

		public List<AutomationEvents> Events { get; } = new();

		public void NotifyPropertyChangedEvent(
			AutomationPeer peer,
			AutomationProperty automationProperty,
			object oldValue,
			object newValue)
			=> Properties.Add(automationProperty);

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
			=> Events.Add(eventId);

		public void NotifyInvalidatePeer(AutomationPeer peer) { }

		public void NotifyNotificationEvent(
			AutomationPeer peer,
			AutomationNotificationKind notificationKind,
			AutomationNotificationProcessing notificationProcessing,
			string displayString,
			string activityId) { }

		public void NotifyTextEditTextChangedEvent(
			AutomationPeer peer,
			AutomationTextEditChangeType changeType,
			IReadOnlyList<string> changedData) { }

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;
	}
}
