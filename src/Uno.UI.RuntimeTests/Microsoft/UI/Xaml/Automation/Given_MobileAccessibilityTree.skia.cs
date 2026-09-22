#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public partial class Given_MobileAccessibilityTree
{
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false, false, false, false)]
	[DataRow(true, false, false, true)]
	[DataRow(false, true, false, true)]
	[DataRow(false, false, true, true)]
	public void When_Popup_Window_Pattern_Matches_Exposed_Surface(
		bool lightDismiss, bool contentDialog, bool submenu, bool expected)
	{
		var popup = new Popup
		{
			IsLightDismissEnabled = lightDismiss,
			IsContentDialog = contentDialog,
			IsSubMenu = submenu,
		};
		var peer = new PopupAutomationPeer(popup);
		Assert.AreEqual(expected, peer.GetPattern(PatternInterface.Window) is IWindowProvider);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Item_Occurrence_Bounds_Are_Queried_Then_Custom_Peer_Override_Is_Preserved()
	{
		var peer = new BoundsItemAutomationPeer(new object(), new ItemsControlAutomationPeer(new ListView()));
		var expected = peer.GetBoundingRectangle();

		Assert.AreEqual(expected, AccessibilityPeerHelper.GetBoundingRectangle(peer, new ListViewItem()));
		Assert.AreEqual(expected, peer.GetBoundingRectangle());
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Popup_Content_Has_A_Separate_Visual_Parent_Then_It_Remains_In_Modal_Scope()
	{
		var content = new Button { Content = "Modal" };
		var popup = new Popup { Child = new Border { Child = content } };

		Assert.IsTrue(AccessibilityPeerHelper.IsWithinModalScope(content, popup));
		Assert.IsFalse(AccessibilityPeerHelper.IsWithinModalScope(new Button(), popup));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Submenu_Is_Modal_Then_Only_Its_Owning_Menu_Remains_In_Scope()
	{
		var menu = new MenuFlyout();
		var parentPresenter = new MenuFlyoutPresenter();
		parentPresenter.SetParentMenuFlyout(menu);
		var submenuPresenter = new MenuFlyoutPresenter();
		submenuPresenter.SetParentMenuFlyout(menu);
		var otherMenu = new MenuFlyout();
		var otherPresenter = new MenuFlyoutPresenter();
		otherPresenter.SetParentMenuFlyout(otherMenu);
		var parentPopup = new Popup { Child = parentPresenter };
		var submenuPopup = new Popup { Child = submenuPresenter };
		var otherPopup = new Popup { Child = otherPresenter };

		Assert.IsTrue(AccessibilityPeerHelper.IsWithinModalScope(parentPopup, submenuPopup));
		Assert.IsTrue(AccessibilityPeerHelper.IsWithinModalScope(parentPresenter, submenuPopup));
		Assert.IsFalse(AccessibilityPeerHelper.IsWithinModalScope(otherPopup, submenuPopup));
		GC.KeepAlive(menu);
		GC.KeepAlive(otherMenu);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Transparent_Peer_Has_Child_Then_Child_Is_Promoted()
	{
		var root = new TestPeer("root", isControlElement: true);
		var transparent = new TestPeer("transparent");
		var child = new TestPeer("child", isContentElement: true);
		root.Children.Add(transparent);
		transparent.Children.Add(child);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		Assert.AreEqual(2, nodes.Count);
		Assert.AreSame(root, nodes[0].Peer);
		Assert.IsNull(nodes[0].ParentIndex);
		Assert.AreSame(child, nodes[1].Peer);
		Assert.AreEqual(0, nodes[1].ParentIndex);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Root_Element_Has_No_Peer_Then_Descendant_Peer_Is_Included()
	{
		var button = new Button { Content = "Descendant" };
		var root = new Grid { Children = { button } };
		await UITestHelper.Load(root);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		Assert.IsTrue(nodes.Any(node => ReferenceEquals(node.Owner, button)));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Transparent_Root_Peer_Has_No_Peer_Children_Then_Visual_Child_Is_Included()
	{
		var button = new Button { Content = "Descendant" };
		var root = new TransparentPeerHost { Children = { button } };
		await UITestHelper.Load(root);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		Assert.IsTrue(nodes.Any(node => ReferenceEquals(node.Owner, button)));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Children_Are_Returned_In_Order_Then_Order_Is_Preserved()
	{
		var root = new TestPeer("root", isControlElement: true);
		var first = new TestPeer("first", isControlElement: true);
		var second = new TestPeer("second", isControlElement: true);
		root.Children.Add(first);
		root.Children.Add(second);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		CollectionAssert.AreEqual(
			new[] { "root", "first", "second" },
			new[] { nodes[0].Peer.GetName(), nodes[1].Peer.GetName(), nodes[2].Peer.GetName() });
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_EventsSource_Is_Set_Then_Provider_Peer_Is_Resolved()
	{
		var root = new TestPeer("root", isControlElement: true);
		var source = new TestPeer("source", isControlElement: true);
		var eventsSource = new TestPeer("events-source", isControlElement: true);
		source.SetAPEventsSource(eventsSource);
		root.Children.Add(source);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		Assert.AreSame(source, nodes[1].Peer);
		Assert.AreSame(eventsSource, nodes[1].ProviderPeer);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Default_Action_Is_Requested_Then_EventsSource_Is_Invoked()
	{
		var source = new TestPeer("source", isControlElement: true);
		var eventsSource = new InvokableTestPeer("events-source");
		source.SetAPEventsSource(eventsSource);

		var invoked = AccessibilityPeerHelper.TryInvokeDefaultAction(source);

		Assert.IsTrue(invoked);
		Assert.IsTrue(eventsSource.WasInvoked);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Peer_Graph_Has_Cycle_Then_Each_Peer_Is_Emitted_Once()
	{
		var root = new TestPeer("root", isControlElement: true);
		var child = new TestPeer("child", isControlElement: true);
		root.Children.Add(child);
		child.Children.Add(root);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		Assert.AreEqual(2, nodes.Count);
		Assert.AreSame(root, nodes[0].Peer);
		Assert.AreSame(child, nodes[1].Peer);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Item_Peer_Has_No_Container_Then_Owner_Remains_Null()
	{
		var itemsControl = new ListView();
		var parent = new TestItemsControlAutomationPeer(itemsControl);
		var item = new TestItemAutomationPeer(new object(), parent);
		parent.Children.Add(item);

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(parent);

		Assert.AreEqual(2, nodes.Count);
		Assert.AreSame(item, nodes[1].Peer);
		Assert.IsNull(nodes[1].Owner);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Unavailable_Peer_Throws_Then_Siblings_Remain()
	{
		var root = new TestPeer("root", isControlElement: true);
		var unavailable = new UnavailableTestPeer();
		var exception = Assert.ThrowsExactly<AutomationPeerUnavailableException>(
			() => unavailable.IsControlElement());
		Assert.AreEqual(unchecked((int)0x80040201), exception.HResult);

		root.Children.Add(new TestPeer("before", isControlElement: true));
		root.Children.Add(unavailable);
		root.Children.Add(new TestPeer("after", isControlElement: true));

		var nodes = MobileAccessibilityTestHelper.GetPeerTree(root);

		CollectionAssert.AreEqual(
			new[] { "root", "before", "after" },
			nodes.Select(node => node.Peer.GetName()).ToArray());
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_MenuFlyout_Submenu_Is_Open_Then_XamlRoot_Tree_Contains_Both_Popup_Levels()
	{
		var leaf = new MenuFlyoutItem { Text = "Leaf" };
		var subItem = new MenuFlyoutSubItem
		{
			Text = "Submenu",
			Items = { leaf },
		};
		AutomationProperties.SetAutomationId(subItem, "submenu");
		AutomationProperties.SetAutomationId(leaf, "leaf");
		var flyout = new MenuFlyout
		{
			Items = { subItem },
		};
		var button = new Button
		{
			Content = "Open",
			Flyout = flyout,
		};

		try
		{
			await UITestHelper.Load(button);
			flyout.ShowAt(button);
			await TestServices.WindowHelper.WaitForLoaded(subItem);
			subItem.Open();
			await TestServices.WindowHelper.WaitForLoaded(leaf);
			await TestServices.WindowHelper.WaitForIdle();

			var nodes = MobileAccessibilityTestHelper.GetPeerTree(
				button.XamlRoot!.VisualTree.RootElement);
			Assert.IsTrue(nodes.Any(node => node.Peer.GetAutomationId() == "submenu"));
			Assert.IsTrue(nodes.Any(node => node.Peer.GetAutomationId() == "leaf"));
		}
		finally
		{
			subItem.Close();
			flyout.Hide();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Peer_Throws_Unrelated_InvalidOperation_Then_It_Propagates()
	{
		var root = new TestPeer("root", isControlElement: true);
		root.Children.Add(new FaultingTestPeer());

		var exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => MobileAccessibilityTestHelper.GetPeerTree(root));

		StringAssert.Contains(exception.Message, "Unexpected peer failure");
	}

	private class TestPeer : AutomationPeer
	{
		private readonly string _name;
		private readonly bool _isControlElement;
		private readonly bool _isContentElement;

		public TestPeer(
			string name,
			bool isControlElement = false,
			bool isContentElement = false)
		{
			_name = name;
			_isControlElement = isControlElement;
			_isContentElement = isContentElement;
		}

		public List<AutomationPeer> Children { get; } = new();

		protected override IList<AutomationPeer> GetChildrenCore() => Children;

		protected override string GetNameCore() => _name;

		protected override bool IsControlElementCore() => _isControlElement;

		protected override bool IsContentElementCore() => _isContentElement;
	}

	private sealed class UnavailableTestPeer : TestPeer
	{
		public UnavailableTestPeer()
			: base("unavailable")
		{
		}

		protected override bool IsControlElementCore()
		{
			ThrowElementNotAvailableError();
			return false;
		}
	}

	private sealed class FaultingTestPeer : TestPeer
	{
		public FaultingTestPeer()
			: base("faulting")
		{
		}

		protected override bool IsControlElementCore()
			=> throw new InvalidOperationException("Unexpected peer failure");
	}

	private sealed class InvokableTestPeer : TestPeer, IInvokeProvider
	{
		public InvokableTestPeer(string name)
			: base(name, isControlElement: true)
		{
		}

		public bool WasInvoked { get; private set; }

		public void Invoke() => WasInvoked = true;
	}

	private sealed class TestItemAutomationPeer : ItemAutomationPeer
	{
		public TestItemAutomationPeer(object item, ItemsControlAutomationPeer parent)
			: base(item, parent)
		{
		}

		protected override bool IsControlElementCore() => true;
	}

	private sealed class BoundsItemAutomationPeer : ItemAutomationPeer
	{
		internal BoundsItemAutomationPeer(object item, ItemsControlAutomationPeer parent)
			: base(item, parent)
		{
		}

		protected override Windows.Foundation.Rect GetBoundingRectangleCore() => new(3, 7, 29, 41);
	}

	private sealed class TestItemsControlAutomationPeer : ItemsControlAutomationPeer
	{
		public TestItemsControlAutomationPeer(ItemsControl owner)
			: base(owner)
		{
		}

		public List<AutomationPeer> Children { get; } = new();

		protected override IList<AutomationPeer> GetChildrenCore() => Children;
	}

	private sealed partial class TransparentPeerHost : Grid
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new TransparentFrameworkElementAutomationPeer(this);
	}

	private sealed class TransparentFrameworkElementAutomationPeer : FrameworkElementAutomationPeer
	{
		public TransparentFrameworkElementAutomationPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		protected override IList<AutomationPeer>? GetChildrenCore() => null;

		protected override bool IsControlElementCore() => false;

		protected override bool IsContentElementCore() => false;
	}
}
