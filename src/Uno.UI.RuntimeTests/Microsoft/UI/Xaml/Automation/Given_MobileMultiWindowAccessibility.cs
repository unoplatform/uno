#if __SKIA__
#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileMultiWindowAccessibility
{
	private static AccessibilityNativeNodeSnapshot[] GetSnapshots(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	private static AccessibilityNativeEventRecord[] GetAndClearEvents(XamlRoot root)
	{
		var records =
			AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor?.Invoke(root)
			?? AccessibilityPeerHelper.IOSAccessibilityEventsAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeEventRecord>();

		AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction?.Invoke(root);
		AccessibilityPeerHelper.IOSClearAccessibilityEventsAction?.Invoke(root);
		return records;
	}

	[TestMethod]
	public async Task When_Primary_Root_Is_Queried_Then_Snapshot_Is_Root_Scoped()
	{
		var button = new Button { Content = "Root scoped probe" };
		AutomationProperties.SetName(button, "Root scoped probe");
		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshots = GetSnapshots(button.XamlRoot!);

		Assert.IsTrue(
			snapshots.Any(snapshot => snapshot.Name == "Root scoped probe"),
			"The root snapshot accessor must return the element mounted in that XamlRoot.");
	}

	[TestMethod]
	public async Task When_Primary_Root_Changes_Then_Event_Is_Routed_To_That_Root()
	{
		var panel = new StackPanel
		{
			Children =
			{
				new Button { Content = "Anchor" },
			},
		};
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;
		GetAndClearEvents(root);

		panel.Children.Add(new Button { Content = "Added" });
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(root).Any(record =>
				record.Kind is AccessibilityNativeEventKind.StructureChanged
					or AccessibilityNativeEventKind.NodeInvalidated),
			"Tree changes must be observable through the owning XamlRoot's event accessor.");
	}

	[TestMethod]
	public async Task When_Native_Focus_Is_Set_Then_Focused_Node_Is_Root_Scoped()
	{
		var button = new Button { Content = "Focus probe" };
		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var focused =
			AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor?.Invoke(button)
			?? AccessibilityPeerHelper.IOSAccessibilityFocusAccessor?.Invoke(button)
			?? false;
		Assert.IsTrue(focused, "The loaded element must accept native accessibility focus.");

		var focusedNode =
			AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor?.Invoke(button.XamlRoot!)
			?? AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(button.XamlRoot!);
		Assert.IsNotNull(focusedNode, "Focused-node lookup must resolve through the owning XamlRoot.");
	}

	[TestMethod]
	public async Task When_Secondary_Window_Is_Created_Then_Current_Limitation_Is_Explicit()
	{
		var button = new Button { Content = "Primary" };
		await UITestHelper.Load(button);

		var exception = Assert.ThrowsExactly<InvalidOperationException>(() => new Window());
		StringAssert.Contains(
			exception.Message,
			"secondary windows",
			"The test must be updated to validate cross-root isolation when mobile secondary windows become supported.");
	}
}

#endif
