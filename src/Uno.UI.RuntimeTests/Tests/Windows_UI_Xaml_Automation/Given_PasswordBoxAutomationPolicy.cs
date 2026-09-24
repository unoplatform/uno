#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_PasswordBoxAutomationPolicy
{
	[TestMethod]
	[DataRow("")]
	[DataRow("Authored help")]
	public void When_Header_And_Placeholder_Change_Then_HelpText_Remains_Authored(string helpText)
	{
		// MUX PasswordBoxAutomationPeer_Partial.cpp, winui3/release/1.8.4, dc46907e:
		// placeholder hints use GetDescribedByCoreImpl, not a HelpText override.
		var passwordBox = new PasswordBox { Header = "Password", PlaceholderText = "Initial hint" };
		AutomationProperties.SetHelpText(passwordBox, helpText);
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(passwordBox);
		Assert.AreEqual(helpText, peer.GetHelpText());

		passwordBox.PlaceholderText = "Updated hint";
		passwordBox.Header = "Updated header";
		Assert.AreEqual(helpText, peer.GetHelpText());

		passwordBox.Header = null;
		Assert.AreEqual(helpText, peer.GetHelpText());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Password_And_Placeholder_Change_Then_No_Fabricated_UIA_Property_Events()
	{
#if HAS_UNO
		var previousListener = AutomationPeer.TestAutomationPeerListener;
		var listener = new CapturingListener();
		try
		{
			AutomationPeer.TestAutomationPeerListener = listener;
			var passwordBox = new PasswordBox { Header = "Password", PlaceholderText = "Initial hint" };
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(passwordBox);
			listener.Changes.Clear();

			passwordBox.PlaceholderText = "Updated hint";
			passwordBox.Password = "secret";
			Assert.IsFalse(listener.Changes.Exists(change =>
				change.Property == AutomationElementIdentifiers.HelpTextProperty ||
				change.Property == ValuePatternIdentifiers.ValueProperty));

			AutomationProperties.SetHelpText(passwordBox, "Authored help");
			// An explicitly raised notification must still reach the listener.
			peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, string.Empty, "Authored help");
			var helpChanges = listener.Changes.FindAll(change => change.Property == AutomationElementIdentifiers.HelpTextProperty);
			Assert.AreEqual(1, helpChanges.Count);
			Assert.AreEqual("Authored help", helpChanges[0].NewValue);
			Assert.AreEqual("Authored help", peer.GetHelpText());

			listener.Changes.Clear();
			passwordBox.PlaceholderText = string.Empty;
			Assert.IsFalse(listener.Changes.Exists(change => change.Property == AutomationElementIdentifiers.HelpTextProperty));
			Assert.AreEqual("Authored help", peer.GetHelpText());
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = previousListener;
		}
#endif
	}

#if HAS_UNO
	private sealed class CapturingListener : IAutomationPeerListener
	{
		public List<(AutomationProperty Property, object NewValue)> Changes { get; } = new();

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
			=> Changes.Add((automationProperty, newValue));
		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child) { }
		public void NotifyInvalidatePeer(AutomationPeer peer) { }
		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData) { }
	}
#endif
}
