// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_AutomationPeer_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

// TODO Uno: Uno's internal CalendarView RowOrColumnMajor in Microsoft.UI.Xaml.Controls is visible here through InternalsVisibleTo.
using RowOrColumnMajor = Microsoft.UI.Xaml.Automation.RowOrColumnMajor;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewAutomationTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 12 of the TableView API test plan: accessibility and automation.
//
// Expectations here come from TableView.idl:617-690, which declares the exact provider interface
// list for each of the five peers, and from TableView-dev-spec.md:329, which states that the UIA
// grid model is on a VISIBLE-column basis - a deliberate divergence from WPF's
// DataGridAutomationPeer. Where neither says anything, the test's comment says so.
//
// Two structural facts shape every test in this file:
//
//   * The column header peer is virtual. Its Owner() is the TableView, not a header element, and
//     it is manufactured per column by TableViewAutomationPeer::GetOrCreateColumnHeaderPeer. The
//     table-level route (IGridProvider.GetColumnHeaders) returns IRawElementProviderSimple and
//     there is no public provider->peer conversion, so tests that assert on header naming, help
//     text or set position construct the peer directly. TableView.idl:670 makes that constructor
//     public for exactly this reason.
//
//   * Providers are opaque to a C# test. Anything typed IRawElementProviderSimple can only be
//     asserted for null / non-null / count / reference identity, unless a test opens it through
//     a peer subclass's protected PeerFromProvider (the GetSelection tests). No test in this repo
//     prises one open with raw UIA pattern ids, and these do not start.
//
// NOT COVERED HERE, deliberately:
//   * The Value pattern on cells - Category 10 owns the advertisement gate in full (four tests).
//   * Tooltip -> HelpText mapping and its duplicate suppression - Category 14. This file keeps
//     only the sort-state half of header help text, which has no tooltip in play.
//   * "Invoke sorts the column" - Category 7.5 owns the positive path; this file owns the
//     withholding. Rule of thumb: what happens to the DATA is Category 7, what the PEER
//     ADVERTISES is Category 12.
//   * Automation events (structure-changed, ExpandCollapseState property-changed). The product
//     gates every raise on AutomationPeer.ListenerExists(...), which is false unless a real UIA
//     client is attached. An API test cannot attach one, so such a test would pass identically if
//     the events were deleted. See section 12.6 of the plan.
[TestClass]
public class TableViewAutomationPeerTests : TableViewApiTestBase
{
	// ---------- 12.1 TableView peer patterns and identity ----------

