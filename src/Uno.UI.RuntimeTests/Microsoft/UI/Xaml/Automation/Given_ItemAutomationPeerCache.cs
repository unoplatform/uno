#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_ItemAutomationPeerCache
{
	[TestMethod]
	[DataRow("remove", false)]
	[DataRow("replace", false)]
	[DataRow("reset", false)]
	[DataRow("source", false)]
	[DataRow("remove", true)]
	[DataRow("replace", true)]
	[DataRow("reset", true)]
	[DataRow("source", true)]
	public async Task When_Selected_Item_Leaves_A_Live_Selector_Then_Its_Graph_Is_Collected(string change, bool ownContainer)
	{
#if HAS_UNO
		var selector = new ComboBox { Width = 240, Height = 40 };
		var references = PopulateSelector(selector, ownContainer);
		var previousListener = AutomationPeer.TestAutomationPeerListener;
		var listener = new SelectionListener(AutomationPeer.AutomationPeerListener);
		try
		{
			await UITestHelper.Load(selector);
			selector.IsDropDownOpen = true;
			await UITestHelper.WaitFor(() => selector.ContainerFromIndex(0) is ComboBoxItem, timeoutMS: 5000);
			selector.SelectedIndex = -1;
			AutomationPeer.TestAutomationPeerListener = listener;
			selector.SelectedIndex = 0;
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(listener.SelectedEvents > 0, "The automatic owner/data-peer notification route must run.");
			Assert.IsNotNull(FrameworkElementAutomationPeer.FromElement(selector));
			if (ownContainer)
			{
				selector.IsDropDownOpen = false;
				await UITestHelper.WaitForIdle();
			}

			// Generated containers stay materialized until removal; a closed popup's bounded
			// recycle pool intentionally keeps their data until reuse. Own containers are never pooled.
			MutateSelector(selector, change, ownContainer);
			await UITestHelper.WaitForIdle();
			selector.IsDropDownOpen = false;
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(-1, selector.SelectedIndex, "The removed item must not remain selected.");
			await AssertCollected(references.Item, references.Payload);

			Assert.AreSame(selector, TestServices.WindowHelper.WindowContent);
			Assert.IsTrue(selector.IsLoaded, "The selector must remain live throughout the collection check.");
			GC.KeepAlive(selector);
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = previousListener;
			selector.IsDropDownOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[DataRow("remove")]
	[DataRow("replace")]
	[DataRow("reset")]
	[DataRow("source")]
	public void When_Items_Change_Then_Only_Absent_Reference_Keys_Are_Evicted(string change)
	{
#if HAS_UNO
		var removed = new EqualItem();
		var retained = new EqualItem();
		var items = new ResettableCollection { removed, retained };
		var owner = new ListBox { ItemsSource = items };
		var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
		var removedPeer = peer.CreateItemAutomationPeer(removed);
		var retainedPeer = peer.CreateItemAutomationPeer(retained);
		Assert.AreNotSame(removedPeer, retainedPeer, "Equal but distinct objects must have distinct reference-keyed peers.");

		switch (change)
		{
			case "remove":
				items.RemoveAt(0);
				break;
			case "replace":
				items[0] = new object();
				break;
			case "reset":
				items.ResetWith(retained);
				break;
			case "source":
				owner.ItemsSource = new ObservableCollection<object> { retained };
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(change));
		}

		Assert.AreSame(retainedPeer, peer.CreateItemAutomationPeer(retained), "A retained item's peer identity must survive the mutation.");
		Assert.AreNotSame(removedPeer, peer.CreateItemAutomationPeer(removed), "A removed item's strong cache entry must be evicted.");
#endif
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_One_Duplicate_Occurrence_Leaves_Then_Its_Peer_Is_Retained(bool replace)
	{
#if HAS_UNO
		var duplicate = new object();
		var retained = new object();
		var items = new ObservableCollection<object> { duplicate, duplicate, retained };
		var owner = new ListBox { ItemsSource = items };
		var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
		var duplicatePeer = peer.CreateItemAutomationPeer(duplicate);
		var retainedPeer = peer.CreateItemAutomationPeer(retained);

		if (replace)
		{
			items[0] = new object();
		}
		else
		{
			items.RemoveAt(0);
		}
		Assert.AreSame(duplicatePeer, peer.CreateItemAutomationPeer(duplicate));
		Assert.AreSame(retainedPeer, peer.CreateItemAutomationPeer(retained));

		items.RemoveAt(replace ? 1 : 0);
		Assert.AreNotSame(duplicatePeer, peer.CreateItemAutomationPeer(duplicate));
		Assert.AreSame(retainedPeer, peer.CreateItemAutomationPeer(retained));
#endif
	}

	[TestMethod]
	public void When_Direct_Item_Is_Replaced_Then_Notification_And_Cache_Are_Updated()
	{
#if HAS_UNO
		var removed = new object();
		var retained = new object();
		var owner = new ListBox { Items = { removed, retained } };
		var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
		var removedPeer = peer.CreateItemAutomationPeer(removed);
		var retainedPeer = peer.CreateItemAutomationPeer(retained);
		var changes = 0;
		owner.Items.VectorChanged += (_, args) =>
		{
			Assert.AreEqual(Windows.Foundation.Collections.CollectionChange.ItemChanged, args.CollectionChange);
			Assert.AreEqual(0u, args.Index);
			changes++;
		};

		owner.Items[0] = new object();

		Assert.AreEqual(1, changes);
		Assert.AreSame(retainedPeer, peer.CreateItemAutomationPeer(retained));
		Assert.AreNotSame(removedPeer, peer.CreateItemAutomationPeer(removed));
#endif
	}

	[TestMethod]
	public void When_Items_Are_Added_Or_Moved_Then_Cached_Peers_Keep_Their_Identity()
	{
#if HAS_UNO
		var item = new object();
		var items = new ObservableCollection<object> { item };
		var owner = new ListBox { ItemsSource = items };
		var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
		var itemPeer = peer.CreateItemAutomationPeer(item);

		items.Add(new object());
		items.Move(0, 1);

		Assert.AreSame(itemPeer, peer.CreateItemAutomationPeer(item));
#endif
	}

	[TestMethod]
	public async Task When_Secondary_Peer_Storage_Loses_An_Item_Then_The_Item_Is_Collected()
	{
#if HAS_UNO
		var owner = new ListBox();
		var reference = SeedSecondaryStorage(owner);
		owner.Items.RemoveAt(0);

		await AssertCollected(reference);
		GC.KeepAlive(owner);
#endif
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_Container_Is_Recycled_Then_Only_Its_Owner_Item_Link_Is_Released(bool unrelatedSource)
	{
#if HAS_UNO
		var item = new object();
		var owner = new ListBox { Items = { item } };
		var ownerPeer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
		var itemPeer = ownerPeer.CreateItemAutomationPeer(item);
		var container = new ListBoxItem { Content = item };
		var containerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(container);
		var source = unrelatedSource
			? FrameworkElementAutomationPeer.CreatePeerForElement(new Button())
			: itemPeer;
		containerPeer.EventsSource = source;

		owner.CleanUpContainer(container);

		if (unrelatedSource)
		{
			Assert.AreSame(source, containerPeer.EventsSource);
		}
		else
		{
			Assert.IsNull(containerPeer.EventsSource);
		}
		Assert.AreSame(itemPeer, ownerPeer.CreateItemAutomationPeer(item), "Recycling is not removal from the item collection.");
#endif
	}

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public async Task When_Grouped_Items_Change_Then_Survivor_And_Duplicate_Peers_Are_Retained(bool replace, bool changeGroup)
	{
#if HAS_UNO
		var removed = new object();
		var retained = new object();
		var shared = new object();
		var first = new ObservableCollection<object> { removed, shared };
		var second = new ObservableCollection<object> { retained, shared };
		var groups = new ObservableCollection<ObservableCollection<object>> { first, second };
		var source = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
		var owner = new ListView { ItemsSource = source.View, Width = 320, Height = 300 };
		try
		{
			await UITestHelper.Load(owner);
			var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
			var removedPeer = peer.CreateItemAutomationPeer(removed);
			var retainedPeer = peer.CreateItemAutomationPeer(retained);
			var sharedPeer = peer.CreateItemAutomationPeer(shared);

			if (changeGroup)
			{
				if (replace)
				{
					groups[0] = new ObservableCollection<object> { new object() };
				}
				else
				{
					groups.RemoveAt(0);
				}
			}
			else if (replace)
			{
				first[0] = new object();
			}
			else
			{
				first.RemoveAt(0);
			}
			await UITestHelper.WaitForIdle();

			Assert.AreSame(retainedPeer, peer.CreateItemAutomationPeer(retained));
			Assert.AreSame(sharedPeer, peer.CreateItemAutomationPeer(shared), "An occurrence in the other group still owns this peer.");
			Assert.AreNotSame(removedPeer, peer.CreateItemAutomationPeer(removed));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

#if HAS_UNO
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference Item, WeakReference Payload) PopulateSelector(ComboBox selector, bool ownContainer)
	{
		var graph = new ItemGraph();
		object removed = ownContainer ? new ComboBoxItem { Content = graph } : graph;
		selector.ItemsSource = new ObservableCollection<object> { removed, CreateItem(ownContainer) };
		return (new WeakReference(removed), new WeakReference(graph.Payload));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void MutateSelector(ComboBox selector, string change, bool ownContainer)
	{
		var items = (ObservableCollection<object>)selector.ItemsSource;
		switch (change)
		{
			case "remove":
				items.RemoveAt(0);
				break;
			case "replace":
				items[0] = CreateItem(ownContainer);
				break;
			case "reset":
				items.Clear();
				break;
			case "source":
				selector.ItemsSource = new ObservableCollection<object> { items[1] };
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(change));
		}
	}

	private static object CreateItem(bool ownContainer)
		=> ownContainer ? new ComboBoxItem { Content = new ItemGraph() } : new ItemGraph();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference SeedSecondaryStorage(ListBox owner)
	{
		var item = new ItemGraph();
		owner.Items.Add(item);
		var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(owner);
		var itemPeer = peer.CreateItemAutomationPeerImpl(item);
		peer.AddItemAutomationPeerToItemPeerStorage(itemPeer);
		return new WeakReference(item);
	}

	private static async Task AssertCollected(params WeakReference[] references)
	{
		var elapsed = Stopwatch.StartNew();
		while (elapsed.Elapsed < TimeSpan.FromSeconds(10))
		{
			await UITestHelper.WaitForIdle();
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);
			if (Array.TrueForAll(references, reference => !reference.IsAlive))
			{
				return;
			}
			await Task.Delay(50);
		}
		Assert.IsTrue(Array.TrueForAll(references, reference => !reference.IsAlive),
			"Removed item graphs must be collectable while their selector and owner automation peer are still alive.");
	}

	private sealed class ItemGraph
	{
		public object Payload { get; } = new byte[4096];
		public override string ToString() => "Item";
	}

	private sealed class EqualItem
	{
		public override bool Equals(object? other) => other is EqualItem;
		public override int GetHashCode() => 0;
	}

	private sealed class ResettableCollection : ObservableCollection<object>
	{
		internal void ResetWith(params object[] items)
		{
			Items.Clear();
			foreach (var item in items)
			{
				Items.Add(item);
			}
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}

	private sealed class SelectionListener(IAutomationPeerListener? inner) : IAutomationPeerListener
	{
		internal int SelectedEvents { get; private set; }
		public bool ListenerExistsHelper(AutomationEvents eventId)
			=> eventId == AutomationEvents.PropertyChanged || inner?.ListenerExistsHelper(eventId) == true;
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty property, object oldValue, object newValue)
		{
			if (property == SelectionItemPatternIdentifiers.IsSelectedProperty && newValue is true)
			{
				SelectedEvents++;
			}
			inner?.NotifyPropertyChangedEvent(peer, property, oldValue, newValue);
		}
		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => inner?.OnAutomationEvent(peer, eventId);
		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => inner?.NotifyAutomationEvent(peer, eventId);
		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType type, AutomationPeer? child)
			=> inner?.NotifyStructureChangedEvent(peer, type, child);
		public void NotifyInvalidatePeer(AutomationPeer peer) => inner?.NotifyInvalidatePeer(peer);
		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind kind, AutomationNotificationProcessing processing, string displayString, string activityId)
			=> inner?.NotifyNotificationEvent(peer, kind, processing, displayString, activityId);
		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType type, IReadOnlyList<string> changedData)
			=> inner?.NotifyTextEditTextChangedEvent(peer, type, changedData);
	}
#endif
}
