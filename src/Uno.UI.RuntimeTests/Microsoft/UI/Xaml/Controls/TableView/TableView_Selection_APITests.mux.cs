// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Selection_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewSelectionTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 6.1 of the TableView test plan: selection mode gating.
//
// Spec basis: TableView.idl:536 - SelectionMode is "On by default, matching ItemsView, ListView and
// WPF's DataGrid. None = display-only", and :20 defines None = 0. "Display-only" is the whole
// contract: in None a table shows data and nothing else.
//
// NOT IMPLEMENTED HERE:
//   VerifySelectionDefaults       - TableViewTests.cs:132-134 already asserts Single / -1 / null on
//                                   a fresh control.
//   VerifySelectionModeRoundtrip  - TableViewTests.cs:326-328 runs SelectionMode through
//                                   VerifySettableDependencyProperty, which also covers the DP path
//                                   and is therefore strictly stronger.
//   VerifySwitchingModeAfterLoadIsHonored - not a separable behavior. Both tests below change the
//                                   mode after load, so a test asserting only "the change took
//                                   effect" has no failure mode of its own.
[TestClass]
public class TableViewSelectionModeTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies Select is a no-op while SelectionMode is None, leaving both projections and all row chrome unselected.")]
	public async Task VerifySelectionModeNoneIgnoresSelectCall()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			tableView.SelectionMode = TableViewSelectionMode.None;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);

			// "None = display-only" (TableView.idl:536). A mode that does not gate the primary
			// selection entry point has no meaning.
			Verify.AreEqual(-1, tableView.SelectedIndex,
				"Select must not move SelectedIndex while SelectionMode is None.");
			Verify.IsNull(tableView.SelectedItem,
				"Select must not set SelectedItem while SelectionMode is None.");
			Verify.IsFalse(tableView.IsSelected(1),
				"IsSelected must stay false for an index Select was rejected for.");

			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.IsFalse(row.IsSelected,
					"No row may render selected chrome in a display-only table.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies switching SelectionMode to None clears an existing selection and its row chrome.")]
	public async Task VerifySelectionModeNoneClearsExistingSelection()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected in Single mode.");

			tableView.SelectionMode = TableViewSelectionMode.None;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Display-only is unachievable if a stale selection survives the switch. SelectionMode
			// carries [MUX_PROPERTY_CHANGED_CALLBACK(TRUE)] in the IDL, confirming the mode change
			// is meant to do something rather than only gate future calls.
			Verify.AreEqual(-1, tableView.SelectedIndex,
				"Switching to None must clear SelectedIndex.");
			Verify.IsNull(tableView.SelectedItem,
				"Switching to None must clear SelectedItem.");

			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.IsFalse(row.IsSelected,
					"A row must not stay lit after selection is turned off - there would be no API left to clear it.");
			}
		});
	}
}

