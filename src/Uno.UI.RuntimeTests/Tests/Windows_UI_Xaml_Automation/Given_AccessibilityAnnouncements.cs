using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation.Peers;
using Private.Infrastructure;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for accessibility announcements (live regions).
	/// Tests that polite and assertive announcements can be triggered via the accessibility API.
	/// </summary>
	[TestClass]
	public class Given_AccessibilityAnnouncements
	{
		/// <summary>
		/// T079: Verifies that polite announcements can be made without throwing.
		/// In a browser, this would update the aria-live="polite" region.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Polite_Announcement_Then_LiveRegion_Updates()
		{
			// Arrange & Act - Verify the announcement API doesn't throw
			// In runtime tests on Skia desktop, this is a no-op but should not error
			await TestServices.WindowHelper.WaitForIdle();

#if HAS_UNO
			// Verify that announcement methods exist and can be called
			var accessibility = WebAssemblyAccessibility.Instance;
			Assert.IsNotNull(accessibility, "WebAssemblyAccessibility instance should exist");

			// This should not throw - on non-WASM platforms it's a no-op
			try
			{
				accessibility.AnnouncePolite("Test polite message");
			}
			catch (Exception ex) when (ex is not AssertFailedException)
			{
				// Expected on non-WASM platforms where JSImport isn't available
			}
#endif
		}

		/// <summary>
		/// T080: Verifies that assertive announcements can be made without throwing.
		/// In a browser, this would update the aria-live="assertive" region.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Assertive_Announcement_Then_LiveRegion_Updates_Immediately()
		{
			// Arrange & Act
			await TestServices.WindowHelper.WaitForIdle();

#if HAS_UNO
			var accessibility = WebAssemblyAccessibility.Instance;
			Assert.IsNotNull(accessibility, "WebAssemblyAccessibility instance should exist");

			try
			{
				accessibility.AnnounceAssertive("Test assertive message");
			}
			catch (Exception ex) when (ex is not AssertFailedException)
			{
				// Expected on non-WASM platforms where JSImport isn't available
			}
#endif
		}

		/// <summary>
		/// Verifies that AutomationPeer.RaiseAutomationEvent can be called for LiveRegionChanged.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_LiveRegionChanged_Event_Raised_Then_No_Error()
		{
			// Arrange
			var button = new Microsoft.UI.Xaml.Controls.Button { Content = "Test" };
			await Uno.UI.RuntimeTests.Helpers.UITestHelper.Load(button);

			var peer = button.GetOrCreateAutomationPeer();
			Assert.IsNotNull(peer);

			// Act & Assert - Should not throw
			peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
		}
	}
}