	[TestMethod]
	[TestProperty("Description", "Verifies the TableView peer advertises Grid, Table and ItemContainer, and answers each as itself.")]
	public async Task VerifyTableViewPeerAdvertisesGridTableAndItemContainer()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);

			// TableView.idl:617-624 declares IGridProvider, ITableProvider, ISelectionProvider and
			// IItemContainerProvider on the runtimeclass. Grid, Table and ItemContainer describe
			// the control's shape rather than its state, so they are unconditional.
			foreach (var pattern in new[] { PatternInterface.Grid, PatternInterface.Table, PatternInterface.ItemContainer })
			{
				var provider = peer.GetPattern(pattern);
				if (provider == null)
				{
					Verify.Fail($"The TableView peer must advertise the {pattern} pattern; TableView.idl:617-624 declares it.");
					continue;
				}

				// Structural patterns are implemented by the peer itself, not forwarded, so an
				// AT client that casts the provider back gets the peer it queried.
				Verify.IsTrue(ReferenceEquals(provider, peer),
					$"The {pattern} pattern should be answered by the TableView peer itself.");
			}

			Verify.IsNotNull(peer.GetPattern(PatternInterface.Grid) as IGridProvider, "Grid must cast to IGridProvider.");
			Verify.IsNotNull(peer.GetPattern(PatternInterface.Table) as ITableProvider, "Table must cast to ITableProvider.");
			Verify.IsNotNull(peer.GetPattern(PatternInterface.ItemContainer) as IItemContainerProvider, "ItemContainer must cast to IItemContainerProvider.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Selection pattern is advertised under SelectionMode.Single and withheld under None.")]
	public async Task VerifySelectionPatternTracksSelectionMode()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);

			// TableView.idl:619 declares ISelectionProvider, so the positive half is the published
			// contract.
			Verify.IsNotNull(peer.GetPattern(PatternInterface.Selection) as ISelectionProvider,
				"Selection must be advertised while SelectionMode is Single.");

			tableView.SelectionMode = TableViewSelectionMode.None;
			tableView.UpdateLayout();

			// The conditional withholding is NOT stated in the IDL or the design spec - it is a
			// product-code rationale that matches UIA guidance (do not advertise a pattern you
			// cannot honour). Flagged in the plan as (needs spec decision): a reader of the IDL
			// alone would conclude the pattern is unconditional.
			Verify.IsNull(peer.GetPattern(PatternInterface.Selection),
				"Selection must be withheld while SelectionMode is None; advertising it tells an AT client the grid is selectable when every Select() would be refused.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the selection provider reports single-select semantics and the current selection.")]
	public async Task VerifySelectionProviderReportsSingleSelectSemantics()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateAutomationTable(items);
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var selection = peer.GetPattern(PatternInterface.Selection) as ISelectionProvider;
			if (selection == null)
			{
				Verify.Fail("Selection must be advertised while SelectionMode is Single.");
				return;
			}

			// TableView.idl:531-554 limits SelectionMode to None/Single this release, which makes
			// CanSelectMultiple == false a contract rather than an implementation detail.
			Verify.IsFalse(selection.CanSelectMultiple,
				"CanSelectMultiple must be false; true makes AT offer multi-select affordances the control refuses.");

			// DeselectAll() is public, so selection can always be empty.
			Verify.IsFalse(selection.IsSelectionRequired,
				"IsSelectionRequired must be false; DeselectAll() is public, so an empty selection is always reachable.");

			var empty = selection.GetSelection();
			Verify.AreEqual(0, empty == null ? 0 : empty.Length, "With nothing selected the selection array must be empty.");

			// Row 1 is realized at this size, so this asserts the selection report rather than the
			// virtualization behaviour; VerifyGetSelectionReportsUnrealizedSelectedRow covers that.
			tableView.Select(1);
			tableView.UpdateLayout();

			var selected = selection.GetSelection();
			Verify.AreEqual(1, selected == null ? 0 : selected.Length,
				"Exactly one provider must be reported for a realized selected row.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies GetSelection reports an off-screen selected row through a data-item peer, without realizing the row or moving the viewport.")]
	public async Task VerifyGetSelectionReportsUnrealizedSelectedRow()
	{
		TableView tableView = null;
		AutomationPeer itemPeer = null;
		string unrealizedName = null;
		var items = MakeNamedPeople(200);
		int target = -1;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => GetBodyScroller(tableView).ChangeView(null, 2000.0, null, true));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			var repeater = GetRowsRepeater(tableView);
			Verify.IsGreaterThan(scroller.VerticalOffset, 0.0, "Precondition: the body must have scrolled.");

			target = Math.Min(RealizedRowIndices(tableView).Max() + 50, items.Count - 1);
			tableView.Select(target);
			tableView.UpdateLayout();
			Verify.IsNull(repeater.TryGetElement(target), "Precondition: the selected row must not be realized.");

			var offset = scroller.VerticalOffset;
			var realized = string.Join(",", RealizedRowIndices(tableView));

			// Report the selected row without realizing it, which would re-anchor the repeater.
			var selected = RequireSelectionProvider(tableView).GetSelection();
			tableView.UpdateLayout();

			Verify.AreEqual(1, selected == null ? 0 : selected.Length,
				"GetSelection must report the selected row even when it is not realized.");
			Verify.AreEqual(offset, scroller.VerticalOffset, "GetSelection must not scroll.");
			Verify.AreEqual(realized, string.Join(",", RealizedRowIndices(tableView)),
				"GetSelection must not change which rows are realized.");
			Verify.IsNull(repeater.TryGetElement(target), "GetSelection must not realize the selected row.");

			itemPeer = new ProviderPeerProbe().ToPeer(selected[0]);
			Verify.IsNotNull(itemPeer, "The reported provider must resolve to a peer.");
			unrealizedName = itemPeer.GetName();
			Verify.IsTrue(unrealizedName.Contains(items[target].Name), $"The data-item peer must be named from its item (got '{unrealizedName}').");
			Verify.AreEqual(AutomationControlType.DataItem, itemPeer.GetAutomationControlType(), "A row is a DataItem.");
			Verify.IsTrue(itemPeer.IsOffscreen(), "An unrealized row is off screen.");
			var selectionItem = itemPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
			Verify.IsNotNull(selectionItem, "The data-item peer must offer SelectionItem.");
			Verify.IsTrue(selectionItem.IsSelected, "The data-item peer must report itself selected.");
			Verify.IsNotNull(itemPeer.GetPattern(PatternInterface.VirtualizedItem) as IVirtualizedItemProvider,
				"An unrealized row must offer VirtualizedItem.");
			Verify.AreSame(itemPeer, new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]),
				"Repeated GetSelection calls must hand back the same provider.");

			((IVirtualizedItemProvider)itemPeer.GetPattern(PatternInterface.VirtualizedItem)).Realize();
			// Realize is synchronous: a client re-queries the provider as soon as it returns.
			Verify.IsNotNull(GetRowsRepeater(tableView).TryGetElement(target), "Realize must create the selected row before it returns.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			var row = GetRowsRepeater(tableView).TryGetElement(target) as TableViewRow;
			Verify.IsNotNull(row, "Realize must create the selected row.");
			var top = row.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0, 0)).Y;
			Verify.IsTrue(top >= -0.5 && top + row.ActualHeight <= scroller.ViewportHeight + 0.5,
				$"Realize must bring the row into view (top {top}, height {row.ActualHeight}, viewport {scroller.ViewportHeight}).");
			Verify.IsNull(itemPeer.GetPattern(PatternInterface.VirtualizedItem),
				"Once realized, the data-item peer must stop offering VirtualizedItem.");

			// A client may keep the provider it was handed; it must now describe the realized row.
			var rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(row);
			var bounds = itemPeer.GetBoundingRectangle();
			Verify.IsTrue(bounds.Width > 0 && bounds.Height > 0, $"The held peer must report the row's bounds (got {bounds}).");
			Verify.IsFalse(itemPeer.IsOffscreen(), "The held peer must not report an on-screen row as off screen.");
			Verify.AreEqual(rowPeer.GetName(), itemPeer.GetName(), "The held peer must report the row's name.");
			Verify.AreEqual(unrealizedName, itemPeer.GetName(), "Realizing the row must not change the data-item peer's name.");
			Verify.AreSame(rowPeer.Navigate(AutomationNavigationDirection.Parent), itemPeer.Navigate(AutomationNavigationDirection.Parent),
				"The data-item peer must have the same parent as a realized row.");

			// Walking the held peer's children must not take the row's cell peers away from the row.
			var cells = rowPeer.GetChildren();
			Verify.IsTrue(cells != null && cells.Count > 0, "The realized row must expose its cells.");
			itemPeer.GetChildren();
			Verify.AreSame(rowPeer, cells[0].Navigate(AutomationNavigationDirection.Parent),
				"A cell's parent must still be its row after the data-item peer's children were read.");

			Verify.AreSame(rowPeer, new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]),
				"A realized selected row must be reported through its own peer.");
			scroller.ChangeView(null, 0.0, null, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(GetRowsRepeater(tableView).TryGetElement(target), "Precondition: the selected row must be unrealized again.");
			Verify.AreSame(itemPeer, new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]),
				"An item that scrolls back out must be reported through the same data-item peer as before.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the data-item peer for an unrealized selected row reports ElementNotAvailable once its item leaves the source.")]
	public async Task VerifyGetSelectionDataItemPeerForRemovedItemIsNotAvailable()
	{
		TableView tableView = null;
		var items = new global::System.Collections.ObjectModel.ObservableCollection<NamedPerson>(MakeNamedPeople(200));

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(150);
			tableView.UpdateLayout();
			Verify.IsNull(GetRowsRepeater(tableView).TryGetElement(150), "Precondition: the selected row must not be realized.");

			var itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
			var selectionItem = itemPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
			Verify.IsTrue(selectionItem.IsSelected, "Precondition: the data-item peer reports itself selected.");

			items.RemoveAt(150);
			tableView.UpdateLayout();

			VerifyElementNotAvailable(() => _ = selectionItem.IsSelected, "IsSelected");
			VerifyElementNotAvailable(() => _ = selectionItem.SelectionContainer, "SelectionContainer");
			VerifyElementNotAvailable(() => selectionItem.RemoveFromSelection(), "RemoveFromSelection");
			VerifyElementNotAvailable(() => _ = itemPeer.GetName(), "GetName");
			Verify.IsNull(itemPeer.GetPattern(PatternInterface.SelectionItem), "A removed item must stop offering SelectionItem.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the data-item peer follows the selected occurrence of an item that appears twice in the source.")]
	public async Task VerifyGetSelectionDataItemPeerTracksSelectedDuplicate()
	{
		TableView tableView = null;
		var items = MakeNamedPeople(200);
		items[180] = items[150];

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var repeater = GetRowsRepeater(tableView);
			tableView.Select(150);
			tableView.UpdateLayout();
			Verify.IsNull(repeater.TryGetElement(150), "Precondition: the selected row must not be realized.");
			RequireSelectionProvider(tableView).GetSelection();

			// The same item, now selected at its other unrealized occurrence.
			tableView.Select(180);
			tableView.UpdateLayout();
			Verify.IsNull(repeater.TryGetElement(180), "Precondition: the selected row must not be realized.");

			var itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
			Verify.IsTrue(((ISelectionItemProvider)itemPeer.GetPattern(PatternInterface.SelectionItem)).IsSelected,
				"The peer for the selected occurrence must report itself selected.");
			((IVirtualizedItemProvider)itemPeer.GetPattern(PatternInterface.VirtualizedItem)).Realize();
			Verify.IsNotNull(repeater.TryGetElement(180), "Realize must create the selected occurrence.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the data-item peer of a scrolled-out selected row from a source that boxes values keeps its name after a garbage collection.")]
	public async Task VerifyGetSelectionDataItemPeerForBoxedValuesKeepsNameAfterCollection()
	{
		TableView tableView = null;
		AutomationPeer itemPeer = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(new BoxingIntList(200), 600, 300);
			tableView.Columns.Add(MakeTextColumn("Value", "."));
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(2);
			tableView.UpdateLayout();
			GetBodyScroller(tableView).ChangeView(null, 2000.0, null, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(GetRowsRepeater(tableView).TryGetElement(2), "Precondition: the selected row must have scrolled out of realization.");
			var view = GetRowsRepeater(tableView).ItemsSourceView;
			Log.Comment($"The source hands out the same object on re-read: {ReferenceEquals(view.GetAt(2), view.GetAt(2))}.");
			itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
		});

		// The box the peer was created for is referenced by nothing else.
		for (int i = 0; i < 2; ++i)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("2", itemPeer.GetName(), "The data-item peer must keep its name after the boxed item is collected.");
			Verify.IsTrue(((ISelectionItemProvider)itemPeer.GetPattern(PatternInterface.SelectionItem)).IsSelected,
				"The data-item peer must still report itself selected.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a held data-item peer keeps its name when its row is realized, then forwards the row's enabled state, culture and ScrollItem.")]
	public async Task VerifyRealizedDataItemPeerForwardsRowState()
	{
		TableView tableView = null;
		AutomationPeer itemPeer = null;
		string unrealizedName = null;
		// No ToString override, so the item's own string is its type name.
		var items = Enumerable.Range(0, 200).Select(i => new PlainPerson { Name = $"Person {i}", Role = $"Role {i}" }).ToList();

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(150);
			tableView.UpdateLayout();
			Verify.IsNull(GetRowsRepeater(tableView).TryGetElement(150), "Precondition: the selected row must not be realized.");

			itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
			unrealizedName = itemPeer.GetName();
			Verify.IsNull(itemPeer.GetPattern(PatternInterface.ScrollItem), "An unrealized row has no element to scroll into view.");
			((IVirtualizedItemProvider)itemPeer.GetPattern(PatternInterface.VirtualizedItem)).Realize();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRowsRepeater(tableView).TryGetElement(150) as TableViewRow;
			Verify.IsNotNull(row, "Realize must create the selected row.");
			var rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(row);
			Verify.AreEqual(rowPeer.GetName(), unrealizedName, "An unrealized row must be named as it is once realized.");
			Verify.AreEqual(rowPeer.GetCulture(), itemPeer.GetCulture(), "The held peer must report the row's culture.");

			Verify.IsTrue(itemPeer.IsEnabled(), "Precondition: the row is enabled.");
			row.IsEnabled = false;
			Verify.IsFalse(itemPeer.IsEnabled(), "The held peer must report a disabled row as disabled.");
			row.IsEnabled = true;

			Verify.IsNotNull(itemPeer.GetPattern(PatternInterface.ScrollItem) as IScrollItemProvider, "A realized row must offer ScrollItem.");
			var scroller = GetBodyScroller(tableView);
			scroller.ChangeView(null, scroller.VerticalOffset + scroller.ViewportHeight + 1, null, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRowsRepeater(tableView).TryGetElement(150) as TableViewRow;
			Verify.IsNotNull(row, "Precondition: the row must still be realized.");
			Verify.IsFalse(IsInViewport(row, GetBodyScroller(tableView)), "Precondition: the row must be out of view.");
			((IScrollItemProvider)itemPeer.GetPattern(PatternInterface.ScrollItem)).ScrollIntoView();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRowsRepeater(tableView).TryGetElement(150) as TableViewRow;
			Verify.IsTrue(row != null && IsInViewport(row, GetBodyScroller(tableView)), "ScrollIntoView must bring the row into view.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a held data-item peer is parented with the realized rows after the TableView template is reapplied.")]
	public async Task VerifyDataItemPeerParentFollowsReappliedTemplate()
	{
		TableView tableView = null;
		AutomationPeer itemPeer = null;
		ItemsRepeater oldRepeater = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(MakeNamedPeople(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(150);
			tableView.UpdateLayout();
			itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
			oldRepeater = GetRowsRepeater(tableView);

			var template = tableView.Template;
			tableView.Template = null;
			tableView.UpdateLayout();
			tableView.Template = template;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var repeater = GetRowsRepeater(tableView);
			Verify.AreNotSame(oldRepeater, repeater, "Precondition: reapplying the template must replace the rows repeater.");
			var row = repeater.TryGetElement(0);
			Verify.IsNotNull(row, "Precondition: the first row must be realized.");
			Verify.AreSame(FrameworkElementAutomationPeer.CreatePeerForElement(row).Navigate(AutomationNavigationDirection.Parent),
				itemPeer.Navigate(AutomationNavigationDirection.Parent),
				"A held data-item peer must have the same parent as the current realized rows.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Realize fails, without scrolling or pinning the layout, when an ElementPrepared handler rebinds the data-item peer to another occurrence.")]
	public async Task VerifyDataItemPeerRealizeFailsWhenReboundDuringCreation()
	{
		TableView tableView = null;
		var items = MakeNamedPeople(200);
		items[180] = items[150];
		double offset = 0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var repeater = GetRowsRepeater(tableView);
			tableView.Select(150);
			tableView.UpdateLayout();
			Verify.IsNull(repeater.TryGetElement(150), "Precondition: the selected row must not be realized.");

			var itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
			var virtualized = (IVirtualizedItemProvider)itemPeer.GetPattern(PatternInterface.VirtualizedItem);
			offset = GetBodyScroller(tableView).VerticalOffset;

			bool rebound = false;
			global::Windows.Foundation.TypedEventHandler<ItemsRepeater, ItemsRepeaterElementPreparedEventArgs> handler = (sender, args) =>
			{
				if (args.Index == 150 && !rebound)
				{
					rebound = true;
					tableView.Select(180);
					RequireSelectionProvider(tableView).GetSelection();
				}
			};
			repeater.ElementPrepared += handler;
			try
			{
				VerifyElementNotAvailable(() => virtualized.Realize(), "Realize of a rebound peer");
			}
			finally
			{
				repeater.ElementPrepared -= handler;
			}
			Verify.IsTrue(rebound, "Precondition: the handler must have rebound the peer.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			Verify.AreEqual(offset, scroller.VerticalOffset, "A failed Realize must not scroll.");
			scroller.ChangeView(null, 3000.0, null, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			var repeater = GetRowsRepeater(tableView);
			Verify.IsTrue(RealizedRowIndices(tableView).Any(i => IsInViewport((FrameworkElement)repeater.TryGetElement(i), scroller)),
				"After a failed Realize, realized rows must follow the viewport.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an empty selection drops the cached data-item peer, so a later selection of the same row gets a new one.")]
	public async Task VerifyGetSelectionDropsDataItemPeerOnEmptySelection()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(MakeNamedPeople(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(150);
			tableView.UpdateLayout();
			Verify.IsNull(GetRowsRepeater(tableView).TryGetElement(150), "Precondition: the selected row must not be realized.");
			var first = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);

			tableView.Deselect(150);
			var empty = RequireSelectionProvider(tableView).GetSelection();
			Verify.AreEqual(0, empty == null ? 0 : empty.Length, "Precondition: the selection must be empty.");

			tableView.Select(150);
			Verify.AreNotSame(first, new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]),
				"An empty selection must release the cached data-item peer.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the data-item peer follows the selected occurrence when an insert moves another occurrence of its item to the index it last had.")]
	public async Task VerifyDataItemPeerPrefersSelectedOccurrenceAfterInsert()
	{
		TableView tableView = null;
		var items = new global::System.Collections.ObjectModel.ObservableCollection<NamedPerson>(MakeNamedPeople(200));
		items[149] = items[150];

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(150);
			tableView.UpdateLayout();
			Verify.IsNull(GetRowsRepeater(tableView).TryGetElement(150), "Precondition: the selected row must not be realized.");
			var itemPeer = new ProviderPeerProbe().ToPeer(RequireSelectionProvider(tableView).GetSelection()[0]);
			var selectionItem = (ISelectionItemProvider)itemPeer.GetPattern(PatternInterface.SelectionItem);

			items.Insert(0, new NamedPerson { Name = "Inserted", Role = "Inserted" });
			tableView.UpdateLayout();
			Verify.AreEqual(151, tableView.SelectedIndex, "Precondition: the selection must move with the inserted row.");

			Verify.IsTrue(selectionItem.IsSelected, "The data-item peer must follow the selected occurrence, not the one now at its old index.");
		});
	}

	private static bool IsInViewport(FrameworkElement element, ScrollViewer scroller)
	{
		var top = element.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0, 0)).Y;
		return top >= -0.5 && top + element.ActualHeight <= scroller.ViewportHeight + 0.5;
	}

	private sealed class PlainPerson
	{
		public string Name { get; set; }

		public string Role { get; set; }
	}

	// A non-generic list that hands out a new box on every read, so its items have no stable identity.
	private sealed class BoxingIntList : global::System.Collections.IList
	{
		private readonly int[] _values;

		public BoxingIntList(int count) { _values = Enumerable.Range(0, count).ToArray(); }

		public object this[int index] { get => (object)_values[index]; set => throw new global::System.NotSupportedException(); }
		public bool IsFixedSize => true;
		public bool IsReadOnly => true;
		public int Count => _values.Length;
		public bool IsSynchronized => false;
		public object SyncRoot => this;
		public int Add(object value) => throw new global::System.NotSupportedException();
		public void Clear() => throw new global::System.NotSupportedException();
		public bool Contains(object value) => IndexOf(value) >= 0;
		public int IndexOf(object value) => value is int i ? global::System.Array.IndexOf(_values, i) : -1;
		public void Insert(int index, object value) => throw new global::System.NotSupportedException();
		public void Remove(object value) => throw new global::System.NotSupportedException();
		public void RemoveAt(int index) => throw new global::System.NotSupportedException();
		public void CopyTo(global::System.Array array, int index) => _values.CopyTo(array, index);
		public global::System.Collections.IEnumerator GetEnumerator() => _values.GetEnumerator();
	}

	private static void VerifyElementNotAvailable(Action action, string what)
	{
		int hr = 0;
		try
		{
			action();
		}
		catch (global::System.Exception e)
		{
			hr = e.HResult;
		}
		Verify.AreEqual(unchecked((int)0x80040201), hr, $"{what} must fail with UIA_E_ELEMENTNOTAVAILABLE for a removed item.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies GetSelection reports a realized selected row through that row's own peer.")]
	public async Task VerifyGetSelectionReturnsRealizedRowPeer()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateNamedPeopleTable(MakeNamedPeople(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => GetBodyScroller(tableView).ChangeView(null, 2000.0, null, true));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var index = RealizedRowIndices(tableView).First();
			tableView.Select(index);
			tableView.UpdateLayout();

			var row = GetRowsRepeater(tableView).TryGetElement(index);
			Verify.IsNotNull(row, "Precondition: the selected row must be realized.");

			var selected = RequireSelectionProvider(tableView).GetSelection();
			Verify.AreEqual(1, selected == null ? 0 : selected.Length, "GetSelection must report the selected row.");
			Verify.AreSame(FrameworkElementAutomationPeer.CreatePeerForElement(row), new ProviderPeerProbe().ToPeer(selected[0]),
				"A realized selected row must be reported through its own row peer, as Grid.GetItem and tree navigation do.");
		});
	}

	// AutomationPeer.PeerFromProvider is protected; a peer subclass is the only way to open a provider here.
	private sealed class ProviderPeerProbe : FrameworkElementAutomationPeer
	{
		public ProviderPeerProbe() : base(new Border()) { }

		public AutomationPeer ToPeer(IRawElementProviderSimple provider) => PeerFromProvider(provider);
	}

	private sealed class NamedPerson
	{
		public string Name { get; set; }

		public string Role { get; set; }

		public override string ToString() => Name;
	}

	private static List<NamedPerson> MakeNamedPeople(int count) => Enumerable
		.Range(0, count)
		.Select(i => new NamedPerson { Name = $"Person {i}", Role = $"Role {i}" })
		.ToList();

	private static TableView CreateNamedPeopleTable(object items)
	{
		var tableView = CreateTableViewShell(items, 600, 300);
		tableView.Columns.Add(MakeTextColumn("Name"));
		tableView.Columns.Add(MakeTextColumn("Role"));
		tableView.SelectionMode = TableViewSelectionMode.Single;
		return tableView;
	}

	private static ISelectionProvider RequireSelectionProvider(TableView tableView)
	{
		var selection = RequireTablePeer(tableView).GetPattern(PatternInterface.Selection) as ISelectionProvider;
		Verify.IsNotNull(selection, "Selection must be advertised while SelectionMode is Single.");
		return selection;
	}

	private static List<int> RealizedRowIndices(TableView tableView)
	{
		var repeater = GetRowsRepeater(tableView);
		return GetRealizedRows(tableView)
			.Select(row => repeater.GetElementIndex(row))
			.Where(index => index >= 0)
			.OrderBy(index => index)
			.ToList();
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Scroll pattern is answered by the body scroller's peer and never by the TableView peer itself.")]
	public async Task VerifyScrollPatternIsForwardedToBodyScrollerNotThePeer()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable(TableViewRowTestHelpers.MakeManyItems(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var provider = peer.GetPattern(PatternInterface.Scroll);

			// Narrator scrolls a grid through this pattern; without it every row below the fold is
			// unreachable by AT.
			if (provider == null)
			{
				Verify.Fail("The TableView peer must answer the Scroll pattern so AT can scroll to unrealized rows.");
				return;
			}

			// The negative half is IDL-backed: TableView.idl:617-624 lists four provider interfaces
			// and IScrollProvider is NOT among them, so the peer must not answer as itself. A peer
			// claiming a pattern it does not implement fails at the cast in every client.
			Verify.IsFalse(ReferenceEquals(provider, peer),
				"Scroll must be forwarded to the body scroller's peer; TableView.idl does not declare IScrollProvider on the TableView peer.");
			Verify.IsNotNull(provider as IScrollProvider,
				"The forwarded Scroll provider must cast to IScrollProvider.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the control types of the table, row and cell peers.")]
	public async Task VerifyTableViewPeerControlTypeIsDataGrid()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateAutomationTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var tablePeer = RequireTablePeer(tableView);
			Verify.AreEqual(AutomationControlType.DataGrid, tablePeer.GetAutomationControlType(),
				"The TableView peer must report DataGrid; anything else loses table reading mode in Narrator.");

			var rowPeer = RequireRowPeer(tableView, 0);
			// TableView-dev-spec.md:170 states the row peer is a DataItem outright.
			Verify.AreEqual(AutomationControlType.DataItem, rowPeer.GetAutomationControlType(),
				"The row peer must report DataItem.");

			var cellPeer = GetCellPeer(rowPeer, 0);
			Verify.AreEqual(AutomationControlType.DataItem, cellPeer.GetAutomationControlType(),
				"The cell peer must report DataItem so Narrator reads the composed cell name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies all five TableView peers report non-empty, pairwise distinct class names aligned to their owner type.")]
	public async Task VerifyPeerClassNamesAreDistinctAndOwnerAligned()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var tablePeer = RequireTablePeer(tableView);
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 0);
			var headerPeer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);
			var groupHeaderPeer = GetGroupHeaderPeer(tableView, 0);

			var names = new Dictionary<string, string>
			{
				{ "TableView", tablePeer.GetClassName() },
				{ "TableViewRow", rowPeer.GetClassName() },
				{ "TableViewCell", cellPeer.GetClassName() },
				{ "TableViewColumnHeader", headerPeer.GetClassName() },
				{ "TableViewGroupHeader", groupHeaderPeer.GetClassName() },
			};

			foreach (var entry in names)
			{
				Log.Comment($"{entry.Key} peer class name: '{entry.Value}'");
				Verify.IsFalse(string.IsNullOrEmpty(entry.Value), $"The {entry.Key} peer must report a class name.");

				// Deliberately asserts the SUFFIX, not the exact qualification. The five peers are
				// inconsistent today: TableView and TableViewRow use hstring_name_of<T>() (the
				// repo-wide convention), while the cell, column-header and group-header peers
				// return hardcoded short literals. Pinning one exact form would freeze half of
				// them into the wrong convention, so the plan carries this as
				// (needs spec decision): pick one, then tighten this to equality.
				Verify.IsTrue(entry.Value != null && entry.Value.EndsWith(entry.Key, StringComparison.Ordinal),
					$"The {entry.Key} peer's class name should end in its owner's type name; saw '{entry.Value}'.");
			}

			// Two peers sharing a class name makes them indistinguishable to AT scripts and to
			// UIA-driven tooling, which commonly select by class name.
			Verify.AreEqual(names.Count, names.Values.Distinct().Count(),
				"All five peer class names must be pairwise distinct.");
		});
	}

	// ---------- 12.2 Grid coordinates ----------

	[TestMethod]
	[TestProperty("Description", "Verifies RowCount spans the whole item source and ColumnCount counts visible columns.")]
	public async Task VerifyGridRowAndColumnCountsMatchVisibleGrid()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable(TableViewRowTestHelpers.MakeManyItems(10));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// Not stated in the spec: reasoned from the UIA grid contract, which is defined over
			// the logical grid rather than the realized window. If RowCount reported only realized
			// rows, Narrator would announce "row 3 of 7" on a 200-item grid and stop navigating at
			// the realization boundary.
			Verify.AreEqual(10, grid.RowCount, "RowCount must span the whole item source, not just realized rows.");
			Verify.AreEqual(3, grid.ColumnCount, "ColumnCount must report the visible column count.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IGridProvider.GetItem returns a provider for a realized cell.")]
	public async Task VerifyGridGetItemReturnsProviderForRealizedCell()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// The most basic grid navigation there is: "move to column 2" on a row that is on screen.
			Verify.IsNotNull(grid.GetItem(0, 1), "GetItem must return a provider for a realized cell.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IGridProvider.GetItem realizes a row far outside the realization window.")]
	public async Task VerifyGridGetItemRealizesOffscreenRow()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable(TableViewRowTestHelpers.MakeManyItems(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// Spec silent. Reasoned from the UIA grid contract, which has no notion of realization:
			// the alternative (returning null) would need VirtualizedItemPattern to be honest, and
			// the control does not implement it. The realization is deliberately bounded to the one
			// requested row, so this asserts reachability, never "the whole source realized".
			Verify.IsNotNull(grid.GetItem(150, 0),
				"GetItem must realize the requested row on demand; otherwise AT can only reach rows the user already scrolled to.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IGridProvider.GetItem returns null for out-of-range coordinates without crashing.")]
	public async Task VerifyGridGetItemOutOfRangeReturnsNull()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);
			int rowCount = grid.RowCount;
			int columnCount = grid.ColumnCount;

			// UIA permits either null or a failure HRESULT here. This pins null, which is the safer
			// of the two and the one a C# caller can handle; the plan carries it as
			// (needs spec decision). AT clients do probe grid bounds, so the real assertion is that
			// probing neither crashes the app nor corrupts the realization window.
			Verify.IsNull(grid.GetItem(-1, 0), "A negative row index must return null.");
			Verify.IsNull(grid.GetItem(0, -1), "A negative column index must return null.");
			Verify.IsNull(grid.GetItem(rowCount, 0), "A row index at RowCount must return null.");
			Verify.IsNull(grid.GetItem(0, columnCount), "A column index at ColumnCount must return null.");

			Verify.IsNotNull(grid.GetItem(0, 0), "The grid must still be usable after out-of-range probes.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a collapsed column is absent from ColumnCount and from the row peer's children.")]
	public async Task VerifyHiddenColumnsAreSkippedInGridCoordinates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[1].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// TableView-dev-spec.md:329 states this outright, including that it intentionally
			// diverges from WPF's DataGridAutomationPeer, which indexes the full collection.
			Verify.AreEqual(2, grid.ColumnCount, "A collapsed column must not be counted in the UIA grid.");

			var rowPeer = RequireRowPeer(tableView, 0);
			var children = rowPeer.GetChildren();
			Verify.AreEqual(2, children == null ? 0 : children.Count,
				"The row peer must expose one cell per VISIBLE column; a zero-width invisible cell would make Narrator stop on nothing.");

			// The deliberate asymmetry worth knowing: the visual layer still generates a hidden cell
			// per collapsed column, so the cell-host child count and the UIA child count legitimately
			// differ. Do not "fix" a failure here by counting panel children.
			var cellsHost = GetCellsHost(GetRealizedRows(tableView)[0]);
			Verify.AreEqual(3, cellsHost.Children.Count,
				"The visual layer keeps a cell per column; only the UIA projection filters to visible columns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing a column shifts the GridItem.Column of the columns after it.")]
	public async Task VerifyHiddenColumnShiftsSubsequentColumnIndices()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[0].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var children = rowPeer.GetChildren();
			if (children == null || children.Count != 2)
			{
				Verify.Fail($"Expected 2 visible cells after collapsing the first column; saw {children?.Count ?? 0}.");
				return;
			}

			// Same spec sentence as the count test (TableView-dev-spec.md:329), different
			// assertion: that test proves the SIZE of the model, this proves the MAPPING. A
			// regression that renumbers without recounting breaks exactly one of them, which is why
			// both are kept.
			var first = children[0].GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			var second = children[1].GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (first == null || second == null)
			{
				Verify.Fail("Both remaining cells must advertise GridItem.");
				return;
			}

			Verify.AreEqual(0, first.Column, "The first visible column must report index 0 once the column before it is collapsed.");
			Verify.AreEqual(1, second.Column, "Indices to the right must shift down by one, so they stay inside 0..ColumnCount-1.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the table provider reports RowMajor and no row headers.")]
	public async Task VerifyTableProviderReportsRowMajorAndNoRowHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var table = peer.GetPattern(PatternInterface.Table) as ITableProvider;
			if (table == null)
			{
				Verify.Fail("The TableView peer must advertise the Table pattern.");
				return;
			}

			// Spec silent on RowOrColumnMajor; reasoned from the control being row-oriented
			// throughout (TableView-dev-spec.md:165 describes keyboard handling as row-oriented).
			// ColumnMajor would make Narrator read down columns instead of across rows.
			Verify.AreEqual(RowOrColumnMajor.RowMajor, table.RowOrColumnMajor, "A row-oriented grid must report RowMajor.");

			// TableView has no row-header element at all, so a non-empty array would hold providers
			// for elements that do not exist.
			var rowHeaders = table.GetRowHeaders();
			Verify.AreEqual(0, rowHeaders == null ? 0 : rowHeaders.Length, "TableView has no row headers.");
		});
	}

	// ---------- 12.3 Cell and row peers ----------

	[TestMethod]
	[TestProperty("Description", "Verifies a row peer exposes one cell peer per visible column, in visible-column order.")]
	public async Task VerifyRowPeerExposesOneCellPerVisibleColumn()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[1].Visibility = Visibility.Collapsed;
			// Alias over Role, so its cells read differently from the Name column's.
			tableView.Columns.RemoveAt(2);
			tableView.Columns.Add(MakeTextColumn("Alias", "Role"));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var children = rowPeer.GetChildren();

			// TableView-dev-spec.md:170 states the row peer exposes its cell peers as children.
			// Without them the row is an opaque leaf: AT reaches the row but not the values in it.
			if (children == null || children.Count != 2)
			{
				Verify.Fail($"The row peer must expose one cell peer per visible column; saw {children?.Count ?? 0}.");
				return;
			}

			// Assert the cast rather than using `as` and skipping - a null child must fail loudly.
			for (int i = 0; i < children.Count; i++)
			{
				Verify.IsTrue(children[i] is TableViewCellAutomationPeer,
					$"Row peer child {i} must be a TableViewCellAutomationPeer.");
			}

			// Visible-column order: the collapsed column is the middle one, so the two survivors are
			// the first and third columns' cells.
			for (int i = 0; i < children.Count; i++)
			{
				var gridItem = children[i].GetPattern(PatternInterface.GridItem) as IGridItemProvider;
				Verify.AreEqual(i, gridItem?.Column ?? -1, $"Cell peer {i} must report visible column {i}.");
			}
			Verify.AreEqual("Asha", ((TableViewCellAutomationPeer)children[0]).GetName(),
				"The first cell peer must belong to the first visible column.");
			Verify.AreEqual("Designer", ((TableViewCellAutomationPeer)children[1]).GetName(),
				"The second cell peer must belong to the third column, the next visible one.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer reports its grid coordinates and unit spans.")]
	public async Task VerifyCellPeerGridItemCoordinates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 1);
			var cellPeer = GetCellPeer(rowPeer, 2);

			// TableView.idl:676-683 declares IGridItemProvider on the cell peer.
			var gridItem = cellPeer.GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (gridItem == null)
			{
				Verify.Fail("The cell peer must advertise GridItem.");
				return;
			}

			// A cell reporting a position inconsistent with the one GetItem used to find it makes
			// Narrator's "row 2, column 3" contradict where the user actually is.
			Verify.AreEqual(1, gridItem.Row, "The cell must report the row index it was reached through.");
			Verify.AreEqual(2, gridItem.Column, "The cell must report its visible-column index.");

			// Spans of 1 are not spec'd; TableView has no merged cells, so anything else would be
			// meaningless.
			Verify.AreEqual(1, gridItem.RowSpan, "Cells never span rows.");
			Verify.AreEqual(1, gridItem.ColumnSpan, "Cells never span columns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the public cell peer constructor rejects arguments that cannot describe a cell, and accepts ones that can.")]
	public async Task VerifyCellPeerConstructorValidatesArguments()
	{
		TableView tableView = null;
		TableView otherTable = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			otherTable = CreateAutomationTable();
			var host = new StackPanel();
			host.Children.Add(tableView);
			host.Children.Add(otherTable);
			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRealizedRows(tableView).First();
			var cell = GetRowCell(row, 0);
			var column = tableView.Columns[0];
			int count = tableView.Columns.Count;

			Verify.Throws<ArgumentException>(() => new TableViewCellAutomationPeer(null, row, column, 0), "A null cell must be rejected.");
			Verify.Throws<ArgumentException>(() => new TableViewCellAutomationPeer(cell, null, column, 0), "A null row must be rejected.");
			Verify.Throws<ArgumentException>(() => new TableViewCellAutomationPeer(cell, row, null, 0), "A null column must be rejected.");
			Verify.Throws<ArgumentException>(() => new TableViewCellAutomationPeer(cell, row, column, -1), "A negative column index must be rejected.");
			Verify.Throws<ArgumentException>(() => new TableViewCellAutomationPeer(cell, row, column, count), "A column index past the table's columns must be rejected.");
			Verify.Throws<ArgumentException>(() => new TableViewCellAutomationPeer(cell, row, otherTable.Columns[0], 0), "A column from another table must be rejected.");

			var peer = new TableViewCellAutomationPeer(cell, row, column, count - 1);
			Verify.IsNotNull(peer, "In-range arguments must construct a peer.");

			// A row no TableView owns has no column set to check against, so only the index sign is enforced.
			Verify.IsNotNull(new TableViewCellAutomationPeer(new Grid(), new TableViewRow(), new TableViewTextColumn(), 3),
				"An unowned row must accept any non-negative index.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer whose row is gone throws ElementNotAvailable from GridItem.Row instead of reporting -1.")]
	public async Task VerifyStaleCellPeerGridItemRowThrowsElementNotAvailable()
	{
		TableView tableView = null;
		IGridItemProvider gridItem = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			gridItem = GetCellPeer(RequireRowPeer(tableView, 1), 2).GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			Verify.IsNotNull(gridItem, "Precondition: the cell peer advertises GridItem.");
			Verify.AreEqual(1, gridItem.Row, "Precondition: the live cell reports its row.");

			tableView.ItemsSource = new List<Person>();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyGridItemRowThrowsElementNotAvailable(gridItem, "a cell whose row was removed");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer replaced by a column reorder on the same row throws ElementNotAvailable from GridItem.Row and Column.")]
	public async Task VerifyRebuiltCellPeerGridItemThrowsElementNotAvailable()
	{
		TableView tableView = null;
		IGridItemProvider gridItem = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			gridItem = GetCellPeer(RequireRowPeer(tableView, 0), 0).GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			Verify.IsNotNull(gridItem, "Precondition: the cell peer advertises GridItem.");
			Verify.AreEqual(0, gridItem.Column, "Precondition: the live cell reports its column.");

			// Only the row's cells are rebuilt, so the old provider's column would name another cell.
			var alias = tableView.Columns[2];
			tableView.Columns.RemoveAt(2);
			tableView.Columns.Insert(0, alias);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyGridItemCoordinateThrowsElementNotAvailable(() => gridItem.Row, "Row", "a cell rebuilt by a column reorder");
			VerifyGridItemCoordinateThrowsElementNotAvailable(() => gridItem.Column, "Column", "a cell rebuilt by a column reorder");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a recycled group header peer throws ElementNotAvailable from GridItem.Row instead of reporting -1.")]
	public async Task VerifyRecycledGroupHeaderGridItemRowThrowsElementNotAvailable()
	{
		TableView tableView = null;
		IGridItemProvider gridItem = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			gridItem = GetGroupHeaderPeer(tableView, 0).GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			Verify.IsNotNull(gridItem, "Precondition: the group header peer advertises GridItem.");
			Verify.AreEqual(0, gridItem.Row, "Precondition: the live header reports its row.");

			tableView.ItemsSource = new List<Person>();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyGridItemRowThrowsElementNotAvailable(gridItem, "a recycled group header");
		});
	}

	private static void VerifyGridItemRowThrowsElementNotAvailable(IGridItemProvider gridItem, string subject)
		=> VerifyGridItemCoordinateThrowsElementNotAvailable(() => gridItem.Row, "Row", subject);

	private static void VerifyGridItemCoordinateThrowsElementNotAvailable(Func<int> read, string coordinate, string subject)
	{
		Exception error = null;
		int value = int.MinValue;
		try
		{
			value = read();
		}
		catch (Exception exception)
		{
			error = exception;
		}

		Verify.AreNotEqual(-1, value, $"GridItem.{coordinate} on {subject} must not report -1, which reads as a real coordinate.");
		Verify.IsNotNull(error, $"GridItem.{coordinate} on {subject} must throw; it returned {value}.");
		Verify.AreEqual(unchecked((int)0x80040201), error.HResult, $"GridItem.{coordinate} on {subject} must fail with UIA_E_ELEMENTNOTAVAILABLE.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer reports its containing grid and exactly one column header, and no row headers.")]
	public async Task VerifyCellPeerTableItemReportsContainingGridAndColumnHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 1);

			// TableView.idl:676-683 declares ITableItemProvider. Only non-null and count are
			// assertable - IRawElementProviderSimple is opaque to a C# test.
			var tableItem = cellPeer.GetPattern(PatternInterface.TableItem) as ITableItemProvider;
			if (tableItem == null)
			{
				Verify.Fail("The cell peer must advertise TableItem.");
				return;
			}

			var gridItem = cellPeer.GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			Verify.IsNotNull(gridItem?.ContainingGrid, "A cell must report the grid that contains it.");

			// This is how a user orients themselves in an unfamiliar table: Narrator announces which
			// column the cell belongs to.
			var columnHeaders = tableItem.GetColumnHeaderItems();
			Verify.AreEqual(1, columnHeaders == null ? 0 : columnHeaders.Length,
				"A cell must report exactly one column header.");

			var rowHeaders = tableItem.GetRowHeaderItems();
			Verify.AreEqual(0, rowHeaders == null ? 0 : rowHeaders.Length, "TableView has no row headers.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell's automation name is its value while column headers are shown.")]
	public async Task VerifyCellNameIsValueWhenColumnHeadersAreShown()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 0);

			Verify.AreEqual("Asha", cellPeer.GetName(),
				"The cell name must be the cell value while the column header is reachable.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell name does not repeat a column header that GetColumnHeaderItems already reports.")]
	public async Task VerifyCellNameDoesNotRepeatReachableColumnHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var cellPeer = GetCellPeer(RequireRowPeer(tableView, 0), 0);
			var tableItem = cellPeer.GetPattern(PatternInterface.TableItem) as ITableItemProvider;
			if (tableItem == null)
			{
				Verify.Fail("The cell peer must advertise TableItem.");
				return;
			}

			// Screen readers already speak the header items on entering a column.
			var columnHeaders = tableItem.GetColumnHeaderItems();
			Verify.AreEqual(1, columnHeaders == null ? 0 : columnHeaders.Length,
				"The shown column header must be reported through GetColumnHeaderItems.");
			Verify.AreEqual("Asha", cellPeer.GetName(),
				"The cell name must not repeat the column header reported through GetColumnHeaderItems.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell name carries the column header only while column headers are hidden.")]
	public async Task VerifyCellNameKeepsHeaderPrefixWhenHeadersHidden()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var cellPeer = GetCellPeer(RequireRowPeer(tableView, 0), 0);
			var tableItem = cellPeer.GetPattern(PatternInterface.TableItem) as ITableItemProvider;
			if (tableItem == null)
			{
				Verify.Fail("The cell peer must advertise TableItem.");
				return;
			}

			Verify.AreEqual("Name, Asha", cellPeer.GetName(),
				"With column headers hidden the cell name must carry the column header.");
			var hiddenHeaders = tableItem.GetColumnHeaderItems();
			Verify.AreEqual(0, hiddenHeaders == null ? 0 : hiddenHeaders.Length,
				"With column headers hidden GetColumnHeaderItems must be empty.");

			tableView.HeadersVisibility = TableViewHeadersVisibility.Column;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var cellPeer = GetCellPeer(RequireRowPeer(tableView, 0), 0);
			Verify.AreEqual("Asha", cellPeer.GetName(),
				"Once column headers are shown again the cell name must drop the header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell name drops a missing header without a dangling separator and names an empty cell blank, with headers shown or hidden.")]
	public async Task VerifyCellNameFallsBackWhenEitherPartIsMissing()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var items = new List<Person> { new Person { Name = string.Empty, Role = "Designer" } };

			tableView = CreateTableViewShell(items, 500, 300);

			// A non-string header has no textual prefix. It must be an object that is neither
			// IStringable nor a String-typed IPropertyValue - a boxed primitive is NOT one, because
			// PropertyValue implements IStringable and stringifies happily.
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new Border { Width = 20, Height = 12 },
				Binding = new Binding { Path = new PropertyPath("Role"), Mode = BindingMode.OneWay },
			});

			// A string header over an empty value.
			tableView.Columns.Add(MakeTextColumn("Name"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);

			// Spec states the composed form but not the degenerate cases; reasoned from the composed
			// form being meaningless when a part is empty. A dangling ", " is announced literally by
			// Narrator ("comma Designer"), and a name that is only punctuation is worse than none.
			Verify.AreEqual("Designer", GetCellPeer(rowPeer, 0).GetName(),
				"With no string header the value must stand alone, with no leading separator.");
			Verify.AreEqual("blank", GetCellPeer(rowPeer, 1).GetName(),
				"An empty cell under a shown header must be named blank, not after the header GetColumnHeaderItems already reports.");

			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Name, blank", GetCellPeer(RequireRowPeer(tableView, 0), 1).GetName(),
				"An empty cell under a hidden header must carry the header and still read as blank.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a hidden non-string column header still names the cell through the header's automation name.")]
	public async Task VerifyCellNameUsesObjectHeaderAutomationNameWhenHeadersHidden()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			var icon = new SymbolIcon { Symbol = Symbol.Important };
			AutomationProperties.SetName(icon, "Priority");
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = icon,
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			});
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Priority, Asha", GetCellPeer(RequireRowPeer(tableView, 0), 0).GetName(),
				"A hidden object header must prefix the cell name with the header's automation name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the table reports no column headers while HeadersVisibility hides them, matching its cells.")]
	public async Task VerifyTableColumnHeadersFollowHeadersVisibility()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var table = RequireTablePeer(tableView).GetPattern(PatternInterface.Table) as ITableProvider;
			if (table == null)
			{
				Verify.Fail("The TableView peer must advertise the Table pattern.");
				return;
			}

			var hidden = table.GetColumnHeaders();
			Verify.AreEqual(0, hidden == null ? 0 : hidden.Length,
				"With column headers hidden the table must report none, as its cells' GetColumnHeaderItems do.");

			tableView.HeadersVisibility = TableViewHeadersVisibility.Column;
			var shown = table.GetColumnHeaders();
			Verify.AreEqual(3, shown == null ? 0 : shown.Length,
				"With column headers shown the table must report one per visible column.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell holding an interactive control is named after its column header, with headers shown or hidden.")]
	public async Task VerifyInteractiveCellNameIsColumnHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			tableView.Columns.Add(MakeTemplateColumn("Actions", (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
					  <Button Content=""Delete"" />
				  </DataTemplate>")));
			tableView.Columns.Add(MakeTemplateColumn("Active", (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
					  <ToggleSwitch />
				  </DataTemplate>")));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		foreach (var visibility in new[] { TableViewHeadersVisibility.Column, TableViewHeadersVisibility.None })
		{
			RunOnUIThread.Execute(() =>
			{
				tableView.HeadersVisibility = visibility;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var rowPeer = RequireRowPeer(tableView, 0);

				// The control names itself ("Delete"), so a cell named "Delete" would be heard twice.
				Verify.AreEqual("Actions", GetCellPeer(rowPeer, 0).GetName(),
					$"A Button cell must be named after its column header (HeadersVisibility {visibility}).");
				Verify.AreEqual("Active", GetCellPeer(rowPeer, 1).GetName(),
					$"An unnamed ToggleSwitch cell must be named after its column header (HeadersVisibility {visibility}).");
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell showing an unnamed non-text visual is named after its column, not \"blank\".")]
	public async Task VerifyUnnamedVisualCellIsNotNamedBlank()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			tableView.Columns.Add(MakeTemplateColumn("Progress", (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
					  <ProgressBar Value=""40"" Width=""80"" />
				  </DataTemplate>")));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Progress", GetCellPeer(RequireRowPeer(tableView, 0), 0).GetName(),
				"A cell showing an unnamed ProgressBar is not blank; it is named after its column header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a template column's cell name uses the template content's automation name.")]
	public async Task VerifyTemplateColumnCellNameUsesContentAutomationName()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "Status",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <Border AutomationProperties.Name=""Active"" Width=""40"" Height=""20"" />
					  </DataTemplate>"),
			});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 0);

			// Spec silent; reasoned from the text-column case being named and template columns having
			// no reason to be a second-class citizen. A cell holding a button, a rating or a status
			// glyph is a common shape, and it must not read as nothing.
			Verify.AreEqual("Active", cellPeer.GetName(),
				"A template column's cell name must incorporate the template content's automation name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the row peer advertises SelectionItem under SelectionMode.Single and withholds it under None.")]
	public async Task VerifyRowPeerSelectionItemPatternTracksSelectionMode()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);

			// TableView.idl:629-633 declares ISelectionItemProvider on the row peer unconditionally,
			// so - as with the table-level selection test - the withholding half is reasoned from
			// UIA guidance rather than stated, and carries the same (needs spec decision).
			Verify.IsNotNull(rowPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider,
				"SelectionItem must be advertised while SelectionMode is Single.");

			tableView.SelectionMode = TableViewSelectionMode.None;
			tableView.UpdateLayout();

			Verify.IsNull(RequireRowPeer(tableView, 0).GetPattern(PatternInterface.SelectionItem),
				"SelectionItem must be withheld under SelectionMode.None; otherwise AT offers a per-row select the control refuses.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies row peers report IsSelected in step with the control's selection.")]
	public async Task VerifyRowPeerIsSelectedMatchesControlSelection()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			tableView.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			for (int i = 0; i < 3; i++)
			{
				var provider = RequireSelectionItemProvider(tableView, i);
				if (provider == null)
				{
					return;
				}

				Verify.AreEqual(i == 1, provider.IsSelected,
					$"Row {i} must report IsSelected == {i == 1}; otherwise Narrator announces the wrong row as selected.");
			}

			tableView.DeselectAll();
			tableView.UpdateLayout();

			for (int i = 0; i < 3; i++)
			{
				var provider = RequireSelectionItemProvider(tableView, i);
				if (provider == null)
				{
					return;
				}

				Verify.IsFalse(provider.IsSelected, $"Row {i} must report unselected after DeselectAll().");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies row-peer Select() makes the row the selection and AddToSelection() does not toggle it off.")]
	public async Task VerifyRowPeerSelectDrivesTheControlAndAddToSelectionDoesNotToggle()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = RequireSelectionItemProvider(tableView, 2);
			if (provider == null)
			{
				return;
			}

			provider.Select();
			tableView.UpdateLayout();
			Verify.AreEqual(2, tableView.SelectedIndex, "Select() on a row peer must make that row the control's selection.");

			// UIA forbids Select() from meaning "toggle": a client calling it twice would clear the
			// selection. AddToSelection on the row that is already the selection is a no-op.
			provider.AddToSelection();
			tableView.UpdateLayout();
			Verify.AreEqual(2, tableView.SelectedIndex, "AddToSelection() on the already selected row must leave it selected, not toggle it off.");
			Verify.IsTrue(provider.IsSelected, "The row must still report itself selected.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies AddToSelection in Single mode fails with UIA_E_INVALIDOPERATION when another row is selected, and leaves the selection alone (#12448).")]
	public async Task VerifyRowPeerAddToSelectionFailsWhenAnotherRowIsSelectedInSingleMode()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var first = RequireSelectionItemProvider(tableView, 1);
			var second = RequireSelectionItemProvider(tableView, 2);
			if (first == null || second == null)
			{
				return;
			}

			first.AddToSelection();
			tableView.UpdateLayout();
			Verify.AreEqual(1, tableView.SelectedIndex, "AddToSelection() with nothing selected must select the row.");

			int selectionChangedCount = 0;
			tableView.SelectionChanged += (s, e) => selectionChangedCount++;

			VerifyFailsWithHResult(UIA_E_INVALIDOPERATION, () => second.AddToSelection(),
				"AddToSelection() on another row of a single-select table must fail.");
			tableView.UpdateLayout();
			Verify.AreEqual(1, tableView.SelectedIndex, "A failed AddToSelection() must leave the selection on the first row.");
			Verify.IsFalse(second.IsSelected, "The second row must not become selected.");
			Verify.AreEqual(0, selectionChangedCount, "A failed AddToSelection() must not raise SelectionChanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies AddToSelection in Single mode, called while a row's IsSelected change is being raised, follows the selection model rather than the not-yet-updated SelectedIndex.")]
	public async Task VerifyRowPeerAddToSelectionDuringIsSelectedCallbackUsesModelSelection()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowOne = (TableViewRow)RequireRowPeer(tableView, 1).Owner;
			var first = RequireSelectionItemProvider(tableView, 1);
			var second = RequireSelectionItemProvider(tableView, 2);
			var zero = RequireSelectionItemProvider(tableView, 0);
			if (first == null || second == null || zero == null)
			{
				return;
			}

			first.Select();
			tableView.UpdateLayout();
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 is the selection.");

			// Moving the selection to row 2 deselects row 1 first; at that point the model already
			// selects row 2 while SelectedIndex still names row 1.
			ISelectionItemProvider addFromCallback = second;
			int callbackCount = 0;
			global::System.Exception callbackError = null;
			long token = rowOne.RegisterPropertyChangedCallback(TableViewRow.IsSelectedProperty, (s, dp) =>
			{
				if (rowOne.IsSelected || addFromCallback == null)
				{
					return;
				}
				var target = addFromCallback;
				addFromCallback = null;
				callbackCount++;
				try
				{
					target.AddToSelection();
				}
				catch (global::System.Exception ex)
				{
					callbackError = ex;
				}
			});

			try
			{
				second.Select();
				tableView.UpdateLayout();
				Verify.AreEqual(1, callbackCount, "Row 1's IsSelected callback must run once.");
				Verify.IsNull(callbackError, "AddToSelection() on the row the model already selects must not fail: " + callbackError?.Message);
				Verify.AreEqual(2, tableView.SelectedIndex, "The selection must end on row 2.");

				// Clearing the selection: the model is empty while SelectedIndex still names row 1.
				first.Select();
				tableView.UpdateLayout();
				Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 is the selection again.");
				addFromCallback = zero;
				callbackCount = 0;
				first.RemoveFromSelection();
				tableView.UpdateLayout();
				Verify.AreEqual(1, callbackCount, "Row 1's IsSelected callback must run once on the clear.");
				Verify.IsNull(callbackError, "AddToSelection() with an empty selection model must not fail: " + callbackError?.Message);
				Verify.AreEqual(0, tableView.SelectedIndex, "AddToSelection() from the callback must select row 0.");
			}
			finally
			{
				rowOne.UnregisterPropertyChangedCallback(TableViewRow.IsSelectedProperty, token);
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies SelectionItem calls on a row of a disabled table fail with UIA_E_ELEMENTNOTENABLED and leave the selection alone.")]
	public async Task VerifyRowPeerSelectionItemFailsWhenTableIsDisabled()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var selected = RequireSelectionItemProvider(tableView, 1);
			var other = RequireSelectionItemProvider(tableView, 2);
			if (selected == null || other == null)
			{
				return;
			}

			selected.Select();
			tableView.IsEnabled = false;
			tableView.UpdateLayout();

			VerifyFailsWithHResult(UIA_E_ELEMENTNOTENABLED, () => other.Select(), "Select() on a disabled table must fail.");
			VerifyFailsWithHResult(UIA_E_ELEMENTNOTENABLED, () => other.AddToSelection(), "AddToSelection() on a disabled table must fail.");
			VerifyFailsWithHResult(UIA_E_ELEMENTNOTENABLED, () => selected.RemoveFromSelection(), "RemoveFromSelection() on a disabled table must fail.");
			Verify.AreEqual(1, tableView.SelectedIndex, "Failed calls on a disabled table must leave the selection unchanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies RemoveFromSelection on a row that is not the current selection is a no-op.")]
	public async Task VerifyRowPeerRemoveFromSelectionOnlyClearsWhenItIsTheSelection()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			tableView.UpdateLayout();
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 is the selection.");

			var otherRow = RequireSelectionItemProvider(tableView, 2);
			if (otherRow == null)
			{
				return;
			}

			// Spec silent; reasoned from the operation being scoped to "this row". This is a real
			// sequence rather than a synthetic one: AT clients routinely cache providers across a
			// selection change, and a stale one must not be able to wipe the current selection.
			otherRow.RemoveFromSelection();
			tableView.UpdateLayout();

			Verify.AreEqual(1, tableView.SelectedIndex,
				"RemoveFromSelection() on a row that is not the selection must not clear the selection.");

			var selectedRow = RequireSelectionItemProvider(tableView, 1);
			if (selectedRow == null)
			{
				return;
			}

			selectedRow.RemoveFromSelection();
			tableView.UpdateLayout();
			Verify.AreEqual(-1, tableView.SelectedIndex,
				"RemoveFromSelection() on the selected row must clear the selection.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a row peer reports a selection container, linking the row back to the grid.")]
	public async Task VerifyRowPeerSelectionContainerIsTheTableView()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = RequireSelectionItemProvider(tableView, 0);
			if (provider == null)
			{
				return;
			}

			// Only non-null is assertable; the provider is opaque. Kept because it costs two lines
			// inside a class that already constructs everything it needs, and a null breaks the
			// selection model's parent link.
			Verify.IsNotNull(provider.SelectionContainer, "A row peer must report the grid that owns its selection.");
		});
	}

	// ---------- 12.4 Header and group peers ----------

	[TestMethod]
	[TestProperty("Description", "Verifies a column header peer names itself from a string Header.")]
	public async Task VerifyColumnHeaderPeerNameUsesStringHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);

			// The base implementation would produce the TableView's own name, because the peer's
			// Owner() is the TableView - so every column would read identically.
			Verify.AreEqual("Name", peer.GetName(), "A string Header must be the column header peer's name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a template-header column peer falls back to the realized header cell's automation name.")]
	public async Task VerifyColumnHeaderPeerNameFallsBackToRealizedHeaderForTemplateHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);

			// A non-string header rendered through HeaderTemplate, with an automation name on the
			// template root. The header object must be neither IStringable nor a String-typed
			// IPropertyValue, or TryGetColumnHeaderText short-circuits the fallback under test -
			// a boxed primitive stringifies and would never reach it.
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new Border { Width = 20, Height = 12 },
				HeaderTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <Border AutomationProperties.Name=""Priority"" Width=""30"" Height=""16"" />
					  </DataTemplate>"),
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);
			var name = peer.GetName();

			// Spec silent on the fallback ORDER; reasoned from string headers being the documented
			// primary source and template headers needing some name.
			Verify.AreEqual("Priority", name,
				"A template header's peer must take its name from the realized header, not from the TableView owner and not empty.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a templated managed-object header is named from its template, not from the object's ToString.")]
	public async Task VerifyColumnHeaderNameIgnoresManagedObjectToStringWhenTemplated()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new TitledColumnHeader { Title = "CPU" },
				HeaderTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <TextBlock Text=""{Binding Title}"" />
					  </DataTemplate>"),
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var name = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]).GetName();

			Verify.AreEqual("CPU", name, "A templated object header must be named from the rendered template.");
			Verify.IsFalse(name.Contains(nameof(TitledColumnHeader)),
				$"A templated object header must not be named after its type; saw '{name}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an unnamed template header is not named by the resize gripper in its header cell.")]
	public async Task VerifyUnnamedTemplateHeaderIsNotNamedByResizeGripper()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			tableView.CanUserResizeColumns = true;
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new Border { Width = 20, Height = 12 },
				CanResize = true,
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var gripperPeer = FrameworkElementAutomationPeer.CreatePeerForElement(RequireGripper(tableView, 0));
			var gripperName = gripperPeer?.GetName();
			Verify.IsFalse(string.IsNullOrEmpty(gripperName), "Precondition: the gripper must have a name.");

			var name = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]).GetName();
			Log.Comment($"Header name: '{name}'; gripper name: '{gripperName}'.");
			Verify.IsFalse(!string.IsNullOrEmpty(name) && name.Contains(gripperName),
				$"An unnamed header must not take the resize gripper's name; saw '{name}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies column header peers have pairwise-distinct runtime ids and keep their identity across enumerations.")]
	public async Task VerifyColumnHeaderPeersAreDistinctAndStable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var table = peer.GetPattern(PatternInterface.Table) as ITableProvider;
			if (table == null)
			{
				Verify.Fail("The TableView peer must advertise the Table pattern.");
				return;
			}

			var first = table.GetColumnHeaders();
			var second = table.GetColumnHeaders();
			Verify.AreEqual(3, first == null ? 0 : first.Length, "Every visible column must contribute a header provider.");
			Verify.AreEqual(3, second == null ? 0 : second.Length, "A second enumeration must report the same three headers.");

			// Peer identity across enumerations is NOT assertable from a managed test: the array
			// holds IRawElementProviderSimple, ProviderFromPeer builds a fresh provider on each
			// enumeration, and the projection gives no stable reference identity for either. The
			// cache in TableViewAutomationPeer::GetColumnHeaders is what preserves peer identity,
			// and it is reachable only by code inspection. What IS observable is that the header
			// set is stable in shape and that its members stay individually identifiable.

			// The product states this as an invariant, with the reasoning written into
			// TableViewColumnHeaderAutomationPeer::GetRuntimeIdCore: the owner-derived id the base
			// class supplies is IDENTICAL for every column, which is a UIA protocol violation. A
			// self-contained per-column id is built instead.
			//
			// GetRuntimeId() is not callable from a managed test - only GetRuntimeIdCore is, and it
			// is protected. The generated automation id is the observable proxy: it is derived from
			// the same per-column IUnknown pointer, so if the runtime id collapsed to one value per
			// owner the generated id would collapse with it.
			var generatedIds = tableView.Columns
				.Select(column => new TableViewColumnHeaderAutomationPeer(tableView, column).GetAutomationId())
				.ToList();

			foreach (var id in generatedIds)
			{
				Log.Comment($"Column header automation id: {id}");
				Verify.IsFalse(string.IsNullOrEmpty(id),
					"Each column header peer must generate a per-column automation id when none is authored.");
			}

			Verify.AreEqual(generatedIds.Count, generatedIds.Distinct().Count(),
				"Column header ids must be pairwise distinct; identical ids make the headers indistinguishable to AT and conflate client caches.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an authored AutomationId on a realized header cell wins over the generated per-column fallback.")]
	public async Task VerifyColumnHeaderAutomationIdPrefersAuthoredId()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerCells = GetHeaderCells(tableView);
			if (headerCells.Count < 2)
			{
				Verify.Fail($"Expected at least two realized header cells; saw {headerCells.Count}.");
				return;
			}

			AutomationProperties.SetAutomationId(headerCells[0], "AuthoredNameColumn");
			tableView.UpdateLayout();

			var authored = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);
			var generated = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]);

			// An app that labels its headers for UI automation testing must not find its ids
			// silently ignored.
			Verify.AreEqual("AuthoredNameColumn", authored.GetAutomationId(), "An authored AutomationId must win.");

			// Without the generated fallback, headers are unaddressable before their templates
			// realize - which is the whole reason the peer is enumerable from Columns().
			var generatedId = generated.GetAutomationId();
			Log.Comment($"Generated automation id: '{generatedId}'");
			Verify.IsFalse(string.IsNullOrEmpty(generatedId), "A column with no authored id must still get a non-empty id.");
			Verify.AreNotEqual(authored.GetAutomationId(), generatedId, "Generated ids must differ between columns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies column header PositionInSet and SizeOfSet are 1-based over visible columns.")]
	public async Task VerifyColumnHeaderPositionInSetAndSizeOfSetUseVisibleColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[0].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// TableView-dev-spec.md:329 names PositionInSet / SizeOfSet explicitly among the values
			// that are on a visible-column basis, so this is directly spec-backed rather than
			// inferred from the count tests. Announcing "column 2 of 3" while only two columns
			// exist tells the user to look for a column that is not there.
			var second = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]);
			var third = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[2]);

			Verify.AreEqual(1, second.GetPositionInSet(), "The first visible column must report position 1.");
			Verify.AreEqual(2, second.GetSizeOfSet(), "SizeOfSet must count visible columns only.");
			Verify.AreEqual(2, third.GetPositionInSet(), "The second visible column must report position 2.");
			Verify.AreEqual(2, third.GetSizeOfSet(), "SizeOfSet must be the same for every header in the band.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Invoke pattern is withheld when either sort gate is closed.")]
	public async Task VerifyInvokePatternIsWithheldWhenTheColumnCannotSort()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserSortColumns = true;
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// TableView.idl:666-670 declares IInvokeProvider unconditionally; the two gates come from
			// the sorting contract. Category 7.5 owns the positive path (invoke actually sorts).
			var column = tableView.Columns[0];
			Verify.IsNotNull(new TableViewColumnHeaderAutomationPeer(tableView, column).GetPattern(PatternInterface.Invoke),
				"Invoke must be offered when both sort gates are open.");

			// Offering "invoke to sort" on a column the control will not sort is worse than no
			// affordance: the action appears to succeed and nothing happens.
			tableView.CanUserSortColumns = false;
			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, column).GetPattern(PatternInterface.Invoke),
				"Invoke must be withheld while CanUserSortColumns is false.");

			tableView.CanUserSortColumns = true;
			column.CanSort = false;
			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, column).GetPattern(PatternInterface.Invoke),
				"Invoke must be withheld while the column's CanSort is false.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header is keyboard-focusable only while actionable, and Invoke is offered only when the column has a sort key.")]
	public async Task VerifyHeaderKeyboardFocusabilityAndInvokeTrackActionability()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns.Add(new TableViewTemplateColumn { Header = "NoSortKey" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[3]).GetPattern(PatternInterface.Invoke),
				"A column with CanSort but no sort key must not offer Invoke.");
			tableView.CanUserResizeColumns = false;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(GetHeaderCell(tableView, 0));
			Verify.IsTrue(peer.IsKeyboardFocusable(), "Sorting alone makes a header keyboard-focusable.");
			Verify.IsFalse(FrameworkElementAutomationPeer.CreatePeerForElement(GetHeaderCell(tableView, 3)).IsKeyboardFocusable(),
				"A header that can neither sort nor resize is not keyboard-focusable.");

			tableView.CanUserSortColumns = false;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = (Control)GetHeaderCell(tableView, 0);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(header);
			Verify.IsFalse(peer.IsKeyboardFocusable(), "A header with nothing to do is not keyboard-focusable.");
			header.Focus(FocusState.Programmatic);
			Verify.AreNotEqual<object>(header, Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(header.XamlRoot), "Programmatic focus on a non-actionable header never lands on it.");

			tableView.Columns[0].HeaderToolTip = "Tip";
			Verify.IsTrue(peer.IsKeyboardFocusable(), "A header tooltip makes the header actionable.");
			header.Focus(FocusState.Programmatic);
			Verify.AreEqual<object>(header, Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(header.XamlRoot), "An actionable header takes focus.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView.Focus aimed at a non-actionable header lands on the table's entry header instead of failing.")]
	public async Task VerifyTableFocusSkipsNonActionableHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserResizeColumns = false;
			tableView.Columns[0].CanSort = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Focus(FocusState.Programmatic);
			Verify.AreEqual<object>(GetHeaderCell(tableView, 1), Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(tableView.XamlRoot),
				"Focus must land on the first actionable header, not the non-actionable first header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies clearing the only HeaderContextFlyout of a focused header takes it out of the tab order and hands focus to the next actionable header.")]
	public async Task VerifyClearingFocusedHeaderContextFlyoutMovesFocus()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserResizeColumns = false;
			tableView.Columns[0].CanSort = false;
			tableView.Columns[0].HeaderContextFlyout = new MenuFlyout();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = (Control)GetHeaderCell(tableView, 0);
			Verify.IsTrue(header.Focus(FocusState.Keyboard), "Precondition: a header with a context flyout takes focus.");
			tableView.Columns[0].HeaderContextFlyout = null;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(((Control)GetHeaderCell(tableView, 0)).IsTabStop, "The header with nothing left to do leaves the tab order.");
			Verify.AreEqual<object>(GetHeaderCell(tableView, 1), Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(tableView.XamlRoot),
				"Focus must move to the next actionable header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a directional focus move aimed at a row of a collapsed group, still parented in the repeater's pool, is refused and focus stays on the group header.")]
	public async Task VerifyDirectionalFocusNeverLandsOnCollapsedGroupRow()
	{
		TableView tableView = null;
		TableViewGroupHeader groupHeader = null;
		TableViewRow hiddenRow = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTwoGroupAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => tableView.CollapseAllGroups());
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var repeater = GetRowsRepeater(tableView);
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(repeater); i++)
			{
				var child = VisualTreeHelper.GetChild(repeater, i) as UIElement;
				int index = repeater.GetElementIndex(child);
				if (child is TableViewGroupHeader header && index == 0)
				{
					groupHeader = header;
				}
				else if (child is TableViewRow row && index < 0 && row.Visibility == Visibility.Visible)
				{
					hiddenRow = row;
				}
			}
			Verify.IsNotNull(groupHeader, "Precondition: the first group header is realized.");
			Verify.IsNotNull(hiddenRow, "Precondition: a collapsed row container is still parented in the repeater.");
			Verify.IsTrue(groupHeader.Focus(FocusState.Keyboard), "Precondition: the first group header takes focus.");

			bool redirected = false;
			EventHandler<Microsoft.UI.Xaml.Input.GettingFocusEventArgs> redirect = (s, e) =>
			{
				if (!redirected && e.Direction == Microsoft.UI.Xaml.Input.FocusNavigationDirection.Up)
				{
					redirected = true;
					e.TrySetNewFocusedElement(hiddenRow);
				}
			};
			Microsoft.UI.Xaml.Input.FocusManager.GettingFocus += redirect;
			try
			{
				Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Up,
					new Microsoft.UI.Xaml.Input.FindNextElementOptions { SearchRoot = tableView.XamlRoot.Content });
			}
			finally
			{
				Microsoft.UI.Xaml.Input.FocusManager.GettingFocus -= redirect;
			}
			Verify.IsTrue(redirected, "Precondition: an Up move was raised.");

			var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(tableView.XamlRoot);
			Log.Comment($"Focused {focused?.GetType().Name}.");
			Verify.AreNotEqual<object>(hiddenRow, focused, "A directional move must never land on a row of a collapsed group.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing ItemsSource while a row has focus does not pull focus onto the header row.")]
	public async Task VerifyItemsSourceReplacementKeepsFocusOffHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(GetRealizedRows(tableView)[0].Focus(FocusState.Keyboard), "Precondition: the first row takes focus.");
			tableView.ItemsSource = MakeItems();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"Focused {focused?.GetType().Name}.");
			bool inRow = false;
			for (var node = focused; node != null; node = VisualTreeHelper.GetParent(node))
			{
				Verify.AreNotEqual<object>(GetHeaderHost(tableView), node, "The focus rescue must not land on the header row.");
				inRow |= node is TableViewRow;
			}
			Verify.IsTrue(inRow, "The focus rescue must land in the body.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that in a read-only table a cell whose only interactive content is an inline Hyperlink is keyboard-focusable.")]
	public async Task VerifyCellWithInlineHyperlinkIsActionable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "Link",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <TextBlock><Run Text=""See ""/><Hyperlink><Run Text=""details""/></Hyperlink></TextBlock>
					  </DataTemplate>"),
			});
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRealizedRows(tableView)[0];
			Verify.IsTrue(GetCellPeer(row, 3).IsKeyboardFocusable(), "A cell with an inline Hyperlink is actionable.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that in a read-only table a cell is actionable when its only interactive content is a Hyperlink inside nested Spans, a focusable non-Control element, or content past the per-level scan cap, and not when the focusable element is under a disabled Control.")]
	public async Task VerifyCellActionabilityCoversNestedLinksFocusablePanelsAndUnscannedChildren()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "NestedLink",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <TextBlock><Bold><Italic><Hyperlink><Run Text=""details""/></Hyperlink></Italic></Bold></TextBlock>
					  </DataTemplate>"),
			});
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "Tile",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <Grid IsTabStop=""True"" Background=""Transparent""><TextBlock Text=""tile""/></Grid>
					  </DataTemplate>"),
			});
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "Many",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <StackPanel Orientation=""Horizontal"">" +
							  string.Concat(Enumerable.Repeat(@"<TextBlock Text=""x""/>", 40)) +
							  @"<Button Content=""Go""/>
						  </StackPanel>
					  </DataTemplate>"),
			});
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "DisabledTile",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <ContentControl IsEnabled=""False""><Grid IsTabStop=""True"" Background=""Transparent""><TextBlock Text=""tile""/></Grid></ContentControl>
					  </DataTemplate>"),
			});
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.IsReadOnly, "Precondition: TableView is read-only by default.");
			var row = GetRealizedRows(tableView)[0];
			Verify.IsTrue(GetCellPeer(row, 3).IsKeyboardFocusable(), "A Hyperlink inside nested Bold/Italic spans makes the cell actionable.");
			Verify.IsTrue(GetCellPeer(row, 4).IsKeyboardFocusable(), "A focusable Grid (IsTabStop) makes the cell actionable.");
			Verify.IsTrue(GetCellPeer(row, 5).IsKeyboardFocusable(), "Children past the per-level scan cap are assumed actionable.");
			Verify.IsFalse(GetCellPeer(row, 6).IsKeyboardFocusable(), "A focusable Grid under a disabled Control does not make the cell actionable.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a read-only cell whose TextBlock has a ContextFlyout binding that resolves to null, or fails and falls back to the built-in menu, is not actionable.")]
	public async Task VerifyNullContextFlyoutBindingDoesNotMakeCellActionable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "NullMenu",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <TextBlock Text=""{Binding Name}"" ContextFlyout=""{Binding Tag, RelativeSource={RelativeSource Self}}""/>
					  </DataTemplate>"),
			});
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "MissingMenu",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <TextBlock Text=""{Binding Name}"" ContextFlyout=""{Binding MissingMenu}""/>
					  </DataTemplate>"),
			});
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRealizedRows(tableView)[0];
			var nullMenu = FindVisualChildrenByType<TextBlock>(GetRowCell(row, 3)).First();
			Verify.IsNotNull(nullMenu.GetBindingExpression(UIElement.ContextFlyoutProperty), "Precondition: the TextBlock's ContextFlyout is a local binding.");
			Verify.IsFalse(nullMenu.ContextFlyout is MenuFlyout, "Precondition: the binding supplies no app menu.");
			Verify.IsFalse(GetCellPeer(row, 3).IsKeyboardFocusable(), "A ContextFlyout binding that resolves to null is not a menu.");
			Verify.IsFalse(GetCellPeer(row, 4).IsKeyboardFocusable(), "A failed ContextFlyout binding's built-in menu is not an app menu.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies refreshing the header tab stops keeps an app's local IsTabStop binding and honors an app's local IsTabStop=false.")]
	public async Task VerifyHeaderTabStopRefreshKeepsAppLocalIsTabStop()
	{
		TableView tableView = null;
		Control boundHeader = null;
		Control optedOutHeader = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			boundHeader = (Control)GetHeaderCell(tableView, 0);
			optedOutHeader = (Control)GetHeaderCell(tableView, 1);
			boundHeader.SetBinding(UIElement.IsTabStopProperty, new Binding { Source = true });
			optedOutHeader.IsTabStop = false;
			// Restamps the header tab stops.
			tableView.Columns[2].HeaderToolTip = "Tip";
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(boundHeader.GetBindingExpression(UIElement.IsTabStopProperty), "The app's IsTabStop binding must survive the refresh.");
			Verify.IsFalse(optedOutHeader.IsTabStop, "The app's local IsTabStop=false must survive the refresh.");
			Verify.IsFalse(FrameworkElementAutomationPeer.CreatePeerForElement(optedOutHeader).IsKeyboardFocusable(),
				"A header the app took out of the tab order is not keyboard-focusable.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header whose only action would be resizing is not actionable while the column is resize-pinned (MinWidth equals MaxWidth, or a lone Star column), and becomes a tab stop once it can resize.")]
	public async Task VerifyResizePinnedHeaderIsNotActionable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			var fixedColumn = MakeTextColumn("Name", width: new GridLength(120));
			fixedColumn.CanSort = false;
			fixedColumn.MinWidth = 120;
			fixedColumn.MaxWidth = 120;
			var starColumn = MakeTextColumn("Role", width: new GridLength(1, GridUnitType.Star));
			starColumn.CanSort = false;
			tableView.Columns.Add(fixedColumn);
			tableView.Columns.Add(starColumn);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.CanUserResizeColumns, "Precondition: columns are user-resizable.");
			for (int i = 0; i < 2; i++)
			{
				var header = (Control)GetHeaderCell(tableView, i);
				Verify.IsFalse(FrameworkElementAutomationPeer.CreatePeerForElement(header).IsKeyboardFocusable(), $"Resize-pinned header {i} is not keyboard-focusable.");
				Verify.IsFalse(header.IsTabStop, $"Resize-pinned header {i} is not a tab stop.");
			}
			tableView.Columns[0].MaxWidth = 300;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(((Control)GetHeaderCell(tableView, 0)).IsTabStop, "A header that can resize again is a tab stop.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an app ToolTip that renders through its own Template with null Content makes a header actionable, and an empty app ToolTip does not.")]
	public async Task VerifyTemplatedAppToolTipWithoutContentMakesHeaderActionable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserResizeColumns = false;
			tableView.Columns[0].CanSort = false;
			tableView.Columns[1].CanSort = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var templated = GetHeaderCell(tableView, 0);
			var empty = GetHeaderCell(tableView, 1);
			ToolTipService.SetToolTip(templated, new ToolTip
			{
				Template = (ControlTemplate)XamlReader.Load(
					@"<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" TargetType=""ToolTip"">
						  <TextBlock Text=""Templated help""/>
					  </ControlTemplate>"),
			});
			ToolTipService.SetToolTip(empty, new ToolTip());
			Verify.IsTrue(FrameworkElementAutomationPeer.CreatePeerForElement(templated).IsKeyboardFocusable(),
				"A templated app ToolTip with null Content makes the header actionable.");
			Verify.IsFalse(FrameworkElementAutomationPeer.CreatePeerForElement(empty).IsKeyboardFocusable(),
				"An app ToolTip with neither Content nor a Template is not an action.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a retained header peer reports IsKeyboardFocusable=false while HeadersVisibility hides the column headers, and the header is a tab stop again once they are shown.")]
	public async Task VerifyHiddenHeaderIsNotKeyboardFocusable()
	{
		TableView tableView = null;
		AutomationPeer peer = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			peer = FrameworkElementAutomationPeer.CreatePeerForElement(GetHeaderCell(tableView, 0));
			Verify.IsTrue(peer.IsKeyboardFocusable(), "Precondition: a sortable header is keyboard-focusable.");
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(peer.IsKeyboardFocusable(), "A hidden header is not keyboard-focusable.");
			// Restamps the header tab stops while the headers are hidden.
			tableView.Columns[0].HeaderToolTip = "Tip";
			tableView.HeadersVisibility = TableViewHeadersVisibility.Column;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(peer.IsKeyboardFocusable(), "A shown header is keyboard-focusable again.");
			Verify.IsTrue(((Control)GetHeaderCell(tableView, 0)).IsTabStop, "A shown actionable header is a tab stop again.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a text column with no Binding has no editor, so its cells are not actionable in an editable table, and UIA SetFocus on an editable cell focuses that exact cell.")]
	public async Task VerifyUnboundTextColumnCellIsNotActionable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.IsReadOnly = false;
			tableView.Columns.Add(new TableViewTextColumn { Header = "Unbound" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = GetRealizedRows(tableView)[0];
			Verify.IsFalse(GetCellPeer(row, 3).IsKeyboardFocusable(), "An unbound text column has no editor, so its cell is inactive.");
			Verify.IsTrue(GetCellPeer(row, 0).IsKeyboardFocusable(), "A bound text cell in an editable table is actionable.");
			GetCellPeer(row, 0).SetFocus();
			var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(tableView.XamlRoot);
			Verify.IsFalse(focused is TableViewRow, "UIA SetFocus on a cell must not leave focus on the row.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that in a read-only table a cell hosting a nested TableView is keyboard-focusable, while a plain text cell is not and refuses UIA SetFocus.")]
	public async Task VerifyCellHostingNestedTableIsActionable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "Nested",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
									xmlns:t=""using:Microsoft.UI.Xaml.Controls.Tabular"">
						  <t:TableView Height=""40"" />
					  </DataTemplate>"),
			});
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.IsReadOnly, "Precondition: TableView is read-only by default.");
			var row = GetRealizedRows(tableView)[0];
			Verify.IsFalse(GetCellPeer(row, 0).IsKeyboardFocusable(), "A read-only text cell with no tooltip is inactive.");
			Verify.IsTrue(GetCellPeer(row, 3).IsKeyboardFocusable(), "A cell hosting a nested TableView is actionable.");

			bool threw = false;
			try { GetCellPeer(row, 0).SetFocus(); } catch (Exception) { threw = true; }
			Verify.IsTrue(threw, "UIA SetFocus on an inactive cell must fail.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header disabled by its HeaderStyle reports IsEnabled=false on its realized and table-owned peers and refuses Invoke.")]
	public async Task VerifyHeaderDisabledByStyleIsNotEnabledOrInvokable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserSortColumns = true;
			tableView.Columns[0].CanSort = true;
			var disabledStyle = new Style(typeof(TableViewColumnHeader));
			disabledStyle.Setters.Add(new Setter(Control.IsEnabledProperty, false));
			tableView.Columns[0].HeaderStyle = disabledStyle;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var header = GetHeaderCell(tableView, 0) as Control;
			Verify.IsNotNull(header, "Precondition: the header cell is a Control.");
			Verify.IsFalse(header.IsEnabled, "Precondition: the HeaderStyle disables the header.");

			var peers = new (AutomationPeer Peer, string Kind)[]
			{
				(FrameworkElementAutomationPeer.CreatePeerForElement(header), "realized"),
				(new TableViewColumnHeaderAutomationPeer(tableView, column), "table-owned"),
			};
			foreach (var (peer, kind) in peers)
			{
				Verify.IsFalse(peer.IsEnabled(), $"The {kind} peer of a disabled header must report IsEnabled=false.");
				var invoke = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
				Verify.IsNotNull(invoke, $"Precondition: the {kind} peer offers Invoke for a sortable column.");
				VerifyFailsWithHResult(UIA_E_ELEMENTNOTENABLED, () => invoke.Invoke(),
					$"Invoke on the {kind} peer of a disabled header must fail.");
			}

			Verify.AreEqual(SortDirection.None, column.SortDirection, "A disabled header must not sort through UIA.");
		});
	}

	private const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
	private const int UIA_E_INVALIDOPERATION = unchecked((int)0x80131509);
	private const int E_INVALIDARG = unchecked((int)0x80070057);

	// By HRESULT rather than exception type: the projected type for a UIA_E_* code is not a contract.
	private static void VerifyFailsWithHResult(int expectedHResult, Action action, string message)
	{
		try
		{
			action();
		}
		catch (Exception e)
		{
			Verify.AreEqual(expectedHResult, e.HResult, $"{message} ({e.GetType().Name})");
			return;
		}

		Verify.Fail($"{message} The call did not fail.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Transform pattern is offered only for columns the user may resize, and never moves or rotates.")]
	public async Task VerifyTransformPatternTracksColumnResizability()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			tableView.Columns[1].CanResize = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// UIA requires Transform on a HeaderItem the user can resize.
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0])
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");
			Verify.IsTrue(transform.CanResize, "A resizable column must report CanResize.");
			Verify.IsFalse(transform.CanMove, "A column header cannot be moved through Transform.");
			Verify.IsFalse(transform.CanRotate, "A column header cannot be rotated.");

			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]).GetPattern(PatternInterface.Transform),
				"Transform must be withheld for a column whose CanResize is false.");

			tableView.CanUserResizeColumns = false;
			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]).GetPattern(PatternInterface.Transform),
				"Transform must be withheld while CanUserResizeColumns is false.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a pinned column keeps the Transform pattern but reports CanResize=false and rejects Resize.")]
	public async Task VerifyTransformCanResizeIsFalseForPinnedColumns()
	{
		TableView starTable = null;
		TableView pixelTable = null;

		RunOnUIThread.Execute(() =>
		{
			starTable = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(1.0, GridUnitType.Star)),
				("Second", new GridLength(1.0, GridUnitType.Star)));
			LoadContent(starTable);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Same rule as gripper visibility: the last Star column has no later Star column to trade with.
			var first = new TableViewColumnHeaderAutomationPeer(starTable, starTable.Columns[0])
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			var last = new TableViewColumnHeaderAutomationPeer(starTable, starTable.Columns[1])
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(first, "The first Star column opts in to resizing, so Transform must be offered.");
			Verify.IsNotNull(last, "The pinned Star column still opts in to resizing, so Transform must be offered.");
			Verify.IsTrue(first.CanResize, "A Star column with a later Star donor can be resized.");
			Verify.IsFalse(last.CanResize, "The last Star column is pinned, so CanResize must be false.");

			var widthBefore = starTable.Columns[1].Width;
			VerifyFailsWithHResult(UIA_E_INVALIDOPERATION, () => last.Resize(80.0, 0.0),
				"Resize on a pinned column must fail.");
			Verify.AreEqual(widthBefore, starTable.Columns[1].Width, "A rejected Resize must not touch Width.");

			pixelTable = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(120.0, GridUnitType.Pixel)));
			LoadContent(pixelTable);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var fixedColumn = pixelTable.Columns[0];
			fixedColumn.MinWidth = 120.0;
			fixedColumn.MaxWidth = 120.0;
			var fixedTransform = new TableViewColumnHeaderAutomationPeer(pixelTable, fixedColumn)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(fixedTransform, "A fixed-width column that opts in to resizing still offers Transform.");
			Verify.IsFalse(fixedTransform.CanResize, "MinWidth == MaxWidth pins the column, so CanResize must be false.");

			fixedColumn.MaxWidth = double.PositiveInfinity;
			Verify.IsTrue(fixedTransform.CanResize, "Lifting MaxWidth must make the column resizable again.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Transform.Resize sets the column width through the resize path and clamps to MinWidth/MaxWidth.")]
	public async Task VerifyTransformResizeSetsWidthAndClampsToBounds()
	{
		TableView tableView = null;
		ITransformProvider transform = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			tableView.Columns[0].MinWidth = 80.0;
			tableView.Columns[0].MaxWidth = 260.0;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			transform = new TableViewColumnHeaderAutomationPeer(tableView, column)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");

			// Height is ignored: a column only resizes along its width. UIA widths are physical pixels.
			transform.Resize(200.0 * tableView.XamlRoot.RasterizationScale, 999.0);
			Verify.AreEqual(GridUnitType.Pixel, column.Width.GridUnitType, "A resize writes a pixel width.");
			Verify.AreEqual(200.0, column.Width.Value, "Resize must set the requested width.");
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			Verify.AreEqual(200.0, column.ActualWidth, "ActualWidth must follow the width Resize wrote.");
			Verify.AreEqual(150.0, tableView.Columns[1].Width.Value, "Resizing one column must not change another fixed column.");

			transform.Resize(1000.0 * tableView.XamlRoot.RasterizationScale, 0.0);
			Verify.AreEqual(260.0, column.Width.Value, "Resize must clamp to MaxWidth.");
			Content.UpdateLayout();

			transform.Resize(10.0 * tableView.XamlRoot.RasterizationScale, 0.0);
			Verify.AreEqual(80.0, column.Width.Value, "Resize must clamp to MinWidth.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Transform.Resize reads its width in physical pixels, the units UIA reports bounds in.")]
	public async Task VerifyTransformResizeWidthIsPhysicalPixels()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, column)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");

			var scale = tableView.XamlRoot.RasterizationScale;
			Log.Comment($"RasterizationScale = {scale}");
			if (scale == 1.0)
			{
				// Not settable from a test, and at 1.0 pixels equal DIPs, so the conversion is unobservable.
				Log.Warning("SKIPPED (reported as passed): the physical-pixel conversion cannot be observed at RasterizationScale 1.0; run on a scaled display to cover it.");
				return;
			}

			transform.Resize(180.0 * scale, 0.0);
			Verify.IsTrue(Math.Abs(column.Width.Value - 180.0) < 0.01,
				$"Resize must convert physical pixels to DIPs; expected 180, got {column.Width.Value}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Transform.Resize converts physical pixels through a scale applied above the table, not only the display scale.")]
	public async Task VerifyTransformResizeAccountsForAncestorScale()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			var scaledHost = new Grid { RenderTransform = new ScaleTransform { ScaleX = 2.0, ScaleY = 2.0 } };
			scaledHost.Children.Add(tableView);
			LoadContent(scaledHost);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, column)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");

			// On screen each DIP of the column spans this many physical pixels.
			var pixelsPerDip = tableView.XamlRoot.RasterizationScale * 2.0;
			transform.Resize(170.0 * pixelsPerDip, 0.0);
			Verify.IsTrue(Math.Abs(column.Width.Value - 170.0) < 0.01,
				$"Resize must convert physical pixels through the ancestor scale; expected 170, got {column.Width.Value}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Transform.Resize applies a width change smaller than the pointer drag's jitter threshold.")]
	public async Task VerifyTransformResizeAppliesSmallWidthChange()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, column)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");
			Verify.AreEqual(150.0, column.ActualWidth, "Precondition: the column starts at 150.");

			// One physical pixel at 300% scale; the pointer drag treats under half a DIP as jitter.
			transform.Resize(150.3 * tableView.XamlRoot.RasterizationScale, 0.0);
			Verify.IsTrue(Math.Abs(column.Width.Value - 150.3) < 0.01,
				$"An absolute UIA width must be applied however small the change; expected 150.3, got {column.Width.Value}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a Transform.Resize below the drag jitter threshold writes Width at most once, lands exactly next to a bound, and leaves an Auto column Auto when it asks for its current width.")]
	[TestProperty("data:Case", "{NearMin, NearMax, AtMin, AutoNoop}")]
	[DataRow("NearMin")]
	[DataRow("NearMax")]
	[DataRow("AtMin")]
	[DataRow("AutoNoop")]
	public async Task VerifyTransformResizeSmallWidthChangeWritesOnce(string caseRow)
	{
		var testCase = caseRow.Trim();
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", testCase == "AutoNoop" ? GridLength.Auto : new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			var first = tableView.Columns[0];
			switch (testCase)
			{
				case "NearMin": first.MinWidth = 100.0; first.Width = new GridLength(100.4, GridUnitType.Pixel); break;
				case "NearMax": first.MaxWidth = 200.0; first.Width = new GridLength(199.6, GridUnitType.Pixel); break;
				case "AtMin": first.MinWidth = 100.0; first.Width = new GridLength(100.0, GridUnitType.Pixel); break;
			}
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, column)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");

			double target = column.ActualWidth + 0.0001;
			double expected = column.ActualWidth;
			switch (testCase)
			{
				case "NearMin": target = expected = 100.1; break;
				case "NearMax": target = expected = 199.9; break;
				case "AtMin": target = 99.7; expected = 100.0; break;
			}
			int expectedWrites = testCase == "NearMin" || testCase == "NearMax" ? 1 : 0;

			int widthWrites = 0;
			var widths = new List<string>();
			column.RegisterPropertyChangedCallback(TableViewColumn.WidthProperty, (s, dp) =>
			{
				widthWrites++;
				widths.Add(column.Width.ToString());
			});

			transform.Resize(target * tableView.XamlRoot.RasterizationScale, 0.0);
			Log.Comment($"{testCase}: Width writes [{string.Join(", ", widths)}], final {column.Width}.");

			Verify.AreEqual(expectedWrites, widthWrites, $"{testCase}: Width must be written {expectedWrites} time(s).");
			if (testCase == "AutoNoop")
			{
				Verify.AreEqual(GridUnitType.Auto, column.Width.GridUnitType, "A request for the current width must leave an Auto column Auto.");
			}
			else
			{
				Verify.IsTrue(Math.Abs(column.Width.Value - expected) < 0.01, $"{testCase}: expected {expected}, got {column.Width.Value}.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Transform.Move and Rotate fail, and Resize rejects a non-finite or negative width.")]
	public async Task VerifyTransformMoveRotateAndInvalidResizeFail()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSizingTestHelpers.CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, column)
				.GetPattern(PatternInterface.Transform) as ITransformProvider;
			Verify.IsNotNull(transform, "A resizable column's header must offer the Transform pattern.");

			VerifyFailsWithHResult(UIA_E_INVALIDOPERATION, () => transform.Move(10.0, 10.0), "Move must fail: CanMove is false.");
			VerifyFailsWithHResult(UIA_E_INVALIDOPERATION, () => transform.Rotate(90.0), "Rotate must fail: CanRotate is false.");
			VerifyFailsWithHResult(E_INVALIDARG, () => transform.Resize(double.NaN, 0.0), "Resize must reject NaN.");
			VerifyFailsWithHResult(E_INVALIDARG, () => transform.Resize(double.PositiveInfinity, 0.0), "Resize must reject infinity.");
			VerifyFailsWithHResult(E_INVALIDARG, () => transform.Resize(-1.0, 0.0), "Resize must reject a negative width.");

			Verify.AreEqual(150.0, column.Width.Value, "A failed Transform call must leave the width untouched.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column header peer is a childless HeaderItem.")]
	public async Task VerifyColumnHeaderPeerIsALeafHeaderItem()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);

			Verify.AreEqual(AutomationControlType.HeaderItem, peer.GetAutomationControlType(),
				"HeaderItem is the UIA control type for table column headers.");

			// The peer's Owner() is the TableView, so a base-class GetChildren() would return the
			// ENTIRE TableView subtree under every column header - the whole grid duplicated once
			// per column in the AT tree. That specific catastrophe is what the override prevents,
			// which makes this assertion worth as much as the control-type one.
			var children = peer.GetChildren();
			Verify.AreEqual(0, children == null ? 0 : children.Count,
				"A column header peer must be a leaf; otherwise the whole TableView subtree appears under every header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies header help text carries a sort state only for columns the control will actually sort.")]
	public async Task VerifyColumnHeaderHelpTextReportsSortStateOnlyForSortableColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserSortColumns = true;
			tableView.Columns[0].CanSort = true;
			tableView.Columns[1].CanSort = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var sortable = tableView.Columns[0];
			var helpTexts = new List<string>();

			foreach (var direction in new[] { SortDirection.None, SortDirection.Ascending, SortDirection.Descending })
			{
				tableView.SortByColumn(sortable, direction);
				tableView.UpdateLayout();

				var text = new TableViewColumnHeaderAutomationPeer(tableView, sortable).GetHelpText();
				Log.Comment($"Help text for SortDirection.{direction}: '{text}'");

				// Asserted as non-empty and pairwise distinct rather than matched against literals:
				// these are localized resources, and pinning English text turns the test into a
				// localization tripwire.
				Verify.IsFalse(string.IsNullOrEmpty(text), $"A sortable column must report a sort state for SortDirection.{direction}.");
				helpTexts.Add(text);
			}

			Verify.AreEqual(3, helpTexts.Distinct().Count(), "Each sort direction must produce a distinct help text.");

			// A column that cannot be sorted must not announce a sort state - that implies an
			// affordance which does not exist. Category 14 owns the tooltip/sort-state combination
			// and its duplicate suppression; there is no tooltip in play here.
			var notSortable = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]).GetHelpText();
			Verify.IsTrue(string.IsNullOrEmpty(notSortable),
				$"A non-sortable column must report no sort state; saw '{notSortable}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the group header peer advertises ExpandCollapse whether or not the group is expandable.")]
	public async Task VerifyGroupHeaderPeerExposesExpandCollapseUnconditionally()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			var peer = GetGroupHeaderPeer(tableView, 0);

			// Spelled out in the IDL itself (TableView.idl:649-655): "ExpandCollapse is
			// unconditional (a non-expandable group reports LeafNode rather than dropping the
			// pattern)", with the in-box Expander / NavigationViewItem precedent named. One of the
			// strongest expectations in this category.
			Verify.IsNotNull(peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider,
				"An expandable group header must advertise ExpandCollapse.");

			header.IsExpandable = false;
			tableView.UpdateLayout();

			var notExpandable = GetGroupHeaderPeer(tableView, 0);
			var provider = notExpandable.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
			if (provider == null)
			{
				Verify.Fail("A non-expandable group header must still advertise ExpandCollapse; a pattern that appears and disappears as data changes forces AT clients to re-query.");
				return;
			}

			Verify.AreEqual(ExpandCollapseState.LeafNode, provider.ExpandCollapseState,
				"A non-expandable group must report LeafNode rather than dropping the pattern.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ExpandCollapseState tracks the group header's IsExpanded.")]
	public async Task VerifyGroupHeaderExpandCollapseStateMatchesIsExpanded()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			header.IsExpandable = true;
			header.IsExpanded = true;
			tableView.UpdateLayout();

			var provider = GetExpandCollapseProvider(tableView, 0);
			if (provider == null)
			{
				return;
			}

			// Narrator announcing "collapsed" for a group whose rows are visible - or the inverse -
			// is the failure this guards.
			Verify.AreEqual(ExpandCollapseState.Expanded, provider.ExpandCollapseState, "An expanded group must report Expanded.");

			header.IsExpanded = false;
			tableView.UpdateLayout();
			Verify.AreEqual(ExpandCollapseState.Collapsed, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"A collapsed group must report Collapsed.");

			header.IsExpandable = false;
			tableView.UpdateLayout();
			Verify.AreEqual(ExpandCollapseState.LeafNode, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"A group that cannot expand must report LeafNode regardless of IsExpanded.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies repeated Expand() and Collapse() calls in one turn are idempotent, not toggles.")]
	public async Task VerifyGroupHeaderExpandAndCollapseAreIdempotent()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = GetExpandCollapseProvider(tableView, 0);
			if (provider == null)
			{
				return;
			}

			// Both calls are made WITHIN ONE UI-thread turn on purpose. The product applies the
			// mutation on a later turn, so a guard reading IsExpanded() cannot make a toggle
			// directional - two Expand() calls in one client turn would queue two toggles and leave
			// the group COLLAPSED, the exact opposite of what was asked. Settling between the calls
			// destroys the condition under test and makes this vacuous.
			provider.Collapse();
			provider.Collapse();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ExpandCollapseState.Collapsed, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"Two Collapse() calls in one turn must leave the group collapsed. Repeated calls are normal AT client behaviour.");

			var provider = GetExpandCollapseProvider(tableView, 0);
			provider.Expand();
			provider.Expand();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ExpandCollapseState.Expanded, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"Two Expand() calls in one turn must leave the group expanded; ExpandCollapsePattern requires Expand/Collapse to be idempotent.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header spans every visible column as a merged cell.")]
	public async Task VerifyGroupHeaderGridItemSpansAllVisibleColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = GetGroupHeaderPeer(tableView, 0);
			var gridItem = peer.GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (gridItem == null)
			{
				Verify.Fail("A group header peer must advertise GridItem; TableView.idl:655-662 declares IGridItemProvider on it.");
				return;
			}

			// A ColumnSpan of 1 would place the band in the first column only, so AT reports a
			// one-cell row where the user sees a full-width band, and the cells to its right appear
			// to belong to a row that has none.
			Verify.AreEqual(0, gridItem.Column, "The band starts at the first column.");
			Verify.AreEqual(1, gridItem.RowSpan, "The band occupies exactly one grid row.");
			Verify.AreEqual(3, gridItem.ColumnSpan, "The band must span every visible column.");

			// Row is the header's index in the flattened row list, so the first group header is the
			// first flattened row.
			Verify.AreEqual(0, gridItem.Row, "The first group header is the first row of the flattened list.");

			tableView.Columns[1].Visibility = Visibility.Collapsed;
			tableView.UpdateLayout();

			// Visible-column basis, consistent with 12.2.
			var narrowed = GetGroupHeaderPeer(tableView, 0).GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (narrowed == null)
			{
				Verify.Fail("The group header peer must still advertise GridItem after a column is collapsed.");
				return;
			}

			Verify.AreEqual(2, narrowed.ColumnSpan,
				"ColumnSpan must fall to the visible column count when a column is collapsed.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header peer's name composes the group's key text and count text.")]
	public async Task VerifyGroupHeaderPeerNameCombinesKeyAndCount()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			var info = header.Content as TableViewGroupInfo;
			if (info == null)
			{
				Verify.Fail("A group header's Content must be the TableViewGroupInfo projection.");
				return;
			}

			// Category 9 owns the VALUES of KeyText / ItemCountText; this owns only their
			// composition, so the expected string is built from the live projection. The name is
			// read from that projection rather than the visual tree, deliberately, so it reflects
			// current state rather than the last render.
			var expected = $"{info.KeyText} {info.ItemCountText}";
			Verify.AreEqual(expected, GetGroupHeaderPeer(tableView, 0).GetName(),
				"The group header name must be the key text and the count text joined by a single space.");

			Verify.IsFalse(string.IsNullOrEmpty(info.KeyText), "Precondition: the group must have key text to announce.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header peer reports the Group control type.")]
	public async Task VerifyGroupHeaderControlTypeIsGroup()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// DataItem would make Narrator read the band as a data row, so the user hears a row that
			// has no cells.
			Verify.AreEqual(AutomationControlType.Group, GetGroupHeaderPeer(tableView, 0).GetAutomationControlType(),
				"The group-header band is a Group, not a DataItem.");
		});
	}

	// ---------- 12.6 Set metadata, names and re-exposure (#11820) ----------
	//
	// Moved here from the interaction tier: each asserts a peer VALUE an in-proc test reads just as
	// well as a client. Authority: TableView-spec.md (api-spec) :884-886 and TableView-dev-spec.md:205-213.

	[TestMethod]
	[TestProperty("Description", "Verifies row peers report 1-based PositionInSet and SizeOfSet over the rows of an ungrouped table.")]
	public async Task VerifyRowPeerPositionInSetAndSizeOfSetAreOneBasedOverRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// api-spec:884 - "Rows report PositionInSet / SizeOfSet". UIA set metadata is 1-based and
			// dev-spec:213 reserves 0 for "cannot be resolved", which a realized flat row never is.
			for (int i = 0; i < 3; i++)
			{
				var peer = GetRowPeer(GetProjectedRow(tableView, i));
				Verify.AreEqual(i + 1, peer.GetPositionInSet(), $"Row {i} must report PositionInSet {i + 1}.");
				Verify.AreEqual(3, peer.GetSizeOfSet(), $"Row {i} must report SizeOfSet 3.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies grouped row peers report PositionInSet and SizeOfSet relative to their own group, excluding header bands.")]
	public async Task VerifyGroupedRowPeerPositionInSetIsRelativeToItsGroup()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTwoGroupAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// api-spec:884 - "When the source is grouped, both values are relative to the containing group and
			// exclude the group-header bands, matching ItemsControlAutomationPeer". Owner confirmed within-group
			// announcement; the #11820 PR's "flat" wording is superseded.
			var groups = new List<List<TableViewRow>>();
			foreach (var element in GetProjectedElements(tableView))
			{
				if (element is TableViewGroupHeader)
				{
					groups.Add(new List<TableViewRow>());
				}
				else if (element is TableViewRow row)
				{
					if (groups.Count == 0)
					{
						Verify.Fail("A data row was projected before any group header.");
						return;
					}
					groups[groups.Count - 1].Add(row);
				}
			}

			var sizes = groups.Select(g => g.Count).OrderBy(c => c).ToList();
			Verify.AreEqual("2,3", string.Join(",", sizes), "Precondition: the fixture groups five rows as 2 Designers and 3 Engineers.");

			foreach (var group in groups)
			{
				for (int i = 0; i < group.Count; i++)
				{
					var peer = GetRowPeer(group[i]);
					Verify.AreEqual(i + 1, peer.GetPositionInSet(),
						$"Row {i} of a {group.Count}-row group must report PositionInSet {i + 1}, relative to its group.");
					Verify.AreEqual(group.Count, peer.GetSizeOfSet(),
						$"A row in a {group.Count}-row group must report SizeOfSet {group.Count}, excluding header bands.");
				}
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies app-set AutomationProperties.PositionInSet and SizeOfSet on a row win over the computed values.")]
	public async Task VerifyAppSetPositionInSetAndSizeOfSetWinOnRowPeer()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// api-spec:884 / dev-spec:213 - "an app-set AutomationProperties value always wins".
			var row = GetProjectedRow(tableView, 0);
			AutomationProperties.SetPositionInSet(row, 7);
			AutomationProperties.SetSizeOfSet(row, 42);

			var peer = GetRowPeer(row);
			Verify.AreEqual(7, peer.GetPositionInSet(), "An app-set PositionInSet must win over the computed one.");
			Verify.AreEqual(42, peer.GetSizeOfSet(), "An app-set SizeOfSet must win over the computed one.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a row peer's name is composed from its visible cells only, and an explicit AutomationProperties.Name wins.")]
	public async Task VerifyRowPeerNameComposesVisibleCellTextAndExplicitNameWins()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// api-spec:884 - rows "compose their name from their visible cells"; api-spec:886 - explicit Name wins.
			// Containment, not equality: the separator is a localized resource.
			var composed = GetRowPeer(GetProjectedRow(tableView, 0)).GetName();
			Log.Comment($"Composed row name: '{composed}'.");
			Verify.IsTrue(composed.Contains("Asha"), "The row name must include the Name cell's text.");
			Verify.IsTrue(composed.Contains("Designer"), "The row name must include the Role cell's text.");

			tableView.Columns[1].Visibility = Visibility.Collapsed;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var narrowed = GetRowPeer(GetProjectedRow(tableView, 0)).GetName();
			Log.Comment($"Row name with Role collapsed: '{narrowed}'.");
			Verify.IsTrue(narrowed.Contains("Asha"), "The row name must still include visible cells.");
			Verify.IsFalse(narrowed.Contains("Designer"), "The row name must not include a collapsed column's text.");

			var row = GetProjectedRow(tableView, 0);
			AutomationProperties.SetName(row, "Authored row name");
			Verify.AreEqual("Authored row name", GetRowPeer(row).GetName(), "An explicit AutomationProperties.Name must win outright.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a single-level group header peer computes no Level (reports 0) and an app-set AutomationProperties.Level wins.")]
	public async Task VerifyGroupHeaderPeerReportsNoLevelAndAppSetLevelWins()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Owner decision, recorded in the dev-spec accessibility list: the group header peer computes NO Level
			// for single-level grouping, so it reports 0 ("not specified"). dev-spec:213 - an app-set value wins.
			var computed = GetGroupHeaderPeer(tableView, 0).GetLevel();
			Verify.AreEqual(0, computed, "A single-level group header must not announce a Level; it must report 0 ('not specified').");

			AutomationProperties.SetLevel(GetGroupHeader(tableView, 0), 3);
			Verify.AreEqual(3, GetGroupHeaderPeer(tableView, 0).GetLevel(), "An app-set Level must win over the computed one.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies every data row (with its cell children) is reachable from the TableView peer again after a group is collapsed and expanded.")]
	public async Task VerifyGroupRowsAreReexposedAfterCollapseAndExpand()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTwoGroupAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var before = CollectRowPeers(RequireTablePeer(tableView));
			Verify.AreEqual(5, before.Count, "Precondition: all five data rows are reachable from the table peer before the collapse.");
			GetExpandCollapseProvider(tableView, 0).Collapse();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ExpandCollapseState.Collapsed, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"Precondition: the first group must report Collapsed.");
			GetExpandCollapseProvider(tableView, 0).Expand();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// dev-spec:205 - a row peer exposes its cell peers as children; api-spec:894 - tree enumeration connects
			// each cell to its row. The #11820 defect: cached peers outlived their containers, so the re-expanded
			// group's rows were absent from the tree.
			var after = CollectRowPeers(RequireTablePeer(tableView));
			Verify.AreEqual(5, after.Count, "After collapse and expand, every data row must be reachable from the table peer again.");

			foreach (var rowPeer in after)
			{
				var children = rowPeer.GetChildren();
				Verify.AreEqual(3, children?.Count ?? 0, $"Re-exposed row '{rowPeer.GetName()}' must expose one cell per visible column.");
			}
		});
	}

	// ---------- 12.5 Primitive peers ----------
	//
	// Both primitives are MUX_INTERNAL but are projected into the test app -
	// TableView_Sizing_APITests.cs already drives ResizeGripper directly. These assert through the
	// base AutomationPeer members, which need no cast to the peer type.

	[TestMethod]
	[TestProperty("Description", "Verifies the SortIndicator peer is decorative and excluded from both the control and content views.")]
	public async Task VerifySortIndicatorPeerIsDecorativeAndExcludedFromBothViews()
	{
		SortIndicator indicator = null;

		RunOnUIThread.Execute(() =>
		{
			indicator = new SortIndicator { Direction = SortIndicatorDirection.Ascending };
			LoadContent(indicator);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(indicator);
			if (peer == null)
			{
				Verify.Fail("A SortIndicator must produce an automation peer.");
				return;
			}

			// The owning header announces the sort state in its help text. If the glyph were also a
			// control or content element, Narrator would stop on it and the user would hear the sort
			// state twice - once from the header and once from a stray image.
			Verify.IsFalse(peer.IsControlElement(), "The sort glyph must be excluded from the control view.");
			Verify.IsFalse(peer.IsContentElement(), "The sort glyph must be excluded from the content view.");
			Verify.AreEqual(AutomationControlType.Image, peer.GetAutomationControlType(), "The sort glyph is an Image.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ResizeGripper peer composes its owner's header text into its name.")]
	public async Task VerifyResizeGripperPeerNameIncludesOwningHeaderText()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			var bare = peer.GetName();
			Log.Comment($"Gripper name with no OwnerName: '{bare}'");
			Verify.IsFalse(string.IsNullOrEmpty(bare), "A gripper must always identify itself, even with no owner text.");

			gripper.OwnerName = "Price";
			gripper.UpdateLayout();

			var composed = FrameworkElementAutomationPeer.CreatePeerForElement(gripper).GetName();
			Log.Comment($"Gripper name with OwnerName 'Price': '{composed}'");

			// Containment rather than an exact string: the format comes from a localized resource
			// with a hardcoded fallback, so pinning either form is a localization tripwire. Without
			// the owner text every gripper in the table announces identically and a user moving
			// across the header row cannot tell which column they are about to resize.
			Verify.IsTrue(composed.Contains("Price"), $"The gripper name must carry its owner's header text; saw '{composed}'.");
			Verify.IsTrue(composed.Contains(bare), $"The gripper name must still carry the localized gripper name; saw '{composed}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ResizeGripper peer never sources its name from Tag or from an ancestor.")]
	public async Task VerifyResizeGripperPeerNameIgnoresTagAndAncestors()
	{
		ResizeGripper gripper = null;
		string bare = null;

		RunOnUIThread.Execute(() =>
		{
			var host = new Grid();
			AutomationProperties.SetName(host, "AncestorDecoy");

			gripper = new ResizeGripper { Width = 8, Height = 24, Tag = "TagDecoy" };
			host.Children.Add(gripper);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			bare = peer.GetName();
			Log.Comment($"Gripper name inside a named ancestor, with a Tag decoy: '{bare}'");

			// The product states the sourcing rule as an invariant: read from the declared OwnerName
			// property, never from Tag, never from an ancestor. Tag is a general-purpose slot a host
			// may already be using, which is what makes the decoy realistic - and the product calls
			// the failure out as "a silent wrong name is an accessibility bug". Silent is the
			// operative word: nothing else would catch it.
			Verify.IsFalse(bare.Contains("TagDecoy"), $"The gripper must not publish its Tag as a name; saw '{bare}'.");
			Verify.IsFalse(bare.Contains("AncestorDecoy"), $"The gripper must not publish an ancestor's name; saw '{bare}'.");
			Verify.IsFalse(string.IsNullOrEmpty(bare), "The gripper must still report its own localized name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ResizeGripper peer reports Thumb and an empty automation id.")]
	public async Task VerifyResizeGripperPeerControlTypeAndEmptyAutomationId()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			// Both choices are stated as deliberate in the product with their rationale, which makes
			// them invariants rather than incidental. Slider would make AT offer value and range
			// operations the gripper does not have: it reports drag distance and owns no value.
			Verify.AreEqual(AutomationControlType.Thumb, peer.GetAutomationControlType(), "The gripper is a Thumb, not a Slider.");

			// A constant id would be duplicated across every column's gripper, violating the
			// requirement that automation ids be unique among siblings.
			Verify.AreEqual(string.Empty, peer.GetAutomationId(), "With nothing authored the gripper's automation id must be empty.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an authored AutomationProperties.Name on a gripper is used verbatim, with no owner text composed in.")]
	public async Task VerifyAuthoredAutomationNameWinsOnGripper()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24, OwnerName = "Price" };
			AutomationProperties.SetName(gripper, "Column splitter");

			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			// An app that names its grippers must not get a corrupted name - its text with the
			// framework's appended to it - and must have a way to opt out of the composition.
			Verify.AreEqual("Column splitter", peer.GetName(), "An authored automation name must win outright.");
		});
	}

	#region Hierarchical rows

	// Tree fixture, ParentBy(Id, ManagerId): Ada > { Ben > { Dan }, Cy }, Eve > { Fay }, Gus.

	[TestMethod]
	[TestProperty("Description", "Verifies a row of a flat projection exposes no ExpandCollapse pattern, including after ClearParentBy.")]
	public async Task VerifyFlatRowHasNoExpandCollapse()
	{
		TableView flatTable = null;

		RunOnUIThread.Execute(() =>
		{
			flatTable = CreateTableView();
			LoadContent(flatTable);
		});

		await SettleLayout(flatTable);

		RunOnUIThread.Execute(() =>
		{
			var row = GetProjectedRow(flatTable, 0);
			Verify.AreEqual(0, row.Level, "A flat row must report Level 0.");
			Verify.IsNull(GetRowPeer(row).GetPattern(PatternInterface.ExpandCollapse),
				"A flat row must not expose ExpandCollapse, or AT announces it as collapsed.");
		});

		TableViewSource source = null;
		var tableView = await PlaceTreeTable(() => source = TableViewSource.From(TableViewTreeTestHelpers.MakeTree())
			.ParentBy(TableViewTreeTestHelpers.ById, TableViewTreeTestHelpers.ByManager));

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(TableViewTreeTestHelpers.GetRowExpandCollapse(TableViewTreeTestHelpers.FindTreeRow(tableView, "Ada")),
				"Precondition: a tree row exposes ExpandCollapse.");
			source.ClearParentBy();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(TableViewTreeTestHelpers.GetRowExpandCollapse(TableViewTreeTestHelpers.FindTreeRow(tableView, "Ada")),
				"Once the relation is cleared the row is flat and must drop ExpandCollapse.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a tree row's ExpandCollapse state matches its node, and Expand() grows the projection.")]
	public async Task VerifyExpandCollapseStates()
	{
		var tableView = await PlaceTreeTable(() => TableViewSource.From(TableViewTreeTestHelpers.MakeTree())
			.ParentBy(TableViewTreeTestHelpers.ById, TableViewTreeTestHelpers.ByManager));
		var countBefore = 0;

		RunOnUIThread.Execute(() =>
		{
			countBefore = GetProjectedCount(tableView);
			Verify.AreEqual(3, countBefore, "Precondition: only the roots Ada, Eve and Gus are projected.");
			Verify.AreEqual("Collapsed", TreeRowState(tableView, "Ada"), "Ada has children and starts collapsed.");
			Verify.AreEqual("LeafNode", TreeRowState(tableView, "Gus"), "Gus has no children.");

			TableViewTreeTestHelpers.GetRowExpandCollapse(TableViewTreeTestHelpers.FindTreeRow(tableView, "Ada")).Expand();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Expanded", TreeRowState(tableView, "Ada"), "Expand() must report Expanded.");
			Verify.AreEqual(countBefore + 2, GetProjectedCount(tableView), "Expanding Ada must project Ben and Cy.");
			Verify.AreEqual("Collapsed", TreeRowState(tableView, "Ben"), "Ben has a child and starts collapsed.");
			Verify.AreEqual("LeafNode", TreeRowState(tableView, "Cy"), "Cy has no children.");
			Verify.AreEqual("LeafNode", TreeRowState(tableView, "Gus"), "Gus has no children.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies tree row peers report Level, PositionInSet and SizeOfSet relative to their sibling set.")]
	public async Task VerifyLevelPositionSizeUngrouped()
	{
		var tableView = await PlaceTreeTable(() => TableViewSource.From(TableViewTreeTestHelpers.MakeTree())
			.ParentBy(TableViewTreeTestHelpers.ById, TableViewTreeTestHelpers.ByManager));

		RunOnUIThread.Execute(() => TableViewTreeTestHelpers.ToggleTreeRow(tableView, "Ada", expand: true));
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var levels = string.Join(" ", GetProjectedElements(tableView).OfType<TableViewRow>()
				.Select(r => $"{TableViewTreeTestHelpers.NameOf(r.DataContext)}:{GetRowPeer(r).GetLevel()}"));
			Verify.AreEqual("Ada:1 Ben:2 Cy:2 Eve:1 Gus:1", levels, "Peer Level must be the 1-based tree depth.");
			Verify.AreEqual("Ada 1/3 Ben 1/2 Cy 2/2 Eve 2/3 Gus 3/3", TableViewTreeTestHelpers.PositionLabels(tableView),
				"PositionInSet and SizeOfSet must count siblings, not projected rows.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies grouped tree root peers report PositionInSet and SizeOfSet scoped to their group.")]
	public async Task VerifyPositionInSetGrouped()
	{
		var tableView = await PlaceTreeTable(() => TableViewSource.From(TableViewTreeTestHelpers.MakeTree())
			.ParentBy(TableViewTreeTestHelpers.ById, TableViewTreeTestHelpers.ByManager)
			.GroupBy(TableViewTreeTestHelpers.EmpKey(e => e.Dept)));

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Ada 1/1 Eve 1/2 Gus 2/2", TableViewTreeTestHelpers.PositionLabels(tableView),
				"Root sibling sets must be scoped by group.");
		});
	}

	private async Task<TableView> PlaceTreeTable(Func<TableViewSource> makeSource)
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			tableView = TableViewTreeTestHelpers.CreateTreeTable(makeSource());
			LoadContent(tableView);
		});

		await SettleLayout(tableView);
		return tableView;
	}

	// The row's ExpandCollapseState as text, or "none" when the pattern is absent.
	private static string TreeRowState(TableView tableView, string name)
	{
		var provider = TableViewTreeTestHelpers.GetRowExpandCollapse(TableViewTreeTestHelpers.FindTreeRow(tableView, name));
		return provider == null ? "none" : provider.ExpandCollapseState.ToString();
	}

	#endregion

	#region Teardown

	[TestMethod]
	[TestProperty("Description", "Verifies a keyboard-focused header cell's peer still answers after its TableView leaves the tree.")]
	public async Task VerifyFocusedHeaderPeerAnswersAfterTableLeavesTree()
	{
		TableView tableView = null;
		StackPanel host = null;
		FrameworkElement headerCell = null;
		AutomationPeer headerPeer = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			host = new StackPanel();
			host.Children.Add(new Button { Content = "Sink" });
			host.Children.Add(tableView);
			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			headerCell = GetHeaderCell(tableView, 1);
			Verify.IsTrue(headerCell.Focus(FocusState.Keyboard), "Precondition: header cell 1 takes keyboard focus.");
			headerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(headerCell);
			Verify.IsNotNull(headerPeer, "Precondition: the header cell has a peer.");
			Verify.AreEqual("Role", headerPeer.GetName(), "Precondition: the live header peer is named by its column.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => host.Children.Remove(tableView));

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Exception error = null;
			try
			{
				Log.Comment($"Detached header peer name: '{headerPeer.GetName()}'");
				Verify.AreEqual(AutomationControlType.HeaderItem, headerPeer.GetAutomationControlType(),
					"A detached header peer must still report its control type.");
				Log.Comment($"Detached header peer has keyboard focus: {headerPeer.HasKeyboardFocus()}");
			}
			catch (Exception e)
			{
				error = e;
			}

			Verify.IsTrue(error == null, $"Querying a detached header peer must not throw. Instead: {error?.Message}");
		});
	}

	#endregion
}