// Category 6.2 of the TableView test plan: the single-selection API.
//
// Spec basis: TableView.idl:531-552 - "Row-scoped, single item. SelectedItem and SelectedIndex are
// read-only projections of the selection and stay coherent with each other. Drive selection through
// Select / Deselect / DeselectAll, matching ItemsView." Also :541 "The selected data item, or null"
// and :544 "The selected item's index, or -1 when nothing is selected".
[TestClass]
public class TableViewSingleSelectionTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies Select(index) sets SelectedIndex and SelectedItem coherently, and that SelectedItem is the app's own object.")]
	public async Task VerifySelectByIndexUpdatesSelectedItemAndIndex()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);

			Verify.AreEqual(1, tableView.SelectedIndex, "Select(1) should set SelectedIndex to 1.");

			// Reference identity, not equality. "The selected data item" (TableView.idl:541) means
			// the app's own object; a projection that hands back an internal wrapper is not that
			// item, and an app comparing against its own collection would find no match.
			Verify.AreSame(items[1], tableView.SelectedItem,
				"SelectedItem must be the very object the app put in the source, not a copy or wrapper.");

			Verify.IsTrue(tableView.IsSelected(1), "IsSelected must agree with SelectedIndex.");
			Verify.IsFalse(tableView.IsSelected(0), "An unselected index must report false.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that in Single mode selecting another index moves the selection instead of accumulating it.")]
	public async Task VerifySelectingSecondIndexClearsFirst()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(0);
			Verify.AreEqual(0, tableView.SelectedIndex, "Precondition: row 0 must be selected.");

			tableView.Select(2);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// "Row-scoped, single item" plus a scalar SelectedIndex/SelectedItem surface admits no
			// other reading: two rows lit at once could never both be reported.
			Verify.AreEqual(2, tableView.SelectedIndex, "The selection should have moved to index 2.");
			Verify.AreSame(items[2], tableView.SelectedItem, "SelectedItem should follow the new index.");
			Verify.IsFalse(tableView.IsSelected(0), "The previous selection must be cleared in Single mode.");

			VerifyExactlyOneRowSelected(tableView, items, 2, "after moving the selection");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Deselect only clears when the supplied index is the selected one.")]
	public async Task VerifyDeselectClearsOnlyMatchingIndex()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected.");

			// An index parameter that does not scope the operation would be meaningless, and
			// DeselectAll() already exists for the unscoped case.
			tableView.Deselect(0);

			Verify.AreEqual(1, tableView.SelectedIndex,
				"Deselecting an index that is not selected must leave the selection untouched.");
			Verify.AreSame(items[1], tableView.SelectedItem,
				"A non-matching Deselect must not disturb SelectedItem.");

			tableView.Deselect(1);

			Verify.AreEqual(-1, tableView.SelectedIndex, "Deselecting the selected index should clear it.");
			Verify.IsNull(tableView.SelectedItem, "SelectedItem should be null once nothing is selected.");
			Verify.IsFalse(tableView.IsSelected(1), "IsSelected should be false after Deselect.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies DeselectAll clears the selection and that a redundant second call changes nothing and raises no event.")]
	public async Task VerifyDeselectAllClearsAndIsIdempotent()
	{
		TableView tableView = null;
		List<Person> items = null;
		var raiseCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected.");

			tableView.SelectionChanged += (sender, args) => raiseCount++;

			tableView.DeselectAll();

			Verify.AreEqual(-1, tableView.SelectedIndex, "DeselectAll should clear SelectedIndex.");
			Verify.IsNull(tableView.SelectedItem, "DeselectAll should clear SelectedItem.");
			Verify.AreEqual(1, raiseCount, "The clearing DeselectAll should raise SelectionChanged exactly once.");

			tableView.DeselectAll();

			Verify.AreEqual(-1, tableView.SelectedIndex, "A second DeselectAll should leave the empty state alone.");
			Verify.IsNull(tableView.SelectedItem, "A second DeselectAll should leave SelectedItem null.");
			Verify.AreEqual(1, raiseCount,
				"A redundant DeselectAll must not raise SelectionChanged - nothing changed.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IsSelected is true for exactly the selected index and agrees with the other two projections.")]
	public async Task VerifyIsSelectedReflectsSelection()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);

			// IsSelected is a third projection of the same selection; the three public ways of
			// asking "is this row selected" must not be able to disagree.
			for (var index = 0; index < items.Count; index++)
			{
				Verify.AreEqual(index == 1, tableView.IsSelected(index),
					$"IsSelected({index}) should be true only for the selected index.");
			}

			VerifySelectionIsCoherent(tableView, items, "with row 1 selected");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies invalid indices passed to IsSelected, Select and Deselect never throw and always leave the selection coherent.")]
	public async Task VerifyInvalidIndicesAreSafeAndLeaveSelectionCoherent()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Indices past the end. These must not disturb an existing selection: Select is declared
			// as a selecting operation, DeselectAll() already covers clearing, and a call that
			// cannot select anything should not be able to unselect something.
			var pastEnd = new[] { items.Count, items.Count + 1, int.MaxValue };

			foreach (var index in pastEnd)
			{
				tableView.Select(1);
				Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected.");

				Verify.IsFalse(tableView.IsSelected(index),
					$"IsSelected({index}) should be false for an index past the end.");

				tableView.Select(index);
				Verify.AreEqual(1, tableView.SelectedIndex,
					$"Select({index}) cannot select anything, so it must not clear the existing selection.");
				VerifySelectionIsCoherent(tableView, items, $"after Select({index})");

				tableView.Deselect(index);
				Verify.AreEqual(1, tableView.SelectedIndex,
					$"Deselect({index}) targets no selected row, so it must leave the selection alone.");
				VerifySelectionIsCoherent(tableView, items, $"after Deselect({index})");
			}

			// Negative indices. The outcome of Select with a negative index is deliberately NOT
			// pinned - "select nothing" and "reject" are equally defensible and the IDL picks
			// neither. Only "does not throw" and coherence are asserted, both of which are stated.
			var negative = new[] { -1, -2, int.MinValue };

			foreach (var index in negative)
			{
				tableView.Select(1);
				Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected.");

				Verify.IsFalse(tableView.IsSelected(index),
					$"IsSelected({index}) should be false for a negative index.");

				tableView.Select(index);
				VerifySelectionIsCoherent(tableView, items, $"after Select({index})");

				tableView.Deselect(index);
				VerifySelectionIsCoherent(tableView, items, $"after Deselect({index})");
			}
		});
	}
}

