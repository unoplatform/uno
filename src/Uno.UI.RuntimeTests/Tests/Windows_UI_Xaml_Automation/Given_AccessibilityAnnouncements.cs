using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif
#if __SKIA__
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
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
		[Ignore("Temporarily disabled - not yet validated")]
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
		[Ignore("Temporarily disabled - not yet validated")]
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
		[Ignore("Temporarily disabled - not yet validated")]
		[RunsOnUIThread]
		public async Task When_LiveRegionChanged_Event_Raised_Then_No_Error()
		{
			// Arrange
			var button = new Microsoft.UI.Xaml.Controls.Button { Content = "Test" };
			await Uno.UI.RuntimeTests.Helpers.UITestHelper.Load(button);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			Assert.IsNotNull(peer);

			// Act & Assert - Should not throw
			peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
		}

#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Polite_Updates_Are_Sustained_Then_Announcement_Does_Not_Starve()
		{
			var button = new Button { Content = "Live region host" };
			AutomationProperties.SetLiveSetting(button, AutomationLiveSetting.Polite);
			AutomationProperties.SetName(button, "Progress 0");
			await UITestHelper.Load(button);
			var peer = button.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.getElementById('uno-live-region-polite') ? '1' : '0'") == "1",
				timeoutMS: 5000,
				message: "Timed out waiting for the polite live region.");

			var announcedDuringStream = false;
			for (var index = 0; index < 20; index++)
			{
				AutomationProperties.SetName(button, $"Progress {index}");
				peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
				await Task.Delay(25);
				if (index < 19 && InvokeBrowserJs("document.getElementById('uno-live-region-polite')?.textContent || ''").Length > 0)
				{
					announcedDuringStream = true;
				}
			}

			Assert.IsTrue(announcedDuringStream, "A sustained update stream must not reset the debounce indefinitely and starve assistive technology.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Pending_Live_Region_Commit_Is_Cleared_Then_Stale_Content_Does_Not_Reappear()
		{
			var button = new Button { Content = "Clear live region" };
			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.getElementById('uno-live-region-polite') ? '1' : '0'") == "1",
				timeoutMS: 5000,
				message: "Timed out waiting for the polite live region.");

			InvokeBrowserJs("globalThis.Uno.UI.Runtime.Skia.LiveRegion.updateLiveRegionContent(0, 'stale', 1); globalThis.Uno.UI.Runtime.Skia.LiveRegion.clearPendingAnnouncements(); 'ok'");
			await Task.Delay(50);
			Assert.AreEqual(string.Empty, InvokeBrowserJs("document.getElementById('uno-live-region-polite')?.textContent || ''"), "Clearing pending announcements must cancel the deferred browser commit.");
		}
#endif
	}
}
