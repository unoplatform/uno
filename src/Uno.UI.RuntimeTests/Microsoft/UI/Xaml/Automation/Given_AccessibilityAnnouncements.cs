#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Helpers;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AccessibilityAnnouncements
{
	[TestMethod]
	public void When_Polite_Announcement_Then_Implementation_Receives_Message()
	{
		var implementation = new RecordingAccessibility();
		var previous = AccessibilityAnnouncer.TestAccessibilityImpl;
		try
		{
			AccessibilityAnnouncer.TestAccessibilityImpl = implementation;

			AccessibilityAnnouncer.AnnouncePolite("Polite message");

			Assert.AreEqual("Polite message", implementation.LastPoliteMessage);
			Assert.AreEqual(1, implementation.PoliteCount);
		}
		finally
		{
			AccessibilityAnnouncer.TestAccessibilityImpl = previous;
		}
	}

	[TestMethod]
	public void When_Assertive_Announcement_Then_Implementation_Receives_Message()
	{
		var implementation = new RecordingAccessibility();
		var previous = AccessibilityAnnouncer.TestAccessibilityImpl;
		try
		{
			AccessibilityAnnouncer.TestAccessibilityImpl = implementation;

			AccessibilityAnnouncer.AnnounceAssertive("Assertive message");

			Assert.AreEqual("Assertive message", implementation.LastAssertiveMessage);
			Assert.AreEqual(1, implementation.AssertiveCount);
		}
		finally
		{
			AccessibilityAnnouncer.TestAccessibilityImpl = previous;
		}
	}

	[TestMethod]
	public void When_Accessibility_Is_Disabled_Then_Announcement_Is_Not_Routed()
	{
		var implementation = new RecordingAccessibility { IsAccessibilityEnabled = false };
		var previous = AccessibilityAnnouncer.TestAccessibilityImpl;
		try
		{
			AccessibilityAnnouncer.TestAccessibilityImpl = implementation;

			AccessibilityAnnouncer.AnnouncePolite("Suppressed");
			AccessibilityAnnouncer.AnnounceAssertive("Suppressed");

			Assert.AreEqual(0, implementation.PoliteCount);
			Assert.AreEqual(0, implementation.AssertiveCount);
		}
		finally
		{
			AccessibilityAnnouncer.TestAccessibilityImpl = previous;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LiveRegionChanged_Event_Raised_Then_No_Error()
	{
		var button = new Button { Content = "Test" };
		await UITestHelper.Load(button);

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
		Assert.IsNotNull(peer);

		peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
	}

	private sealed class RecordingAccessibility : IUnoAccessibility
	{
		public bool IsAccessibilityEnabled { get; set; } = true;

		public int PoliteCount { get; private set; }

		public int AssertiveCount { get; private set; }

		public string? LastPoliteMessage { get; private set; }

		public string? LastAssertiveMessage { get; private set; }

		public void AnnouncePolite(string text)
		{
			PoliteCount++;
			LastPoliteMessage = text;
		}

		public void AnnounceAssertive(string text)
		{
			AssertiveCount++;
			LastAssertiveMessage = text;
		}
	}
}