// Category 6.3 of the TableView test plan: selection across source mutation.
//
// Spec basis: the coherence invariant at TableView.idl:532-534 plus :541 "The selected data item".
// The IDL says nothing more specific about collection changes, so these tests are deliberately
// scoped to what it does state. Two of them were renamed from the backlog because the original
// names asserted an outcome the IDL never picks - see the test plan.
[TestClass]
public class TableViewSelectionSourceMutationTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies the selection is anchored to the item, so inserting and removing above it shifts SelectedIndex without changing SelectedItem.")]
	public async Task VerifySelectionTracksItemAcrossInsertAndRemoveAbove()
	{
		TableView tableView = null;
		ObservableCollection<Person> items = null;
		Person selected = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<Person>(MakeManyItems(8));
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(2);
			selected = items[2];
			Verify.AreSame(selected, tableView.SelectedItem, "Precondition: index 2 must be selected.");

			items.Insert(0, new Person { Name = "Inserted", Role = "Inserted" });
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// SelectedItem and SelectedIndex are "read-only projections of the selection" - the
			// selection is the primary thing and the index is derived from it, so an insert above
			// must move the index rather than the selection.
			Verify.AreSame(selected, tableView.SelectedItem,
				"Inserting above the selection must not move the selection to a different record.");
			Verify.AreEqual(3, tableView.SelectedIndex,
				"SelectedIndex should shift down by one to keep pointing at the same item.");
			Verify.AreEqual(items.IndexOf(selected), tableView.SelectedIndex,
				"SelectedIndex must agree with the item's real position in the collection.");

			items.RemoveAt(0);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(selected, tableView.SelectedItem,
				"Removing above the selection must not move the selection to a different record.");
			Verify.AreEqual(2, tableView.SelectedIndex, "SelectedIndex should shift back up by one.");
			Verify.AreEqual(items.IndexOf(selected), tableView.SelectedIndex,
				"SelectedIndex must agree with the item's real position in the collection.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing the selected item never leaves SelectedItem referencing an object that is no longer in the source.")]
	public async Task VerifyRemovingSelectedItemClearsSelection()
	{
		TableView tableView = null;
		ObservableCollection<Person> items = null;
		Person removed = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<Person>(MakeManyItems(8));
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(2);
			removed = items[2];
			Verify.AreSame(removed, tableView.SelectedItem, "Precondition: index 2 must be selected.");

			items.RemoveAt(2);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// An object that is not in the source cannot be "the selected data item". Deliberately
			// does not assert WHAT it becomes - clearing and re-anchoring to the item that shifted
			// into the slot are both defensible, and the IDL picks neither.
			Verify.IsFalse(ReferenceEquals(removed, tableView.SelectedItem),
				"SelectedItem must not keep referencing an item that was removed from the source.");

			VerifySelectionIsCoherent(tableView, items, "after removing the selected item");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies selection stays coherent across an ItemsSource swap and never survives into the new dataset as a stale item.")]
	public async Task VerifySelectionRemainsCoherentAfterItemsSourceSwap()
	{
		TableView tableView = null;
		List<Person> original = null;
		List<Person> replacement = null;

		RunOnUIThread.Execute(() =>
		{
			original = MakeManyItems(8);
			tableView = CreateSelectionTable(original);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(2);
			Verify.AreSame(original[2], tableView.SelectedItem, "Precondition: index 2 must be selected.");

			replacement = MakeManyItems(5);
			tableView.ItemsSource = replacement;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Scoped to the invariant that rules out the real bug - "stale selection after refresh",
			// where SelectedItem points into the previous page of data. Passes whether the control
			// clears on swap or re-anchors by identity.
			foreach (var stale in original)
			{
				Verify.IsFalse(ReferenceEquals(stale, tableView.SelectedItem),
					"SelectedItem must never be an object from the source that was replaced.");
			}

			VerifySelectionIsCoherent(tableView, replacement, "after swapping ItemsSource");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies selection stays coherent across a collection Reset, and that an item which did not survive the reset is not left selected.")]
	public async Task VerifySelectionRemainsCoherentAfterCollectionReset()
	{
		TableView tableView = null;
		ResettableCollection<Person> items = null;
		List<Person> survivors = null;
		Person selected = null;

		RunOnUIThread.Execute(() =>
		{
			survivors = MakeManyItems(8);
			items = new ResettableCollection<Person>(survivors);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(2);
			selected = survivors[2];
			Verify.AreSame(selected, tableView.SelectedItem, "Precondition: index 2 must be selected.");

			// ReplaceAll, not Clear()+Add(): the latter raises its Reset while the collection is
			// EMPTY, so the selected item is genuinely absent at the moment the reset is handled and
			// the "did it survive?" question cannot even be asked. ReplaceAll is the refresh/re-query
			// shape - one Reset, new contents already in place - and is the only one that exercises
			// this path. Reordered here so the item survives but its index moves.
			var reordered = survivors.AsEnumerable().Reverse().ToList();
			items.ReplaceAll(reordered);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// A Reset destroys index information but not item identity, so re-anchoring a
			// still-present item is a legitimate design and "clears" is deliberately NOT asserted.
			// Coherence is the part the IDL does state. If the selection did survive, it must have
			// followed the item to its new index rather than staying on the old one.
			VerifySelectionIsCoherent(tableView, items, "after a reset that preserved the selected item");

			if (tableView.SelectedItem != null)
			{
				Verify.AreSame(selected, tableView.SelectedItem,
					"A reset that preserved the selected item must not silently move the selection to a different record.");
				Verify.AreEqual(items.IndexOf(selected), tableView.SelectedIndex,
					"A surviving selection must follow its item to the item's new position.");
			}

			// Reset #2: the selected object is gone. This half is the strong one - a vanished item
			// is not a data item and must not remain selected.
			items.ReplaceAll(MakeManyItems(6));
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(ReferenceEquals(selected, tableView.SelectedItem),
				"An item that did not survive the reset must not stay selected - it is no longer in the data.");

			VerifySelectionIsCoherent(tableView, items, "after a reset that dropped the selected item");
		});
	}
}

// Category 6.4 of the TableView test plan: selection events.
//
// Spec basis: TableView.idl:554 declares SelectionChanged using the platform
// Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs, not a bespoke type. Reusing the platform
// type is itself the contract: the args mean what they mean on Selector and ListView, or a handler
// could not be shared between them.
[TestClass]
public class TableViewSelectionEventTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies SelectionChanged reports the new item in AddedItems and the previous one in RemovedItems.")]
	public async Task VerifySelectionChangedReportsAddedAndRemovedItems()
	{
		TableView tableView = null;
		List<Person> items = null;
		var events = new List<(IList<object> Added, IList<object> Removed)>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.SelectionChanged += (sender, args) =>
				events.Add((args.AddedItems.ToList(), args.RemovedItems.ToList()));

			tableView.Select(0);
			tableView.Select(2);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(2, events.Count, "Two distinct selections should raise two events.");

			// The args constructor takes (removedItems, addedItems) - the reverse of the property
			// reading order - so both collections are asserted explicitly rather than only counted.
			// Swapped collections would otherwise look correct: the counts would still match.
			Verify.AreEqual(1, events[0].Added.Count, "The first event should add exactly one item.");
			Verify.AreSame(items[0], events[0].Added[0], "The first event should add the newly selected item.");
			Verify.AreEqual(0, events[0].Removed.Count,
				"Nothing was selected before, so the first event should remove nothing.");

			Verify.AreEqual(1, events[1].Added.Count, "The second event should add exactly one item.");
			Verify.AreSame(items[2], events[1].Added[0], "The second event should add the newly selected item.");
			Verify.AreEqual(1, events[1].Removed.Count, "The second event should remove exactly one item.");
			Verify.AreSame(items[0], events[1].Removed[0],
				"The second event should report the previously selected item as removed.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies SelectionChanged does not fire for calls that do not change the selection.")]
	public async Task VerifySelectionChangedDoesNotFireForNoOpChanges()
	{
		TableView tableView = null;
		List<Person> items = null;
		var raiseCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected.");

			// Armed only after the real selection, so the counter measures no-ops alone.
			tableView.SelectionChanged += (sender, args) => raiseCount++;

			tableView.Select(1);
			tableView.Deselect(0);
			tableView.IsSelected(1);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// An event named SelectionChanged carrying an added/removed delta has no meaningful
			// payload when nothing changed - it would have to report two empty collections.
			Verify.AreEqual(0, raiseCount,
				"Re-selecting the current row, deselecting an unselected row, and querying IsSelected must all be silent.");
			Verify.AreEqual(1, tableView.SelectedIndex, "The selection itself should be unchanged.");
			Verify.AreSame(items[1], tableView.SelectedItem, "SelectedItem should be unchanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies switching SelectionMode to None raises exactly one SelectionChanged reporting the cleared item.")]
	public async Task VerifySelectionChangedFiresOnceForModeChangeClear()
	{
		TableView tableView = null;
		List<Person> items = null;
		var events = new List<(IList<object> Added, IList<object> Removed)>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 must be selected.");

			tableView.SelectionChanged += (sender, args) =>
				events.Add((args.AddedItems.ToList(), args.RemovedItems.ToList()));

			tableView.SelectionMode = TableViewSelectionMode.None;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// More than one raise would mean the clear is implemented as several passes over the
			// same transition; zero would mean a handler never learns the selection went away.
			Verify.AreEqual(1, events.Count,
				"Switching to None should raise SelectionChanged exactly once.");

			if (events.Count != 1)
			{
				return;
			}

			Verify.AreEqual(0, events[0].Added.Count, "A clearing event should add nothing.");
			Verify.AreEqual(1, events[0].Removed.Count, "A clearing event should remove exactly one item.");
			Verify.AreSame(items[1], events[0].Removed[0],
				"The clearing event should report the item that was selected.");
		});
	}
}