internal static class TableViewAutomationTestHelpers
{
	// Three text columns over distinct properties, so a composed cell name is unambiguous about
	// which column it came from.
	internal static TableView CreateAutomationTable() => CreateAutomationTable(MakeItems());

	internal static TableView CreateAutomationTable(List<Person> items)
		=> AddAutomationColumns(CreateTableViewShell(items, 600, 300));

	// The same three columns over a source grouped by Role, so group headers realize.
	internal static TableView CreateGroupedAutomationTable()
	{
		var source = TableViewSource.From(MakeItems());
		source.GroupBy(item => (object)((Person)item).Role);

		return AddAutomationColumns(CreateTableViewShell(source, 600, 400));
	}

	private static TableView AddAutomationColumns(TableView tableView)
	{
		tableView.Columns.Add(MakeTextColumn("Name"));
		tableView.Columns.Add(MakeTextColumn("Role"));
		tableView.Columns.Add(MakeTextColumn("Alias", "Name"));

		return tableView;
	}

	// Five rows grouped by Role into a 2-row and a 3-row group, so group-relative set metadata is distinguishable
	// from flat metadata.
	internal static TableView CreateTwoGroupAutomationTable()
	{
		var source = TableViewSource.From(new List<Person>
		{
			new Person { Name = "Asha", Role = "Designer" },
			new Person { Name = "Bo", Role = "Designer" },
			new Person { Name = "Diego", Role = "Engineer" },
			new Person { Name = "Eli", Role = "Engineer" },
			new Person { Name = "Fay", Role = "Engineer" },
		});
		source.GroupBy(item => (object)((Person)item).Role);

		return AddAutomationColumns(CreateTableViewShell(source, 600, 500));
	}

