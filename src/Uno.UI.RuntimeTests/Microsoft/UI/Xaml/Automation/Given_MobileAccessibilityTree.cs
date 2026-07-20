#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public partial class Given_MobileAccessibilityTree
{
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