// Category 6.5 of the TableView test plan: the row selection surface.
//
// Spec basis: TableView.idl:423-426 - TableViewRow.IsSelected is a "Read-only DP written only by the
// owning TableView ... Drives the Selected* states."
//
// NOT IMPLEMENTED HERE: VerifyRowIsSelectedClearedOnDeselectAll - folded into the test below as its
// final step rather than repeating the whole setup for one extra assertion.
[TestClass]
public class TableViewRowSelectionSurfaceTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies TableViewRow.IsSelected tracks the owning control's selection as it is set, moved and cleared.")]
	public async Task VerifyRowIsSelectedFollowsOwnerSelection()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(8);
			tableView = CreateSelectionTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => tableView.Select(1));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
			VerifyExactlyOneRowSelected(tableView, items, 1, "after selecting index 1"));

		RunOnUIThread.Execute(() => tableView.Select(2));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
			// The stale-row case: a row left lit after the selection moves would show two selected
			// rows in a single-selection control.
			VerifyExactlyOneRowSelected(tableView, items, 2, "after moving the selection to index 2"));

		RunOnUIThread.Execute(() => tableView.DeselectAll());
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.IsFalse(row.IsSelected, "DeselectAll must drive every realized row to IsSelected == false.");
			}
		});
	}
}

