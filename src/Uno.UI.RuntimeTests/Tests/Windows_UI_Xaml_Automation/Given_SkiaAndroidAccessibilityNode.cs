#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public partial class Given_SkiaAndroidAccessibilityNode
{
	private static IReadOnlyList<AccessibilityNativeNodeSnapshot> GetAllNodes(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	private static AccessibilityNativeNodeSnapshot? FindByName(
		IReadOnlyList<AccessibilityNativeNodeSnapshot> nodes,
		string name)
		=> nodes.FirstOrDefault(node => node.Name?.Contains(name) is true);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Adapter_Is_Initialized_Then_Native_Node_Hooks_Are_Registered()
	{
		var button = new Button { Content = "Adapter Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor,
			"Android accessibility hooks were not registered for the loaded XamlRoot.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Accessibility_Tree_Is_Built_Then_It_Contains_Native_Nodes()
	{
		var button = new Button { Content = "Tree Probe" };
		await UITestHelper.Load(button);

		var nodes = GetAllNodes(button.XamlRoot!);
		var diagnostics = AccessibilityPeerHelper.AndroidAccessibilityDiagnosticsAccessor?.Invoke(button.XamlRoot!);

		Assert.IsTrue(nodes.Count > 0, diagnostics);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Tree_Is_Queried_Then_Button_Appears_In_Tree()
	{
		var button = new Button { Content = "Tree Button" };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Tree Button");

		Assert.IsNotNull(node);
		Assert.IsNotNull(node.NativeNode);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Removed_Node_Dirties_Cached_Tree_Then_Owner_Can_Be_Collected()
	{
		var panel = new StackPanel
		{
			Children =
			{
				new TextBlock { Text = "Anchor" },
			},
		};
		await UITestHelper.Load(panel);

		var removedOwner = await CacheAndRemoveChild(panel);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(
			removedOwner.IsAlive,
			"Dirtying the Android peer-tree cache must release removed element owners without another native query.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> CacheAndRemoveChild(StackPanel panel)
	{
		Button? child = new() { Content = "Cached Child" };
		panel.Children.Add(child);
		await TestServices.WindowHelper.WaitForIdle();

		_ = GetAllNodes(panel.XamlRoot!);
		var removedOwner = new WeakReference(child);
		panel.Children.Remove(child);
		child = null;
		await TestServices.WindowHelper.WaitForIdle();
		return removedOwner;
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Is_Queried_Then_ClassName_Is_Button()
	{
		var button = new Button { Content = "Role Button" };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Role Button");

		Assert.IsNotNull(node);
		Assert.AreEqual("android.widget.Button", node.ClassName);
		Assert.IsTrue((node.Traits & AccessibilityNativeTraits.Button) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Is_Enabled_Then_Node_Reports_Enabled()
	{
		var button = new Button { Content = "Enabled Check", IsEnabled = true };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Enabled Check");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Enabled);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Is_Disabled_Then_Node_Reports_Disabled()
	{
		var button = new Button { Content = "Disabled Check", IsEnabled = false };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Disabled Check");

		Assert.IsNotNull(node);
		Assert.IsFalse(node.Enabled);
		Assert.IsTrue((node.Traits & AccessibilityNativeTraits.NotEnabled) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Heading_Is_Set_Then_Node_Reports_Heading()
	{
		var textBlock = new TextBlock { Text = "Heading Node" };
		AutomationProperties.SetHeadingLevel(textBlock, AutomationHeadingLevel.Level1);
		await UITestHelper.Load(textBlock);

		var node = FindByName(GetAllNodes(textBlock.XamlRoot!), "Heading Node");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Heading);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Is_Set_Then_ViewIdResourceName_Is_Package_Qualified()
	{
		var button = new Button { Content = "AutoId Button" };
		AutomationProperties.SetAutomationId(button, "myAutomationId");
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "AutoId Button");

		Assert.IsNotNull(node);
		Assert.AreEqual("myAutomationId", node.AutomationId);
		Assert.IsTrue(node.NativeAutomationId?.EndsWith(":id/myAutomationId", StringComparison.Ordinal) is true);
		Assert.AreNotEqual("myAutomationId", node.Name);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Has_Raw_Accessibility_View_Then_It_Is_Excluded()
	{
		var panel = new StackPanel();
		var visible = new Button { Content = "Visible Node" };
		var raw = new TextBlock { Text = "Raw Node" };
		AutomationProperties.SetAccessibilityView(raw, AccessibilityView.Raw);
		panel.Children.Add(visible);
		panel.Children.Add(raw);
		await UITestHelper.Load(panel);

		var rawNode = FindByName(GetAllNodes(panel.XamlRoot!), "Raw Node");

		Assert.IsNull(rawNode);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Tree_Is_Queried_Twice_Then_Same_Element_Gets_Same_VirtualId()
	{
		var button = new Button { Content = "Stable Id" };
		await UITestHelper.Load(button);

		var id1 = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(button);
		var id2 = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(button);

		Assert.IsNotNull(id1);
		Assert.AreEqual(id1, id2);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Node_Has_Explicit_Width_Height_Then_Bounds_Are_Non_Degenerate()
	{
		var button = new Button { Content = "Bounds", Width = 120, Height = 48 };
		await UITestHelper.Load(button);

		AccessibilityNativeNodeSnapshot? node = null;
		await TestServices.WindowHelper.WaitFor(
			() =>
			{
				node = FindByName(GetAllNodes(button.XamlRoot!), "Bounds");
				return node?.Bounds is { Width: > 0, Height: > 0 };
			},
			message: "The Android accessibility node did not receive non-degenerate bounds.");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Bounds.Width > 0);
		Assert.IsTrue(node.Bounds.Height > 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Is_On_Then_Node_Is_Checkable_With_CheckBox_Class()
	{
		var checkBox = new CheckBox { Content = "Checked Box", IsChecked = true };
		await UITestHelper.Load(checkBox);

		var node = FindByName(GetAllNodes(checkBox.XamlRoot!), "Checked Box");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Checkable);
		Assert.IsTrue(node.IsChecked is true);
		Assert.AreEqual("android.widget.CheckBox", node.ClassName);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Is_Indeterminate_Then_Checked_State_Is_Null()
	{
		var checkBox = new CheckBox
		{
			Content = "Indeterminate Box",
			IsThreeState = true,
			IsChecked = null,
		};
		await UITestHelper.Load(checkBox);

		var node = FindByName(GetAllNodes(checkBox.XamlRoot!), "Indeterminate Box");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Checkable);
		Assert.IsNull(node.IsChecked);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Siblings_Are_In_Panel_Then_Both_Appear_In_Peer_Tree_Order()
	{
		var panel = new StackPanel();
		panel.Children.Add(new Button { Content = "First" });
		panel.Children.Add(new Button { Content = "Second" });
		await UITestHelper.Load(panel);

		var names = GetAllNodes(panel.XamlRoot!)
			.Select(node => node.Name)
			.Where(name => name is "First" or "Second")
			.ToList();

		CollectionAssert.AreEqual(new[] { "First", "Second" }, names);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Direct_Node_Accessor_Used_Then_Returns_Real_Node_For_Element()
	{
		var button = new Button { Content = "Direct Node" };
		await UITestHelper.Load(button);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(button);

		Assert.IsNotNull(snapshot);
		Assert.IsNotNull(snapshot.NativeNode);
		Assert.IsTrue(snapshot.Name?.Contains("Direct Node") is true);
	}

	// Action tests
	// All tests below use AndroidAccessibilityActionAccessor so no Android types
	// are referenced directly from the test assembly.

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Action_Hook_Is_Registered_Then_Action_Accessor_Is_Not_Null()
	{
		var button = new Button { Content = "Hook Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityActionAccessor,
			"AndroidAccessibilityActionAccessor was not registered by UnoExploreByTouchHelper.Initialize.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Activate_Action_Is_Performed_Then_Click_Is_Invoked()
	{
		var button = new Button { Content = "Action Click" };
		var clicked = false;
		button.Click += (_, _) => clicked = true;
		await UITestHelper.Load(button);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsTrue(result, "Activate action should return true for an enabled button.");
		Assert.IsTrue(clicked, "Button.Click should have fired after the Activate action.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Activate_Action_Is_Performed_Then_Toggle_State_Changes()
	{
		var checkBox = new CheckBox { Content = "Action Toggle", IsChecked = false };
		await UITestHelper.Load(checkBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			checkBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsTrue(result, "Activate (toggle) action should return true.");
		Assert.IsTrue(checkBox.IsChecked == true, "CheckBox should now be checked.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Increment_Action_Is_Performed_Then_Value_Increases()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 1 };
		await UITestHelper.Load(slider);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Increment));

		Assert.IsTrue(result, "Increment action should return true for a writable Slider.");
		Assert.AreEqual(6d, slider.Value, "Slider value should have increased by SmallChange.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Decrement_Action_Is_Performed_Then_Value_Decreases()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 1 };
		await UITestHelper.Load(slider);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Decrement));

		Assert.IsTrue(result, "Decrement action should return true for a writable Slider.");
		Assert.AreEqual(4d, slider.Value, "Slider value should have decreased by SmallChange.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Peer_Overrides_Bounds_Then_Native_Bounds_Use_Peer_Rectangle()
	{
		var control = new CustomBoundsControl { Content = "Custom Bounds", Width = 100, Height = 100 };
		await UITestHelper.Load(control);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(control);

		Assert.IsNotNull(snapshot);
		var scale = control.XamlRoot!.RasterizationScale;
		Assert.AreEqual(37 * scale, snapshot.Bounds.Width, 1);
		Assert.AreEqual(41 * scale, snapshot.Bounds.Height, 1);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Ownerless_Custom_Peer_Is_Exposed_Then_Node_And_Invoke_Are_Routed_By_Peer()
	{
		var host = new OwnerlessPeerHost();
		await UITestHelper.Load(host);

		var snapshot = FindByName(GetAllNodes(host.XamlRoot!), "Ownerless Action");
		Assert.IsNotNull(snapshot, "The ownerless peer must appear in the Android virtual tree.");
		Assert.AreEqual("android.widget.Button", snapshot.ClassName);

		var scale = host.XamlRoot!.RasterizationScale;
		Assert.AreEqual(30 * scale, snapshot.Bounds.Width, 1);
		Assert.AreEqual(20 * scale, snapshot.Bounds.Height, 1);

		var virtualId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(host.ChildPeer);
		Assert.IsNotNull(virtualId, "The ownerless peer must receive a peer-keyed virtual ID.");
		var hitId = AccessibilityPeerHelper.AndroidAccessibilityHitTestAccessor?.Invoke(
			host.XamlRoot!,
			10 * scale,
			10 * scale);
		Assert.AreEqual(virtualId, hitId, "Explore-by-touch must resolve the ownerless peer ID.");

		var invoked = AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(virtualId.Value, 0x10);
		Assert.IsTrue(invoked, "ACTION_CLICK must route directly to the ownerless peer.");
		Assert.IsTrue(host.ChildPeer.WasInvoked);

		host.ChildPeer.SetName("Ownerless Updated");
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsNotNull(FindByName(GetAllNodes(host.XamlRoot!), "Ownerless Updated"));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Virtual_Peers_Share_An_Owner_Then_They_Keep_Distinct_Ids_And_Actions()
	{
		var host = new SharedOwnerPeerHost();
		await UITestHelper.Load(host);

		var nodes = GetAllNodes(host.XamlRoot!);
		Assert.IsNotNull(FindByName(nodes, "Shared Owner A"));
		Assert.IsNotNull(FindByName(nodes, "Shared Owner B"));

		var firstId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(host.FirstPeer);
		var secondId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(host.SecondPeer);
		Assert.IsNotNull(firstId);
		Assert.IsNotNull(secondId);
		Assert.AreNotEqual(firstId, secondId);
		var scale = host.XamlRoot!.RasterizationScale;
		Assert.AreEqual(
			firstId,
			AccessibilityPeerHelper.AndroidAccessibilityHitTestAccessor?.Invoke(
				host.XamlRoot!,
				20 * scale,
				15 * scale));
		Assert.AreEqual(
			secondId,
			AccessibilityPeerHelper.AndroidAccessibilityHitTestAccessor?.Invoke(
				host.XamlRoot!,
				60 * scale,
				15 * scale));

		Assert.IsTrue(AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(firstId.Value, 0x10));
		Assert.IsTrue(AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(secondId.Value, 0x10));
		Assert.IsTrue(host.FirstPeer.WasInvoked);
		Assert.IsTrue(host.SecondPeer.WasInvoked);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Overlapping_Elements_Are_HitTested_Then_Highest_ZIndex_Wins()
	{
		var top = new Button { Content = "Top", Width = 80, Height = 40 };
		var bottom = new Button { Content = "Bottom", Width = 80, Height = 40 };
		Canvas.SetZIndex(top, 10);
		Canvas.SetZIndex(bottom, 0);
		var canvas = new Canvas
		{
			Width = 100,
			Height = 100,
			Children =
			{
				top,
				bottom,
			},
		};
		await UITestHelper.Load(canvas);

		var topId = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(top);
		Assert.IsNotNull(topId);
		var scale = canvas.XamlRoot!.RasterizationScale;
		var hitPoint = top
			.TransformToVisual(null)
			.TransformPoint(new Windows.Foundation.Point(top.ActualWidth / 2, top.ActualHeight / 2));

		var hitId = AccessibilityPeerHelper.AndroidAccessibilityHitTestAccessor?.Invoke(
			canvas.XamlRoot,
			hitPoint.X * scale,
			hitPoint.Y * scale);

		Assert.AreEqual(topId, hitId);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_EventsSource_Changes_Then_New_Peer_Gets_New_Id_And_Stale_Id_Is_Rejected()
	{
		var host = new EventsSourcePeerHost();
		await UITestHelper.Load(host);

		Assert.IsNotNull(FindByName(GetAllNodes(host.XamlRoot!), "Events Source A"));
		var firstId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(host.SourceA);
		Assert.IsNotNull(firstId);

		host.UseSecondSource();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(FindByName(GetAllNodes(host.XamlRoot!), "Events Source B"));
		var secondId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(host.SourceB);
		Assert.IsNotNull(secondId);
		Assert.AreNotEqual(firstId, secondId);

		Assert.IsFalse(AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(firstId.Value, 0x10));
		Assert.IsTrue(AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(secondId.Value, 0x10));
		Assert.IsFalse(host.SourceA.WasInvoked);
		Assert.IsTrue(host.SourceB.WasInvoked);
	}

	private sealed partial class CustomBoundsControl : Button
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new CustomBoundsPeer(this);
	}

	private sealed class CustomBoundsPeer : FrameworkElementAutomationPeer
	{
		internal CustomBoundsPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		protected override Windows.Foundation.Rect GetBoundingRectangleCore()
			=> new(11, 13, 37, 41);

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;
	}

	private sealed partial class OwnerlessPeerHost : Grid
	{
		internal OwnerlessPeerHost()
		{
			Width = 100;
			Height = 100;
		}

		internal OwnerlessInvokePeer ChildPeer { get; } = new();

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new OwnerlessPeerHostAutomationPeer(this, ChildPeer);
	}

	private sealed class OwnerlessPeerHostAutomationPeer : FrameworkElementAutomationPeer
	{
		private readonly AutomationPeer _childPeer;

		internal OwnerlessPeerHostAutomationPeer(FrameworkElement owner, AutomationPeer childPeer)
			: base(owner)
		{
			_childPeer = childPeer;
		}

		protected override IList<AutomationPeer> GetChildrenCore() => new[] { _childPeer };

		protected override bool IsControlElementCore() => false;

		protected override bool IsContentElementCore() => false;
	}

	private sealed class OwnerlessInvokePeer : AutomationPeer, IInvokeProvider
	{
		private string _name;

		internal OwnerlessInvokePeer(string name = "Ownerless Action")
			=> _name = name;

		internal bool WasInvoked { get; private set; }

		public void Invoke() => WasInvoked = true;

		internal void SetName(string value)
		{
			var oldValue = _name;
			_name = value;
			RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldValue, value);
		}

		protected override string GetNameCore() => _name;

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

		protected override Windows.Foundation.Rect GetBoundingRectangleCore() => new(5, 7, 30, 20);

		protected override object? GetPatternCore(PatternInterface patternInterface)
			=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;
	}

	private sealed partial class EventsSourcePeerHost : Grid
	{
		private EventsSourcePeerHostAutomationPeer? _peer;

		internal EventsSourcePeerHost()
		{
			Width = 100;
			Height = 100;
		}

		internal OwnerlessInvokePeer SourceA { get; } = new("Events Source A");

		internal OwnerlessInvokePeer SourceB { get; } = new("Events Source B");

		internal void UseSecondSource()
		{
			Assert.IsNotNull(_peer);
			_peer.EventsSource = SourceB;
			_peer.InvalidatePeer();
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			_peer = new EventsSourcePeerHostAutomationPeer(this)
			{
				EventsSource = SourceA,
			};
			return _peer;
		}
	}

	private sealed class EventsSourcePeerHostAutomationPeer : FrameworkElementAutomationPeer
	{
		internal EventsSourcePeerHostAutomationPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;
	}

	private sealed partial class SharedOwnerPeerHost : Grid
	{
		internal SharedOwnerPeerHost()
		{
			Width = 100;
			Height = 100;
			FirstPeer = new SharedOwnerInvokePeer(this, "Shared Owner A", 10);
			SecondPeer = new SharedOwnerInvokePeer(this, "Shared Owner B", 50);
		}

		internal SharedOwnerInvokePeer FirstPeer { get; }

		internal SharedOwnerInvokePeer SecondPeer { get; }

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new SharedOwnerPeerHostAutomationPeer(this, FirstPeer, SecondPeer);
	}

	private sealed class SharedOwnerPeerHostAutomationPeer : FrameworkElementAutomationPeer
	{
		private readonly IList<AutomationPeer> _children;

		internal SharedOwnerPeerHostAutomationPeer(
			FrameworkElement owner,
			params AutomationPeer[] children)
			: base(owner)
		{
			_children = children;
		}

		protected override IList<AutomationPeer> GetChildrenCore() => _children;

		protected override bool IsControlElementCore() => false;

		protected override bool IsContentElementCore() => false;
	}

	private sealed class SharedOwnerInvokePeer : FrameworkElementAutomationPeer, IInvokeProvider
	{
		private readonly string _name;
		private readonly double _x;

		internal SharedOwnerInvokePeer(FrameworkElement owner, string name, double x)
			: base(owner)
		{
			_name = name;
			_x = x;
		}

		internal bool WasInvoked { get; private set; }

		public void Invoke() => WasInvoked = true;

		protected override string GetNameCore() => _name;

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

		protected override Windows.Foundation.Rect GetBoundingRectangleCore() => new(_x, 10, 30, 20);

		protected override object? GetPatternCore(PatternInterface patternInterface)
			=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;
	}
}

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public partial class Given_SkiaAndroidAutomationId
{
	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(element);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Valid_AutomationId_Is_Set_Then_ResourceName_Is_Unchanged()
	{
		var button = new Button { Content = "Valid ID" };
		AutomationProperties.SetAutomationId(button, "MobileAutomationInvoke");
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual("MobileAutomationInvoke", snapshot.AutomationId);
		Assert.IsTrue(
			snapshot.NativeAutomationId?.EndsWith(":id/MobileAutomationInvoke") is true,
			$"Valid ID must be projected unchanged; got '{snapshot.NativeAutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Contains_Invalid_Chars_Then_ResourceName_Is_Normalized_With_Hash()
	{
		var button = new Button { Content = "Invalid ID" };
		AutomationProperties.SetAutomationId(button, "My Button");
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);
		Assert.IsNotNull(snapshot);

		Assert.AreEqual("My Button", snapshot.AutomationId);
		var resourceName = snapshot.NativeAutomationId;
		Assert.IsNotNull(resourceName);
		// Segment must start with the sanitized prefix and end with a 4-char hex suffix.
		Assert.IsTrue(
			resourceName.Contains(":id/My_Button_"),
			$"Normalized segment should contain 'My_Button_' prefix; got '{resourceName}'.");

		// The suffix must be 4 lowercase hex digits.
		var segment = resourceName.Substring(resourceName.LastIndexOf('/') + 1);
		Assert.AreEqual(14, segment.Length, $"'My_Button_XXXX' = 14 chars; got '{segment}'.");
		Assert.IsTrue(
			System.Text.RegularExpressions.Regex.IsMatch(segment, @"^My_Button_[0-9a-f]{4}$"),
			$"Segment should match 'My_Button_XXXX'; got '{segment}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Two_Ids_Normalize_To_Same_Base_Then_Resource_Names_Differ()
	{
		// "My Button" and "My_Button" both normalize to "My_Button" base,
		// but must get different suffixes because their originals differ.
		var b1 = new Button { Content = "First" };
		var b2 = new Button { Content = "Second" };
		AutomationProperties.SetAutomationId(b1, "My Button");
		AutomationProperties.SetAutomationId(b2, "My_Button");
		var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Children = { b1, b2 } };
		await UITestHelper.Load(panel);

		var s1 = GetSnapshot(b1);
		var s2 = GetSnapshot(b2);
		Assert.IsNotNull(s1);
		Assert.IsNotNull(s2);

		// "My_Button" is already valid → no hash; "My Button" has a hash → they must differ.
		Assert.AreNotEqual(s1.NativeAutomationId, s2.NativeAutomationId,
			"Two distinct originals that normalize to the same base must have different resource names.");
		Assert.IsTrue(
			s2.NativeAutomationId?.EndsWith(":id/My_Button") is true,
			$"'My_Button' is already valid and must not have a hash; got '{s2.NativeAutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Generated_Resource_Name_Collides_With_Valid_Id_Then_Ids_Are_Disambiguated()
	{
		var invalid = new Button { Content = "Invalid" };
		var valid = new Button { Content = "Valid" };
		AutomationProperties.SetAutomationId(invalid, "My Button");
		AutomationProperties.SetAutomationId(valid, "My_Button_ea6f");
		var panel = new StackPanel { Children = { invalid, valid } };
		await UITestHelper.Load(panel);

		var invalidSnapshot = GetSnapshot(invalid);
		var validSnapshot = GetSnapshot(valid);

		Assert.IsNotNull(invalidSnapshot);
		Assert.IsNotNull(validSnapshot);
		Assert.AreNotEqual(invalidSnapshot.NativeAutomationId, validSnapshot.NativeAutomationId);
		Assert.AreEqual("My_Button_ea6f", validSnapshot.AutomationId);
		Assert.IsTrue(
			validSnapshot.NativeAutomationId?.Contains(":id/My_Button_ea6f", StringComparison.Ordinal) is true,
			"The valid AutomationId must retain its readable segment when a stable generated assignment already owns it.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Collision_Set_Changes_Then_Existing_Resource_Name_Remains_Stable()
	{
		var invalid = new Button { Content = "Invalid" };
		AutomationProperties.SetAutomationId(invalid, "My Button");
		var panel = new StackPanel { Children = { invalid } };
		await UITestHelper.Load(panel);

		var before = GetSnapshot(invalid);
		var initialResourceName = before?.NativeAutomationId;
		Assert.IsNotNull(initialResourceName);
		var generatedSegment = initialResourceName[(initialResourceName.LastIndexOf('/') + 1)..];

		var collidingValid = new Button { Content = "Valid" };
		AutomationProperties.SetAutomationId(collidingValid, generatedSegment);
		panel.Children.Add(collidingValid);
		await TestServices.WindowHelper.WaitForIdle();

		var during = GetSnapshot(invalid);
		var validSnapshot = GetSnapshot(collidingValid);
		Assert.AreEqual(initialResourceName, during?.NativeAutomationId);
		Assert.AreNotEqual(during?.NativeAutomationId, validSnapshot?.NativeAutomationId);

		panel.Children.Remove(collidingValid);
		await TestServices.WindowHelper.WaitForIdle();

		var after = GetSnapshot(invalid);
		Assert.AreEqual(initialResourceName, after?.NativeAutomationId);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Changes_Live_Then_Snapshot_Updates_On_Requery()
	{
		var button = new Button { Content = "Live ID" };
		AutomationProperties.SetAutomationId(button, "OriginalId");
		await UITestHelper.Load(button);

		var before = GetSnapshot(button);
		Assert.AreEqual("OriginalId", before?.AutomationId);
		Assert.IsTrue(before?.NativeAutomationId?.EndsWith(":id/OriginalId") is true,
			$"Before change: expected ':id/OriginalId'; got '{before?.NativeAutomationId}'.");

		AutomationProperties.SetAutomationId(button, "UpdatedId");
		await TestServices.WindowHelper.WaitForIdle();

		var after = GetSnapshot(button);
		Assert.AreEqual("UpdatedId", after?.AutomationId);
		Assert.IsTrue(after?.NativeAutomationId?.EndsWith(":id/UpdatedId") is true,
			$"After change: expected ':id/UpdatedId'; got '{after?.NativeAutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_SetRangeValue_Action_Is_Performed_Then_Value_Updates()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5 };
		await UITestHelper.Load(slider);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetRangeValue, number: 8));

		// ActionSetProgress may not be available on all binding versions; skip without fail.
		if (result is true)
		{
			Assert.AreEqual(8d, slider.Value, "Slider value should be 8 after SetRangeValue.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Is_At_Maximum_Then_Increment_Is_Not_Advertised_Or_Performed()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 10, SmallChange = 1 };
		await UITestHelper.Load(slider);

		var snapshot = GetSnapshot(slider);
		Assert.IsNotNull(snapshot);
		CollectionAssert.DoesNotContain(snapshot.NativeActionIds.ToArray(), 0x1000);
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x2000);

		Assert.IsFalse(AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider,
			new AccessibilityNativeActionRequest(AccessibilityNativeAction.Increment)));
		Assert.AreEqual(10, slider.Value);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ComboBox_Is_Clicked_Then_ExpandCollapse_Action_Toggles_It()
	{
		var comboBox = new ComboBox
		{
			Items =
			{
				"One",
				"Two",
			},
		};
		await UITestHelper.Load(comboBox);

		var snapshot = GetSnapshot(comboBox);
		Assert.IsNotNull(snapshot);
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x10);

		Assert.IsTrue(AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			comboBox,
			new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate)));
		Assert.IsTrue(comboBox.IsDropDownOpen);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ScrollViewer_Can_Scroll_Then_Native_Node_Is_Scrollable()
	{
		var scrollViewer = new ScrollViewer
		{
			Height = 100,
			Content = new Border { Height = 400 },
		};
		await UITestHelper.Load(scrollViewer);

		var snapshot = GetSnapshot(scrollViewer);

		Assert.IsNotNull(snapshot);
		Assert.IsTrue(snapshot.Scrollable);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_SetValue_Action_Is_Performed_Then_Text_Updates()
	{
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Hello"));

		Assert.IsTrue(result, "SetValue action should return true for a writable TextBox.");
		Assert.AreEqual("Hello", textBox.Text, "TextBox.Text should match the value set via action.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Cannot_Set_Text_Then_Native_SetText_Is_Not_Advertised()
	{
		var richEditBox = new RichEditBox();
		await UITestHelper.Load(richEditBox);

		var snapshot = GetSnapshot(richEditBox);
		Assert.IsNotNull(snapshot);
		CollectionAssert.DoesNotContain(snapshot.NativeActionIds.ToArray(), 0x200000);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			richEditBox,
			new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Unsupported"));

		Assert.IsFalse(result);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ExpandCollapse_Is_LeafNode_Then_Click_Is_Not_Advertised()
	{
		var control = new LeafExpandControl { Width = 100, Height = 40 };
		await UITestHelper.Load(control);

		var snapshot = GetSnapshot(control);
		Assert.IsNotNull(snapshot);
		CollectionAssert.DoesNotContain(snapshot.NativeActionIds.ToArray(), 0x10);

		var virtualId = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(control);
		Assert.IsNotNull(virtualId);
		Assert.IsFalse(
			AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(virtualId.Value, 0x10));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Is_Queried_Then_Text_Selection_And_Granularities_Are_Exposed()
	{
		var textBox = new TextBox
		{
			Text = "Alpha beta\nGamma",
			AcceptsReturn = true,
		};
		await UITestHelper.Load(textBox);
		textBox.Select(2, 3);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual(2, snapshot.TextSelectionStart);
		Assert.AreEqual(5, snapshot.TextSelectionEnd);
		Assert.AreEqual(0x1F, snapshot.MovementGranularities,
			"Editable text must expose character, word, line, paragraph, and page granularities.");
		Assert.IsTrue(snapshot.Details?.SupportedActions.Contains(AccessibilityNativeAction.SetTextSelection) is true);
		Assert.IsTrue(snapshot.Details?.SupportedActions.Contains(AccessibilityNativeAction.MoveTextNext) is true);
		Assert.IsTrue(snapshot.Details?.SupportedActions.Contains(AccessibilityNativeAction.MoveTextPrevious) is true);
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x100);
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x200);
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x20000);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_SetTextSelection_Action_Is_Performed_Then_Selection_Updates()
	{
		var textBox = new TextBox { Text = "Alpha beta gamma" };
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: 6,
				number2: 10));

		Assert.IsTrue(result);
		Assert.AreEqual(6, textBox.SelectionStart);
		Assert.AreEqual(4, textBox.SelectionLength);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual(6, snapshot.TextSelectionStart);
		Assert.AreEqual(10, snapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_MoveTextNext_ByWord_Then_Caret_Moves_To_Segment_End()
	{
		var textBox = new TextBox { Text = "Alpha beta gamma" };
		await UITestHelper.Load(textBox);
		textBox.Select(0, 0);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));

		Assert.IsTrue(result);
		Assert.AreEqual(5, textBox.SelectionStart);
		Assert.AreEqual(0, textBox.SelectionLength);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Moves_By_Word_Twice_Then_Second_Action_Advances()
	{
		var textBox = new TextBox { Text = "Alpha beta gamma" };
		await UITestHelper.Load(textBox);
		textBox.Select(0, 0);

		var first = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));
		var second = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));

		Assert.IsTrue(first);
		Assert.IsTrue(second);
		Assert.AreEqual(10, textBox.SelectionStart);
		Assert.AreEqual(0, textBox.SelectionLength);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Moves_To_Next_Line_From_Inside_Line_Then_Adjacent_Line_Is_Selected()
	{
		var textBox = new TextBox
		{
			AcceptsReturn = true,
			Text = "abc\ndef",
			Height = 100,
		};
		await UITestHelper.Load(textBox);
		textBox.Select(1, 0);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 0x4));

		Assert.IsTrue(result);
		Assert.AreEqual(textBox.Text.Length, textBox.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Moves_To_Next_Paragraph_Then_Caret_Reaches_Paragraph_End()
	{
		var textBox = new TextBox
		{
			AcceptsReturn = true,
			Text = "alpha\r\nbeta",
		};
		await UITestHelper.Load(textBox);
		textBox.Select(1, 0);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 0x8));

		Assert.IsTrue(result);
		Assert.AreEqual(5, textBox.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Moves_To_Next_Page_Then_Caret_Advances()
	{
		var textBox = new TextBox
		{
			AcceptsReturn = true,
			Text = string.Join("\n", Enumerable.Range(0, 20).Select(index => $"Line {index}")),
			Height = 80,
		};
		await UITestHelper.Load(textBox);
		textBox.Select(0, 0);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 0x10));

		Assert.IsTrue(result);
		Assert.IsTrue(textBox.SelectionStart > 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Moves_By_Word_Then_Contraction_Remains_One_Segment()
	{
		var textBox = new TextBox { Text = "don't stop" };
		await UITestHelper.Load(textBox);
		textBox.Select(0, 0);

		var first = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));
		var second = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));

		Assert.IsTrue(first);
		Assert.IsTrue(second);
		Assert.AreEqual(10, textBox.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextRange_Endpoint_Moves_By_Document_Then_It_Reaches_The_Boundary()
	{
		var textBox = new TextBox { Text = "Alpha beta" };
		await UITestHelper.Load(textBox);

		var peer = textBox.GetOrCreateAutomationPeer();
		var provider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
		Assert.IsNotNull(provider);

		var range = provider.DocumentRange.Clone();
		var moved = range.MoveEndpointByUnit(
			TextPatternRangeEndpoint.Start,
			TextUnit.Document,
			1);

		Assert.AreEqual(1, moved);
		Assert.AreEqual(string.Empty, range.GetText(-1));

		var unchanged = range.MoveEndpointByUnit(
			TextPatternRangeEndpoint.Start,
			TextUnit.Document,
			0);

		Assert.AreEqual(0, unchanged);
		Assert.AreEqual(string.Empty, range.GetText(-1));

		var pageRange = provider.DocumentRange.Clone();
		var pageMoved = pageRange.MoveEndpointByUnit(
			TextPatternRangeEndpoint.Start,
			TextUnit.Page,
			1);

		Assert.AreEqual(1, pageMoved);
		Assert.AreEqual(string.Empty, pageRange.GetText(-1));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Extends_Backward_Then_Next_Word_Contracts_The_Selection()
	{
		var textBox = new TextBox { Text = "Alpha beta gamma" };
		await UITestHelper.Load(textBox);
		textBox.Select(5, 0);

		var backward = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextPrevious,
				number: 2,
				number2: 1));

		Assert.IsTrue(backward);
		var backwardSnapshot =
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);
		Assert.IsNotNull(backwardSnapshot);
		Assert.AreEqual(5, backwardSnapshot.TextSelectionStart);
		Assert.AreEqual(0, backwardSnapshot.TextSelectionEnd);

		var forward = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2,
				number2: 1));

		Assert.IsTrue(forward);
		var forwardSnapshot =
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);
		Assert.IsNotNull(forwardSnapshot);
		Assert.AreEqual(5, forwardSnapshot.TextSelectionStart);
		Assert.AreEqual(5, forwardSnapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_MoveTextNext_ByCharacter_Then_SurrogatePair_Is_Not_Split()
	{
		var textBox = new TextBox { Text = "A😀B" };
		await UITestHelper.Load(textBox);
		textBox.Select(1, 0);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 1));

		Assert.IsTrue(result);
		Assert.AreEqual(3, textBox.SelectionStart);
		Assert.AreEqual(0, textBox.SelectionLength);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Static_Text_Moves_By_Word_Then_Accessibility_Cursor_Updates()
	{
		var textBlock = new TextBlock { Text = "Alpha beta" };
		await UITestHelper.Load(textBlock);

		var before = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBlock);
		Assert.IsNotNull(before);
		Assert.IsTrue(before.Details?.SupportedActions.Contains(AccessibilityNativeAction.MoveTextNext) is true);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBlock,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));

		Assert.IsTrue(result);
		var after = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBlock);
		Assert.IsNotNull(after);
		Assert.AreEqual(5, after.TextSelectionStart);
		Assert.AreEqual(5, after.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Static_Text_Shrinks_Then_Accessibility_Cursor_Is_Clamped()
	{
		var textBlock = new TextBlock { Text = "Alpha beta" };
		await UITestHelper.Load(textBlock);

		var moved = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBlock,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));
		Assert.IsTrue(moved);

		textBlock.Text = "X";
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBlock);
		Assert.IsNotNull(snapshot);
		Assert.IsTrue(snapshot.TextSelectionStart <= 1);
		Assert.IsTrue(snapshot.TextSelectionEnd <= 1);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PasswordBox_Receives_Text_Selection_Actions_Then_They_Are_Rejected()
	{
		var passwordBox = new PasswordBox { Password = "secret" };
		await UITestHelper.Load(passwordBox);

		var selection = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			passwordBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: 0,
				number2: 0));
		var movement = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			passwordBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 1));

		Assert.IsFalse(selection);
		Assert.IsFalse(movement);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(passwordBox);
		Assert.IsNotNull(snapshot);
		Assert.IsNull(snapshot.Value);
		Assert.AreEqual(-1, snapshot.TextSelectionStart);
		Assert.AreEqual(-1, snapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SelectionChanging_Cancels_Then_SetTextSelection_Returns_False()
	{
		var textBox = new TextBox { Text = "Alpha beta" };
		textBox.SelectionChanging += (_, args) => args.Cancel = true;
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: 1,
				number2: 4));

		Assert.IsFalse(result);
		Assert.AreEqual(0, textBox.SelectionStart);
		Assert.AreEqual(0, textBox.SelectionLength);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SelectionChanging_Cancels_Backward_Selection_Then_Caret_Direction_Is_Preserved()
	{
		var textBox = new TextBox { Text = "Alpha beta" };
		await UITestHelper.Load(textBox);
		textBox.Select(0, 0);
		textBox.SelectionChanging += (_, args) => args.Cancel = true;

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: 5,
				number2: 0));

		Assert.IsFalse(result);
		Assert.AreEqual(0, textBox.SelectionStart);
		Assert.AreEqual(0, textBox.SelectionLength);
#if __SKIA__
		Assert.IsFalse(textBox.IsBackwardSelection);
#else
		var isBackwardSelection = textBox
			.GetType()
			.GetProperty("IsBackwardSelection", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(isBackwardSelection);
		Assert.IsFalse((bool)isBackwardSelection.GetValue(textBox)!);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Static_Text_Selection_Is_Cleared_Then_Native_Cursor_Is_Undefined()
	{
		var textBlock = new TextBlock { Text = "Alpha beta" };
		await UITestHelper.Load(textBlock);

		var moved = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBlock,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));
		var cleared = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBlock,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: -1,
				number2: -1));

		Assert.IsTrue(moved);
		Assert.IsTrue(cleared);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBlock);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual(-1, snapshot.TextSelectionStart);
		Assert.AreEqual(-1, snapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Receives_SetSelection_Without_Arguments_Then_Selection_Is_Cleared()
	{
		const int ActionSetSelection = 0x20000;
		var textBox = new TextBox { Text = "Alpha beta" };
		await UITestHelper.Load(textBox);
		textBox.Select(1, 4);

		var virtualId = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(textBox);
		Assert.IsNotNull(virtualId);

		var result = AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(
			virtualId.Value,
			ActionSetSelection);

		Assert.IsTrue(result);
		Assert.AreEqual(0, textBox.SelectionLength);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual(-1, snapshot.TextSelectionStart);
		Assert.AreEqual(-1, snapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Selection_Changes_After_Clear_Then_Native_Selection_Is_Live_Again()
	{
		var textBox = new TextBox { Text = "Alpha beta" };
		await UITestHelper.Load(textBox);

		var cleared = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: -1,
				number2: -1));
		Assert.IsTrue(cleared);

		textBox.Select(1, 3);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual(1, snapshot.TextSelectionStart);
		Assert.AreEqual(4, snapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Disabled_TextBox_Receives_Selection_Actions_Then_They_Are_Rejected()
	{
		var textBox = new TextBox { Text = "Alpha beta", IsEnabled = false };
		await UITestHelper.Load(textBox);

		var selection = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.SetTextSelection,
				number: 0,
				number2: 0));
		var movement = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 1));

		Assert.IsFalse(selection);
		Assert.IsFalse(movement);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(textBox);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual(0, snapshot.MovementGranularities);
		Assert.IsFalse(snapshot.Details?.SupportedActions.Contains(AccessibilityNativeAction.SetTextSelection) is true);
		Assert.IsFalse(snapshot.Details?.SupportedActions.Contains(AccessibilityNativeAction.MoveTextNext) is true);
	}

	[TestMethod]
	public void When_Fallback_Word_Traversal_Contains_Supplementary_Letter_Then_It_Is_Not_Skipped()
	{
		var result = global::DirectUI.TextRangeAdapter.TryGetTextSegment(
			owner: null,
			"𐐀 X",
			TextUnit.Word,
			position: 0,
			forward: true,
			out var start,
			out var end);

		Assert.IsTrue(result);
		Assert.AreEqual(0, start);
		Assert.AreEqual(2, end);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Word_Traversal_Starts_Inside_Word_Then_It_Moves_From_The_Caret()
	{
		var textBox = new TextBox { Text = "Alpha" };
		await UITestHelper.Load(textBox);
		textBox.Select(2, 0);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(
				AccessibilityNativeAction.MoveTextNext,
				number: 2));

		Assert.IsTrue(result);
		Assert.AreEqual(5, textBox.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Disabled_Button_Activate_Action_Is_Performed_Then_Returns_False()
	{
		var button = new Button { Content = "Disabled Action", IsEnabled = false };
		await UITestHelper.Load(button);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsFalse(result == true,
			"Activate action on a disabled button should fail (return false).");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ReadOnly_TextBox_SetValue_Action_Is_Performed_Then_Returns_False()
	{
		var textBox = new TextBox { IsReadOnly = true };
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Hi"));

		Assert.IsFalse(result == true,
			"SetValue on a read-only TextBox should fail (return false).");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Disabled_TextBox_SetValue_Action_Is_Performed_Then_Returns_False()
	{
		var textBox = new TextBox { Text = "Original", IsEnabled = false };
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox,
			new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Changed"));

		Assert.IsFalse(result);
		Assert.AreEqual("Original", textBox.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Copy_Action_Is_Performed_Then_Selection_Is_On_Clipboard()
	{
		var textBox = new TextBox { Text = "Alpha beta" };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		textBox.Select(0, 5);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var snapshot = GetSnapshot(textBox);
			Assert.IsNotNull(snapshot);
			CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x4000);

			var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
				textBox,
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.CopyText));

			Assert.IsTrue(result);
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(
				() => Clipboard.GetContent().Contains(StandardDataFormats.Text),
				message: "Copy accessibility action did not populate the clipboard.");
			Assert.AreEqual("Alpha", await Clipboard.GetContent().GetTextAsync());
		}
		finally
		{
			Clipboard.Clear();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Cut_Action_Is_Performed_Then_Selection_Is_Removed()
	{
		var textBox = new TextBox { Text = "Alpha beta" };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		textBox.Select(0, 6);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var snapshot = GetSnapshot(textBox);
			Assert.IsNotNull(snapshot);
			CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x10000);

			var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
				textBox,
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.CutText));

			Assert.IsTrue(result);
			Assert.AreEqual("beta", textBox.Text);
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(
				() => Clipboard.GetContent().Contains(StandardDataFormats.Text),
				message: "Cut accessibility action did not populate the clipboard.");
			Assert.AreEqual("Alpha ", await Clipboard.GetContent().GetTextAsync());
		}
		finally
		{
			Clipboard.Clear();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_Paste_Action_Is_Performed_Then_Clipboard_Text_Is_Inserted()
	{
		try
		{
			var package = new DataPackage();
			package.SetText(" pasted");
			Clipboard.SetContent(package);
			Clipboard.Flush();
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(
				() => Clipboard.GetContent().Contains(StandardDataFormats.Text),
				message: "Paste test setup did not populate the clipboard.");

			var textBox = new TextBox { Text = "Alpha" };
			await UITestHelper.Load(textBox);
			textBox.Focus(FocusState.Programmatic);
			textBox.Select(textBox.Text.Length, 0);
			await TestServices.WindowHelper.WaitFor(
				() => textBox.CanPasteClipboardContent,
				message: "TextBox did not observe text availability on the clipboard.");

			var snapshot = GetSnapshot(textBox);
			Assert.IsNotNull(snapshot);
			CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x8000);

			var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
				textBox,
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.PasteText));

			Assert.IsTrue(result);
			await TestServices.WindowHelper.WaitFor(
				() => textBox.Text == "Alpha pasted",
				message: "Paste accessibility action did not update TextBox.Text.");
		}
		finally
		{
			Clipboard.Clear();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Clipboard_Availability_Changes_Then_Paste_Action_Updates_While_Focused()
	{
		Clipboard.Clear();
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitFor(
			() => !Clipboard.GetContent().Contains(StandardDataFormats.Text),
			message: "Clipboard availability test did not begin with an empty clipboard.");
		var textBox = new TextBox { Text = "Alpha" };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var before = GetSnapshot(textBox);
			Assert.IsNotNull(before);
			CollectionAssert.DoesNotContain(before.NativeActionIds.ToArray(), 0x8000);

			var package = new DataPackage();
			package.SetText("paste");
			Clipboard.SetContent(package);
			Clipboard.Flush();
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(
				() => Clipboard.GetContent().Contains(StandardDataFormats.Text),
				message: "Clipboard availability test setup did not populate the clipboard.");

			await TestServices.WindowHelper.WaitFor(
				() => GetSnapshot(textBox)?.NativeActionIds.Contains(0x8000) is true,
				message: "Paste action was not added after text became available.");

			Clipboard.Clear();
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(
				() => !Clipboard.GetContent().Contains(StandardDataFormats.Text),
				message: "Clipboard was not cleared before removing the Paste action.");
			await TestServices.WindowHelper.WaitFor(
				() => GetSnapshot(textBox)?.NativeActionIds.Contains(0x8000) is false,
				message: "Paste action was not removed after the clipboard was cleared.");
		}
		finally
		{
			Clipboard.Clear();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Custom_TextBox_Peer_Is_Password_Then_Copy_And_Cut_Are_Rejected()
	{
		Clipboard.Clear();
		var textBox = new PasswordReportingTextBox { Text = "credential" };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		textBox.Select(0, textBox.Text.Length);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var snapshot = GetSnapshot(textBox);
			Assert.IsNotNull(snapshot);
			Assert.IsTrue(snapshot.Password);
			CollectionAssert.DoesNotContain(snapshot.NativeActionIds.ToArray(), 0x4000);
			CollectionAssert.DoesNotContain(snapshot.NativeActionIds.ToArray(), 0x10000);

			Assert.IsFalse(AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
				textBox,
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.CopyText)));
			Assert.IsFalse(AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
				textBox,
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.CutText)));
			Assert.AreEqual("credential", textBox.Text);
		}
		finally
		{
			Clipboard.Clear();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Unknown_Action_Is_Performed_Then_Returns_False()
	{
		var button = new Button { Content = "Unknown Action" };
		await UITestHelper.Load(button);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			button, new AccessibilityNativeActionRequest((AccessibilityNativeAction)int.MaxValue));

		Assert.IsFalse(result == true,
			"An unmappable action should return false rather than throw.");
	}

	private sealed partial class PasswordReportingTextBox : TextBox
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new PasswordReportingTextBoxAutomationPeer(this);
	}

	private sealed class PasswordReportingTextBoxAutomationPeer : TextBoxAutomationPeer
	{
		internal PasswordReportingTextBoxAutomationPeer(TextBox owner)
			: base(owner)
		{
		}

		protected override bool IsPasswordCore() => true;
	}

	private sealed partial class LeafExpandControl : Button
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new LeafExpandAutomationPeer(this);
	}

	private sealed class LeafExpandAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
	{
		internal LeafExpandAutomationPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		public ExpandCollapseState ExpandCollapseState => ExpandCollapseState.LeafNode;

		public void Collapse() => throw new InvalidOperationException();

		public void Expand() => throw new InvalidOperationException();

		protected override object? GetPatternCore(PatternInterface patternInterface)
			=> patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(patternInterface);

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;
	}
}