	// Every row peer reachable from root, without descending into a row (its children are cells).
	internal static List<TableViewRowAutomationPeer> CollectRowPeers(AutomationPeer root)
	{
		var found = new List<TableViewRowAutomationPeer>();
		var children = root.GetChildren();
		if (children == null)
		{
			return found;
		}

		foreach (var child in children)
		{
			if (child is TableViewRowAutomationPeer rowPeer)
			{
				found.Add(rowPeer);
			}
			else
			{
				found.AddRange(CollectRowPeers(child));
			}
		}

		return found;
	}

	internal static TableViewAutomationPeer RequireTablePeer(TableView tableView)
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(tableView) as TableViewAutomationPeer;
		Verify.IsTrue(peer != null, "A TableView must produce a TableViewAutomationPeer.");
		return peer;
	}

	internal static IGridProvider RequireGridProvider(TableView tableView)
	{
		var provider = RequireTablePeer(tableView).GetPattern(PatternInterface.Grid) as IGridProvider;
		Verify.IsTrue(provider != null, "The TableView peer must advertise IGridProvider.");
		return provider;
	}

	internal static TableViewRowAutomationPeer RequireRowPeer(TableView tableView, int rowIndex)
	{
		var rows = GetRealizedRows(tableView);
		Verify.IsTrue(rows.Count > rowIndex, $"The test needs a realized row at index {rowIndex}; saw {rows.Count} rows.");
		return GetRowPeer(rows[rowIndex]);
	}

	internal static ISelectionItemProvider RequireSelectionItemProvider(TableView tableView, int rowIndex)
	{
		var peer = RequireRowPeer(tableView, rowIndex);
		var provider = peer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
		Verify.IsTrue(provider != null, $"Row {rowIndex}'s peer must advertise SelectionItem for this test to drive selection.");
		return provider;
	}

	internal static List<FrameworkElement> GetHeaderCells(TableView tableView)
		=> GetHeaderHost(tableView).Children
			.OfType<FrameworkElement>()
			.Where(child => child.Tag is TableViewColumn)
			.ToList();
}

// A plain managed header object: CsWinRT gives it IStringable, whose ToString is the type name.
internal sealed class TitledColumnHeader
{
	public string Title { get; set; }
}