// An ObservableCollection whose contents can be replaced wholesale, raising exactly one Reset with
// the new contents ALREADY IN PLACE. This is the shape a refresh, a re-query or a filter change
// produces, and it is the only reset shape where "did the selected item survive?" is answerable at
// the moment the notification is handled.
//
// Clear() + Add() is deliberately NOT equivalent: Clear() raises its Reset while the collection is
// empty, so a still-wanted item is genuinely absent right then and no control could preserve it.
// Writing the reset test that way makes it look like the surviving-item path is covered when it
// never runs.
internal class ResettableCollection<T> : ObservableCollection<T>
{
	internal ResettableCollection(IEnumerable<T> items) : base(items)
	{
	}

	internal void ReplaceAll(IEnumerable<T> items)
	{
		// Mutate through Items so the individual edits stay silent, then raise one Reset.
		Items.Clear();

		foreach (var item in items)
		{
			Items.Add(item);
		}

		OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
		OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}
}

internal static class TableViewSelectionTestHelpers
{
	// Deliberately takes object: the source mutation tests pass an ObservableCollection, the rest a
	// plain List, and the control's contract is the same for both.
	internal static TableView CreateSelectionTable(object itemsSource)
		=> CreateTableViewWithColumns(itemsSource);

	// The invariant TableView.idl:532-534 states outright: the two projections "stay coherent with
	// each other". Everything here follows from that plus ":541 the selected data item" - an item
	// that is not in the source is not a data item.
	internal static void VerifySelectionIsCoherent(TableView tableView, IList<Person> source, string context)
	{
		var index = tableView.SelectedIndex;
		var item = tableView.SelectedItem;

		if (index == -1)
		{
			Verify.IsNull(item, $"SelectedItem must be null exactly when SelectedIndex is -1 ({context}).");
			return;
		}

		Verify.IsGreaterThanOrEqual(index, 0,
			$"SelectedIndex must be -1 or a real index, never another negative value ({context}).");
		Verify.IsLessThan(index, source.Count,
			$"SelectedIndex must be within the current source ({context}).");
		Verify.IsNotNull(item,
			$"SelectedItem must not be null while SelectedIndex reports a selection ({context}).");
		Verify.AreSame(source[index], item,
			$"SelectedItem must be the item at SelectedIndex ({context}).");
		Verify.IsTrue(tableView.IsSelected(index),
			$"IsSelected must agree with SelectedIndex ({context}).");
	}

	// Row chrome, checked by DataContext rather than by position, so a scrolled or recycled row
	// cannot make a wrong row look right.
	internal static void VerifyExactlyOneRowSelected(
		TableView tableView,
		IList<Person> items,
		int expectedIndex,
		string context)
	{
		var rows = GetRealizedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, $"Rows should be realized ({context}).");

		var selectedCount = 0;

		foreach (var row in rows)
		{
			if (!row.IsSelected)
			{
				continue;
			}

			selectedCount++;

			var rowItem = row.DataContext as Person;
			if (rowItem == null)
			{
				Verify.Fail($"A selected row should carry its source item as DataContext ({context}).");
				continue;
			}

			Verify.AreSame(items[expectedIndex], rowItem,
				$"The row reporting IsSelected should be the one showing the selected item ({context}).");
		}

		Verify.AreEqual(1, selectedCount,
			$"Exactly one realized row should report IsSelected in a single-selection control ({context}).");
	}
}
