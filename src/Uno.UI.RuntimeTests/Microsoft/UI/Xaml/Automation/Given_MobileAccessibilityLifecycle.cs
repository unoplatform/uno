#nullable enable

// Android and iOS native focus-event and stale-focused-node tests.
// All tests use only shared hooks from AccessibilityPeerHelper and
// platform-neutral AccessibilityNativeNodeSnapshot; no direct references to
// Android or UIKit platform types.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Verifies Android native accessibility focus lifecycle via shared hooks:
/// focus request, loop stability, removed-node recovery, and modal filtering.
/// Runs only on the SkiaAndroid target where the hooks are registered.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public partial class Given_MobileAccessibilityLifecycle
{
	// Helpers

	private static bool RequestNativeFocus(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor?.Invoke(element) is true;

	private static object? GetFocusedNode(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor?.Invoke(root);

	// Android focus tests

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focus_Hooks_Are_Registered_Then_Accessors_Are_Not_Null()
	{
		// Load any element so the adapter is initialised.
		var button = new Button { Content = "Hook Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor,
			"AndroidAccessibilityFocusAccessor was not registered by the Android adapter.");

		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor,
			"AndroidFocusedNativeNodeAccessor was not registered by the Android adapter.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focus_Is_Requested_For_In_Tree_Element_Then_Accessor_Returns_True()
	{
		var button = new Button { Content = "Focus Request" };
		await UITestHelper.Load(button);

		var result = RequestNativeFocus(button);

		Assert.IsTrue(result, "RequestNativeFocus should return true for an element in the live tree.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focus_Is_Requested_Then_FocusedNode_Accessor_Returns_Snapshot()
	{
		var button = new Button { Content = "Focused Node" };
		await UITestHelper.Load(button);

		RequestNativeFocus(button);
		var node = GetFocusedNode(button.XamlRoot!);

		Assert.IsNotNull(
			node,
			"GetFocusedNode should return a non-null snapshot after RequestNativeFocus.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focus_Is_Requested_Then_Snapshot_Has_Correct_Name()
	{
		var button = new Button { Content = "Snapshot Name" };
		await UITestHelper.Load(button);

		RequestNativeFocus(button);
		var node = GetFocusedNode(button.XamlRoot!) as AccessibilityNativeNodeSnapshot;

		Assert.IsNotNull(node);
		Assert.IsTrue(
			node.Name?.Contains("Snapshot Name") is true,
			$"Expected snapshot to contain 'Snapshot Name', got '{node.Name}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Checked_Node_Is_Focused_Then_Focused_Snapshot_Preserves_Checked_State()
	{
		var checkBox = new CheckBox { Content = "Focused check box", IsChecked = true };
		await UITestHelper.Load(checkBox);

		Assert.IsTrue(RequestNativeFocus(checkBox));
		var snapshot = GetFocusedNode(checkBox.XamlRoot!) as AccessibilityNativeNodeSnapshot;

		Assert.IsNotNull(snapshot);
		Assert.IsTrue(snapshot.IsChecked is true);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focus_Is_Requested_Twice_For_Same_Element_Then_Result_Is_Stable()
	{
		// Verifies no infinite loop and consistent result.
		var button = new Button { Content = "Stable Target" };
		await UITestHelper.Load(button);

		var r1 = RequestNativeFocus(button);
		var r2 = RequestNativeFocus(button);

		Assert.IsTrue(r1, "First focus request should succeed.");
		Assert.IsTrue(r2, "Second focus request for same element should also succeed.");

		// Focused node should still refer to the same element.
		var node = GetFocusedNode(button.XamlRoot!) as AccessibilityNativeNodeSnapshot;
		Assert.IsNotNull(node);
		Assert.IsTrue(node.Name?.Contains("Stable Target") is true);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focused_Element_Is_Removed_From_Tree_Then_FocusedNode_Returns_Null()
	{
		// Stale-focus recovery: after the focused element leaves the tree the
		// accessor must return null rather than a stale snapshot.
		var panel = new StackPanel();
		var button = new Button { Content = "Removable" };
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		// Establish focus tracking.
		RequestNativeFocus(button);
		Assert.IsNotNull(
			GetFocusedNode(button.XamlRoot!),
			"Focused-node should be available before removal.");

		// Remove the element from the tree and wait for the visual tree to settle.
		var root = button.XamlRoot!;
		panel.Children.Remove(button);
		await TestServices.WindowHelper.WaitForIdle();

		// After the removal the helper must prune stale IDs during the next scan.
		// GetFocusedNode triggers a scan internally.
		var nodeAfterRemoval = GetFocusedNode(root);

		Assert.IsNull(
			nodeAfterRemoval,
			"GetFocusedNode must return null after the focused element is removed from the tree.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focus_Requested_For_Off_Tree_Element_Then_Returns_False()
	{
		// An element that has never been in the live tree should return false.
		var orphan = new Button { Content = "Orphan" };

		var result = AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor?.Invoke(orphan);

		Assert.IsFalse(
			result is true,
			"Focus request for an off-tree element should not succeed.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Modal_Dialog_Is_Active_Then_Background_Element_Cannot_Receive_Focus()
	{
		// Modal filtering: an element outside a dialog peer must not receive focus
		// when a dialog peer is active.
		var panel = new StackPanel();
		var background = new Button { Content = "Background" };
		var dialogButton = new Button { Content = "Dialog Button" };
		var dialog = new DialogHost { Content = dialogButton };
		panel.Children.Add(background);
		panel.Children.Add(dialog);
		await UITestHelper.Load(panel);

		// The background button is OUTSIDE the dialog subtree.
		var canFocusBackground = RequestNativeFocus(background);
		// The dialog button is inside; it should still succeed.
		var canFocusDialogButton = RequestNativeFocus(dialogButton);

		Assert.IsFalse(
			canFocusBackground,
			"Background element should not receive accessibility focus when a modal dialog is active.");

		Assert.IsTrue(
			canFocusDialogButton,
			"Element inside the active dialog should still receive accessibility focus.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focused_Element_Is_Disabled_Then_Native_Enabled_State_Updates()
	{
		var button = new Button { Content = "Will Disable" };
		await UITestHelper.Load(button);

		button.Focus(FocusState.Programmatic);
		await UITestHelper.WaitForIdle();
		Assert.AreSame(
			button,
			FocusManager.GetFocusedElement(button.XamlRoot!),
			"Precondition: button should be XAML-focused before disabling.");

		button.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(button);
		Assert.IsNotNull(snapshot, "The disabled focused element must remain represented natively.");
		Assert.IsFalse(snapshot.Enabled, "Disabling a focused element must update its native enabled state.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NonLightDismiss_Modal_Popup_Opens_Then_Background_Elements_Are_Excluded()
	{
		var background = new Button { Content = "Android Background" };
		var modalButton = new Button { Content = "Android Modal" };
		var popup = new Popup
		{
			IsLightDismissEnabled = false,
			Child = new Border { Child = modalButton },
		};
		var root = new Grid { Children = { background, popup } };
		await UITestHelper.Load(root);
		var backgroundVirtualId = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(background);
		Assert.IsNotNull(backgroundVirtualId);

		try
		{
			popup.IsOpen = true;
			Assert.IsFalse(
				AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(
					backgroundVirtualId.Value,
					0x40) is true,
				"A cached background virtual ID must reject ACTION_ACCESSIBILITY_FOCUS while the modal is active.");
			Assert.IsFalse(
				AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(
					backgroundVirtualId.Value,
					0x1) is true,
				"A cached background virtual ID must reject ACTION_FOCUS while the modal is active.");
			await TestServices.WindowHelper.WaitForIdle();

			var snapshots = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root.XamlRoot!)
				?? [];
			Assert.IsFalse(
				snapshots.Any(snapshot => snapshot.Name == "Android Background"),
				"Background elements must be removed from the native tree while the modal popup is open.");
			Assert.IsTrue(
				snapshots.Any(snapshot => snapshot.Name == "Android Modal"),
				"The modal popup content must remain in the native tree.");
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	private sealed partial class DialogHost : ContentControl
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new DialogHostAutomationPeer(this);
	}

	private sealed class DialogHostAutomationPeer : FrameworkElementAutomationPeer
	{
		public DialogHostAutomationPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;

		protected override bool IsDialogCore() => true;
	}
}

/// <summary>
/// iOS lifecycle tests for US3: focus synchronisation, re-entry guard, stale-node recovery,
/// and modal background exclusion. All tests exercise behaviour through the shared
/// <see cref="AccessibilityPeerHelper.IOSAccessibilityFocusAccessor"/> /
/// <see cref="AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor"/> /
/// <see cref="AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor"/> hooks and
/// contain no UIKit type references, so the file compiles on all platforms.
/// </summary>
/// <remarks>
/// <b>Shared builds on Windows</b>: this file compiles on Windows and other non-iOS targets
/// but the tests are skipped at runtime via <see cref="PlatformConditionAttribute"/>.
/// Actual iOS execution requires a macOS/Xcode runner; see
/// <c>build/test-scripts/ios-uitest-run.sh</c> and the T033 tracking note in
/// <c>specs/005-mobile-a11y-automation/tasks.md</c>.
/// </remarks>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityLifecycle_IOS
{
	// Helpers

	private static bool RequestNativeFocus(UIElement element)
		=> AccessibilityPeerHelper.IOSAccessibilityFocusAccessor?.Invoke(element) is true;

	private static object? GetFocusedNativeNode(XamlRoot root)
		=> AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);

	private static AccessibilityNativeNodeSnapshot[] GetOrderedSnapshots(XamlRoot xamlRoot)
		=> AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? [];

	// Focus request

	/// <summary>
	/// Verifies that <see cref="AccessibilityPeerHelper.IOSAccessibilityFocusAccessor"/>
	/// (the stable native focus-notification path) returns true for a registered element
	/// and that <see cref="AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor"/> then
	/// reports that element as the last-focused native node.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FocusAccessor_Invoked_Then_FocusedNativeNodeIsTracked()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityFocusAccessor,
			"iOS accessibility focus hook was not registered; adapter may not have initialized.");

		var button = new Button { Content = "Focus Target" };
		await UITestHelper.Load(button);

		var requested = RequestNativeFocus(button);
		Assert.IsTrue(requested, "IOSAccessibilityFocusAccessor returned false for a loaded element.");

		await TestServices.WindowHelper.WaitForIdle();

		var focused = GetFocusedNativeNode(button.XamlRoot!);
		Assert.IsNotNull(focused, "IOSFocusedNativeNodeAccessor returned null after a focus request.");
	}

	// Stable focus without loops

	/// <summary>
	/// Verifies that setting XAML focus on an element updates native focus tracking without
	/// re-posting a redundant notification (no XAML -> native -> XAML loop).
	/// The focused native node should correspond to the XAML-focused element, and XAML focus
	/// must remain on that element after the native path confirms it.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_XAML_Focus_Moves_To_Element_Then_NativeNodeIsTracked_Without_Loop()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor,
			"iOS focused-node hook was not registered.");

		var button = new Button { Content = "XAML Focus" };
		await UITestHelper.Load(button);

		// Programmatic XAML focus triggers AutomationFocusChanged -> SetNativeFocus.
		button.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		// XAML focus must still be on the button; no loop moved it elsewhere.
		var actualFocused = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot);
		Assert.AreSame(
			button,
			actualFocused,
			"XAML focus was moved away from the button; possible focus feedback loop.");

		// Native node tracker must reflect the button.
		var nativeNode = GetFocusedNativeNode(button.XamlRoot!);
		Assert.IsNotNull(
			nativeNode,
			"IOSFocusedNativeNodeAccessor returned null after XAML focus was set on the button.");

		// Snapshot must be present in the live tree.
		var snapshot = AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(button);
		Assert.IsNotNull(snapshot, "Snapshot for the XAML-focused button was not found in the tree.");
	}

	// Removed-node recovery

	/// <summary>
	/// Verifies that when the natively-focused element is removed from the visual tree the
	/// adapter clears the stale focus handle and the element is absent from the registry,
	/// so no stale <c>UIAccessibilityElement</c> lingers in the container.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NativeFocused_Element_Is_Removed_Then_StaleHandle_Is_Cleared()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityFocusAccessor,
			"iOS accessibility focus hook was not registered.");

		var panel = new StackPanel();
		var focusTarget = new Button { Content = "Will Be Removed" };
		var survivor = new Button { Content = "Survivor" };
		panel.Children.Add(focusTarget);
		panel.Children.Add(survivor);
		await UITestHelper.Load(panel);

		// Establish native focus on the element that will be removed.
		var focusRequested = RequestNativeFocus(focusTarget);
		Assert.IsTrue(focusRequested, "Focus request failed for the target button.");
		await TestServices.WindowHelper.WaitForIdle();

		// Confirm it has a snapshot before removal.
		var snapshotBefore =
			AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(focusTarget);
		Assert.IsNotNull(snapshotBefore, "Target button should have a snapshot before removal.");

		// Remove the focused element.
		var root = focusTarget.XamlRoot!;
		panel.Children.Remove(focusTarget);
		await TestServices.WindowHelper.WaitForIdle();

		// Removed element must no longer appear in the registry.
		var snapshotAfter =
			AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(focusTarget);
		Assert.IsNull(
			snapshotAfter,
			"Removed element still has a snapshot; stale entry in the native registry.");

		// The stale handle must have been cleared.
		var focusedNode = GetFocusedNativeNode(root);
		Assert.IsNull(
			focusedNode,
			"IOSFocusedNativeNodeAccessor still points to the removed element.");

		// Survivor must remain accessible.
		var survivorSnapshot =
			AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(survivor);
		Assert.IsNotNull(survivorSnapshot, "Survivor button disappeared after sibling removal.");

		// Overall element list must not contain the removed element.
		var allSnapshots = GetOrderedSnapshots(survivor.XamlRoot!);
		Assert.IsFalse(
			allSnapshots.Any(s => s.Name == "Will Be Removed"),
			"Removed element still appears in the ordered snapshot list.");
	}

	// Modal background exclusion

	/// <summary>
	/// Verifies that when a <see cref="Microsoft.UI.Xaml.Controls.Primitives.Popup"/>
	/// with <c>IsLightDismissEnabled=false</c> is open, background elements are excluded
	/// even though <c>PopupAutomationPeer</c> does not expose the Window pattern.
	/// The full element list is restored when the popup closes.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Modal_Popup_Opens_Then_Background_Elements_Are_Excluded()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor,
			"iOS all-snapshots hook was not registered.");

		const string backgroundName = "BackgroundBtn";
		const string modalName = "ModalBtn";

		var backgroundButton = new Button { Content = backgroundName };
		var modalButton = new Button { Content = modalName };

		var popup = new Popup
		{
			IsLightDismissEnabled = false,
			Child = new Border { Child = modalButton },
		};

		var grid = new Grid();
		grid.Children.Add(backgroundButton);
		grid.Children.Add(popup);
		await UITestHelper.Load(grid);

		var xamlRoot = grid.XamlRoot!;

		// Before the popup is open: background button must be accessible.
		var snapshotsBefore = GetOrderedSnapshots(xamlRoot);
		Assert.IsTrue(
			snapshotsBefore.Any(s => s.Name == backgroundName),
			$"'{backgroundName}' should be accessible before the popup opens.");

		// Opening the popup triggers a tree rebuild and modal detection.
		popup.IsOpen = true;
		Assert.IsFalse(
			AccessibilityPeerHelper.IOSAccessibilityActionAccessor?.Invoke(
				backgroundButton,
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate)) is true,
			"A background action must be rejected immediately when the modal popup opens.");
		await TestServices.WindowHelper.WaitForIdle();

		// With the modal active: background must be excluded.
		var snapshotsModal = GetOrderedSnapshots(xamlRoot);
		Assert.IsFalse(
			snapshotsModal.Any(s => s.Name == backgroundName),
			$"'{backgroundName}' is still accessible while the modal popup is open; background not excluded.");

		Assert.IsTrue(
			snapshotsModal.Any(s => s.Name == modalName),
			$"'{modalName}' is absent from the snapshot list while the modal popup is open.");

		// Close the popup: full element list must be restored.
		popup.IsOpen = false;
		await TestServices.WindowHelper.WaitForIdle();

		var snapshotsAfter = GetOrderedSnapshots(xamlRoot);
		Assert.IsTrue(
			snapshotsAfter.Any(s => s.Name == backgroundName),
			$"'{backgroundName}' was not restored after the modal popup closed.");
	}
}
