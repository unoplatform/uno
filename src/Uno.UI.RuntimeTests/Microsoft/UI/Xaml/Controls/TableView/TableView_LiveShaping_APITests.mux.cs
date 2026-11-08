// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_LiveShaping_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTreeTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewEditingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// TableViewSource.IsLiveShaping on flat, grouped and tree projections.
//
// "Mutate" means setting a property on an observable item, which raises PropertyChanged. A live
// reshape is posted to the dispatcher rather than run inside PropertyChanged, so every test settles
// the layout before it reads the projection. Projections are compared as TreeLabels strings: a flat
// row is its bare name, a tree row is Name + Level + '-'/'+' (expanded/collapsed), a group header is
// [key]. Live-off staleness and the default flag value are covered by TableViewSourceShapingTests.
[TestClass]
public class TableViewLiveShapingTests : TableViewApiTestBase
{
	// PENDING DECISION (not implemented): live reshape and keyboard focus.
	// The dev spec says focus stays at the same position across a reshape (FocusStaysAtSamePositionWhenSortReordersRows,
	// currently Ignored as a product gap). Whether a live reshape keeps focus on the moved item or on the position
	// needs an owner decision before a focus test is added. Selection-follows-item is covered by VerifySelectionFollowsItem.

	#region 4.2 Sort

	[TestMethod]
	[TestProperty("Description", "Verifies a sort key edit on a live source re-sorts the moved row.")]
	public async Task VerifyLiveOnRowMoves()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));
		Verify.AreEqual("A B C D", Labels(tableView), "Precondition: sorted by Score ascending.");

		Verify.AreEqual("B C D A", await After(tableView, () => Named(list, "A").Score = 50),
			"A live sort key edit must move the row to its new sorted slot.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live reshape is posted to the dispatcher rather than run inside PropertyChanged.")]
	public async Task VerifyReshapeIsPostedNotInline()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));

		string inline = null;
		RunOnUIThread.Execute(() =>
		{
			Named(list, "A").Score = 50;
			tableView.UpdateLayout();
			inline = TreeLabels(tableView);
		});

		await SettleLayout(tableView);

		Verify.AreEqual("A B C D", inline, "The reshape must not run synchronously inside PropertyChanged.");
		Verify.AreEqual("B C D A", Labels(tableView), "The posted reshape must have run once the dispatcher is idle.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies several sort key edits in one turn produce one consistent order.")]
	public async Task VerifyBulkMutationOneTurn()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));

		var actual = await After(tableView, () =>
		{
			Named(list, "A").Score = 50;
			Named(list, "B").Score = 45;
			Named(list, "C").Score = 1;
		});

		Verify.AreEqual("C D B A", actual, "Edits made in one turn must settle into the final sorted order.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an edit to a property the shaping does not read leaves the order alone.")]
	public async Task VerifyUnrelatedPropertyKeepsOrder()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));

		Verify.AreEqual("A B2 C D", await After(tableView, () => Named(list, "B").Name = "B2"),
			"Renaming a row must not reorder a Score sort.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a property-path sort key is tracked live.")]
	public async Task VerifyPathAxis()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(nameof(LiveRow.Score), SortDirection.Ascending), live: true));

		Verify.AreEqual("D A B C", await After(tableView, () => Named(list, "D").Score = 5),
			"A Sort(path) key edit must move the row live.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies selection stays on the item a live reshape moved.")]
	public async Task VerifySelectionFollowsItem()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));

		RunOnUIThread.Execute(() => tableView.Select(0));
		await SettleLayout(tableView);

		await After(tableView, () => Named(list, "A").Score = 50);

		RunOnUIThread.Execute(() =>
		{
			var selected = tableView.SelectedItem as LiveRow;
			Verify.AreEqual("A", selected?.Name ?? "(none)", "Selection must follow the moved item.");
			Verify.AreEqual(3, tableView.SelectedIndex, "SelectedIndex must report the item's new position.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an app selector that throws during a live reshape does not crash or drop rows.")]
	public async Task VerifyThrowingSelectorNoCrash()
	{
		var list = MakeLiveFlat();
		var throwing = LiveKey(r => r.Score == 99 ? throw new InvalidOperationException("boom") : (object)r.Score);
		var tableView = await Place(MakeSource(list, s => s.Sort(throwing, SortDirection.Ascending), live: true));

		await After(tableView, () => Named(list, "A").Score = 99);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(4, GetProjectedCount(tableView), "A throwing selector must not crash or drop rows.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies turning IsLiveShaping off stops tracking edits.")]
	public async Task VerifyDisableStopsTracking()
	{
		var list = MakeLiveFlat();
		var source = MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true);
		var tableView = await Place(source);

		RunOnUIThread.Execute(() => source.IsLiveShaping = false);

		Verify.AreEqual("A B C D", await After(tableView, () => Named(list, "A").Score = 50),
			"Edits made after live shaping is turned off must not reshape.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies turning IsLiveShaping on runs one catch-up reshape for edits made while it was off.")]
	public async Task VerifyTurnOnPicksUpUntrackedEdit()
	{
		var list = MakeLiveFlat();
		var source = MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: false);
		var tableView = await Place(source);

		Verify.AreEqual("A B C D", await After(tableView, () => Named(list, "A").Score = 50),
			"With live shaping off, the edit must stay stale.");
		Verify.AreEqual("B C D A", await After(tableView, () => source.IsLiveShaping = true),
			"Turning live shaping on must catch up the untracked edit.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort key edit followed by an Add in the same turn settles into one correct order.")]
	public async Task VerifySortKeyEditThenAddInOneTurnKeepsOrder()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));
		Verify.AreEqual("A B C D", Labels(tableView), "Precondition: sorted by Score ascending.");

		// The live reshape is posted, so the Add arrives while the projection still reflects A's
		// old Score. The incremental sorted insert must not binary-search that stale order.
		var actual = await After(tableView, () =>
		{
			Named(list, "A").Score = 50;
			list.Add(new LiveRow { Id = 5, Name = "E", Score = 25, Team = "X" });
		});

		Verify.AreEqual("B E C D A", actual,
			"An edit and an Add in one turn must settle into the fully sorted order.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies turning IsLiveShaping off hands sort key staleness back to the next-collection-change repair.")]
	public async Task VerifyDisableHandsStalenessBackToTheCollectionChangeRepair()
	{
		var list = MakeLiveFlat();
		var source = MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true);
		var tableView = await Place(source);

		RunOnUIThread.Execute(() => source.IsLiveShaping = false);

		Verify.AreEqual("A B C D", await After(tableView, () => Named(list, "A").Score = 50),
			"With live shaping off, the edit must stay stale until a collection change.");

		Verify.AreEqual("B C D A E",
			await After(tableView, () => list.Add(new LiveRow { Id = 5, Name = "E", Score = 60, Team = "X" })),
			"The next collection change must repair the stale row before applying the add.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live reshape waits for an open cell edit and runs once the edit closes.")]
	public async Task VerifyHeldWhileEditing()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true), editable: true);

		// A vetoed commit keeps the editor opened by SetValue open.
		var veto = true;
		var began = false;
		RunOnUIThread.Execute(() =>
		{
			tableView.CellEditEnding += (s, e) => e.Cancel = veto;
			CaptureSetValue(GetValueProvider(tableView, Named(list, "C"), columnIndex: 0), "Zed");
			began = tableView.IsEditing;
			veto = false;
		});

		await SettleLayout(tableView);
		Verify.IsTrue(began, "Precondition: SetValue with a vetoed commit must leave an edit open.");

		var during = await After(tableView, () => Named(list, "A").Score = 50);
		var editingDuring = false;
		RunOnUIThread.Execute(() => editingDuring = tableView.IsEditing);

		Verify.AreEqual("A B C D", during, "A live reshape must wait while a cell edit is open.");
		Verify.IsTrue(editingDuring, "A live change must not end the user's edit.");

		var after = await After(tableView, () => tableView.CancelEdit());
		var editingAfter = true;
		RunOnUIThread.Execute(() => editingAfter = tableView.IsEditing);

		Verify.IsFalse(editingAfter, "CancelEdit must close the edit.");
		Verify.AreEqual("B C D A", after, "The held reshape must run once the edit closes.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies bulk expand and collapse calls on a flat source during an open edit do not end it.")]
	public async Task VerifyBulkOnFlatDoesNotEndEdit()
	{
		var items = MakeTree();
		var tableView = await Place(MakeSource(items, s => s.Sort(EmpKey(e => e.Name), SortDirection.Ascending), live: false), editable: true);

		var veto = true;
		var endings = 0;
		var editing = false;
		var kept = false;
		var observed = -1;

		RunOnUIThread.Execute(() =>
		{
			tableView.CellEditEnding += (s, e) =>
			{
				endings++;
				if (veto)
				{
					e.Cancel = true;
				}
			};

			// The vetoed commit leaves the editor open.
			CaptureSetValue(GetValueProvider(tableView, Named(items, "Ada"), columnIndex: 0), "Zed");
			veto = false;
			editing = tableView.IsEditing;
			endings = 0;

			tableView.ExpandAllRows();
			tableView.CollapseAllRows();
			tableView.ExpandAllGroups();
			tableView.CollapseAllGroups();

			kept = tableView.IsEditing;
			observed = endings;
			tableView.CancelEdit();
		});

		await SettleLayout(tableView);

		Verify.IsTrue(editing, "Precondition: SetValue with a vetoed commit must leave an edit open.");
		Verify.IsTrue(kept, "Expand/collapse-all calls with nothing to do must not end the edit.");
		Verify.AreEqual(0, observed, "No CellEditEnding may be raised by the no-op bulk calls.");
	}

	#endregion

	#region 4.3 Custom comparer

	[TestMethod]
	[TestProperty("Description", "Verifies a CustomSortComparer sort is stale with live shaping off and re-places a changed row with it on.")]
	public async Task VerifyComparerLiveOffThenOn()
	{
		var offList = MakeLiveFlat();
		var offTable = await Place(MakeSource(offList, s => s, live: false));
		await SortByComparerColumn(offTable, SortDirection.Descending);
		Verify.AreEqual("D C B A", Labels(offTable), "Precondition: comparer sort, Score descending.");
		Verify.AreEqual("D C B A", await After(offTable, () => Named(offList, "A").Score = 25),
			"With live shaping off, a comparer sort must stay stale.");

		var onList = MakeLiveFlat();
		var onTable = await Place(MakeSource(onList, s => s, live: true));
		await SortByComparerColumn(onTable, SortDirection.Descending);
		Verify.AreEqual("D C B A", Labels(onTable), "Precondition: comparer sort, Score descending.");
		Verify.AreEqual("D C A B", await After(onTable, () => Named(onList, "A").Score = 25),
			"With live shaping on, the comparer must re-place the changed row.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies turning IsLiveShaping on re-ranks a comparer sort for edits made while it was off.")]
	public async Task VerifyComparerTurnOnPicksUpUntrackedEdit()
	{
		var list = MakeLiveFlat();
		var source = MakeSource(list, s => s, live: false);
		var tableView = await Place(source);
		await SortByComparerColumn(tableView, SortDirection.Descending);

		Verify.AreEqual("D C B A", await After(tableView, () => Named(list, "A").Score = 25),
			"With live shaping off, the comparer sort must stay stale.");
		Verify.AreEqual("D C A B", await After(tableView, () => source.IsLiveShaping = true),
			"Turning live shaping on must re-rank the untracked edit.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a row edited while out of the source is re-placed by the comparer when re-inserted.")]
	public async Task VerifyComparerReinsertedRowIsReplaced()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s, live: true));
		await SortByComparerColumn(tableView, SortDirection.Descending);

		var actual = await After(tableView, () =>
		{
			var a = Named(list, "A");
			list.Remove(a);
			a.Score = 25;
			list.Add(a);
		});

		Verify.AreEqual("D C A B", actual, "A re-inserted row must not keep the rank it had before the edit.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a row leaving a comparer tie re-ranks the former partner correctly.")]
	public async Task VerifyComparerLeavesTie()
	{
		var list = MakeLiveFlat();
		Named(list, "C").Score = 20;
		var tableView = await Place(MakeSource(list, s => s, live: true));
		await SortByComparerColumn(tableView, SortDirection.Ascending);

		Verify.AreEqual("A B C D", Labels(tableView), "Precondition: B and C tie at 20.");
		Verify.AreEqual("A C B D", await After(tableView, () => Named(list, "C").Score = 15),
			"C leaving the tie must move ahead of B.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a burst of comparer key edits in one turn is re-ranked in a single pass.")]
	public async Task VerifyComparerBulkReranks()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s, live: true));
		await SortByComparerColumn(tableView, SortDirection.Ascending);

		var actual = await After(tableView, () =>
		{
			Named(list, "A").Score = 40;
			Named(list, "B").Score = 30;
			Named(list, "C").Score = 20;
			Named(list, "D").Score = 10;
		});

		Verify.AreEqual("D C B A", actual, "Every changed row must be re-ranked exactly once.");
	}

	#endregion

	#region 4.4 Filter and group

	[TestMethod]
	[TestProperty("Description", "Verifies rows leave and join a live filter when the predicate input changes.")]
	public async Task VerifyFilterLiveOnLeaveAndJoin()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Filter(MinScore(20)), live: true));
		Verify.AreEqual("B C D", Labels(tableView), "Precondition: Score >= 20.");

		Verify.AreEqual("C D", await After(tableView, () => Named(list, "B").Score = 5), "B must leave the filter.");
		Verify.AreEqual("A C D", await After(tableView, () => Named(list, "A").Score = 25), "A must join the filter.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies one edit that moves both the sort key and the filter verdict re-applies both.")]
	public async Task VerifySortKeyEditAlsoRefilters()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Filter(MinScore(20)).Sort(LiveByScore, SortDirection.Ascending), live: true));

		Verify.AreEqual("C D", await After(tableView, () => Named(list, "B").Score = 5),
			"The edit must remove the row, not just re-sort it.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live group key change moves the row, creates the new group and orders groups by first appearance.")]
	public async Task VerifyGroupLiveOnRowChangesBucket()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.GroupBy(LiveByTeam), live: true));
		Verify.AreEqual("[X] A C [Y] B D", Labels(tableView), "Precondition: grouped by Team.");

		// The rebuild orders groups by first appearance, so A now opens [Y] ahead of [X].
		Verify.AreEqual("[Y] A B D [X] C", await After(tableView, () => Named(list, "A").Team = "Y"),
			"A must move to the [Y] group.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies object group keys regroup by the group identity selector.")]
	public async Task VerifyObjectKeyRegroup()
	{
		var list = MakeLiveObjectTree();
		var tableView = await Place(MakeSource(list,
			s => s.GroupBy(LiveKey(r => r.ParentRow ?? r), new TableViewIdentitySelector(o => ((LiveRow)o).Name)),
			live: true));

		// Object keys print as the type name; shorten it so the expectation reads.
		var actual = (await After(tableView, () => Named(list, "Eli").ParentRow = Named(list, "Ann")))
			.Replace(typeof(LiveRow).FullName, "g");

		Verify.AreEqual("[g] Ann Bob Cat Eli [g] Dee", actual, "Eli must move to Ann's group.");
	}

	#endregion

	#region 4.5 Source tracking

	[TestMethod]
	[TestProperty("Description", "Verifies an item added to the source is tracked live.")]
	public async Task VerifyAddedItemIsTracked()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));
		var added = new LiveRow { Id = 5, Name = "E", Score = 25, Team = "X" };

		Verify.AreEqual("A B E C D", await After(tableView, () => list.Add(added)), "The added row must sort into place.");
		Verify.AreEqual("A B C D E", await After(tableView, () => added.Score = 100), "The added row must be tracked.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an item removed from the source is no longer tracked.")]
	public async Task VerifyRemovedItemIsUntracked()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));
		var removed = Named(list, "A");

		Verify.AreEqual("B C D", await After(tableView, () => list.Remove(removed)), "Precondition: A removed.");
		Verify.AreEqual("B C D", await After(tableView, () => removed.Score = 100),
			"Editing a removed item must not reshape the projection.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a replace untracks the old item and tracks the new one.")]
	public async Task VerifyReplacedItemOldOutNewIn()
	{
		var list = MakeLiveFlat();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));
		var oldA = list[0];
		var newA = new LiveRow { Id = 1, Name = "A2", Score = 10, Team = "X" };

		await After(tableView, () => list[0] = newA);

		Verify.AreEqual("A2 B C D", await After(tableView, () => oldA.Score = 100), "Edits to the replaced item must be ignored.");
		Verify.AreEqual("B C D A2", await After(tableView, () => newA.Score = 100), "Edits to the new item must be tracked.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a Reset re-subscribes the items it brings back.")]
	public async Task VerifyResetRetracks()
	{
		var list = MakeLiveFlat();
		var items = list.ToList();
		var tableView = await Place(MakeSource(list, s => s.Sort(LiveByScore, SortDirection.Ascending), live: true));

		await After(tableView, () =>
		{
			list.Clear();
			foreach (var item in items)
			{
				list.Add(item);
			}
		});

		Verify.AreEqual("B C D A", await After(tableView, () => Named(list, "A").Score = 50),
			"Items must be tracked again after a Reset.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies items without INotifyPropertyChanged are ignored safely.")]
	public async Task VerifyNoINPCNotTracked()
	{
		var list = new ObservableCollection<PlainRow>(new[]
		{
			new PlainRow { Name = "A", Score = 10 },
			new PlainRow { Name = "B", Score = 20 },
			new PlainRow { Name = "C", Score = 30 },
			new PlainRow { Name = "D", Score = 40 },
		});
		var tableView = await Place(MakeSource(list, s => s.Sort(nameof(PlainRow.Score), SortDirection.Ascending), live: true));

		Verify.AreEqual("A B C D", await After(tableView, () => list[0].Score = 50),
			"A non-observable item cannot be tracked, and must not throw.");
	}

	#endregion

	#region 4.6 Live shaping on a tree

	[TestMethod]
	[TestProperty("Description", "Verifies a live re-sort on a tree stays inside the sibling set.")]
	public async Task VerifyLiveSortWithinSiblings()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent).Sort(LiveByScore, SortDirection.Ascending), live: true),
			expandRows: true);
		Verify.AreEqual("Dee1- Eli2 Ann1- Cat2 Bob2", Labels(tableView), "Precondition: siblings sorted by Score.");

		Verify.AreEqual("Dee1- Eli2 Ann1- Bob2 Cat2", await After(tableView, () => Named(list, "Cat").Score = 40),
			"Bob and Cat must swap under Ann, keeping the tree.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live parent-key edit reparents the row and keeps other nodes' expansion.")]
	public async Task VerifyLiveOnReparentByProperty()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent), live: true), expandRows: true);
		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", Labels(tableView), "Precondition: fully expanded.");

		Verify.AreEqual("Ann1- Bob2 Cat2 Eli2 Dee1", await After(tableView, () => Named(list, "Eli").ParentId = 1),
			"Eli must move under Ann, and Ann must stay expanded.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a parent-key edit stays stale with live shaping off and is picked up by a re-sort.")]
	public async Task VerifyLiveOffReparentStaysStaleUntilResort()
	{
		var staleList = MakeLiveTree();
		var staleTable = await Place(MakeSource(staleList, s => s.ParentBy(LiveById, LiveByParent), live: false), expandRows: true);
		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", await After(staleTable, () => Named(staleList, "Eli").ParentId = 1),
			"With live shaping off, a parent-key edit must not reparent.");

		var list = MakeLiveTree();
		var source = MakeSource(list, s => s.ParentBy(LiveById, LiveByParent), live: false);
		var tableView = await Place(source, expandRows: true);
		var actual = await After(tableView, () =>
		{
			Named(list, "Eli").ParentId = 1;
			source.Sort(LiveByScore, SortDirection.Ascending);
		});

		Verify.AreEqual("Dee1 Ann1- Eli2 Cat2 Bob2", actual, "A re-sort must re-read parent keys.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort key edit and a reparent in the same turn compose correctly.")]
	public async Task VerifyReparentAfterSortEditSameTurn()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent).Sort(LiveByScore, SortDirection.Ascending), live: true),
			expandRows: true);

		var actual = await After(tableView, () =>
		{
			Named(list, "Cat").Score = 40;
			Named(list, "Eli").ParentId = 1;
		});

		Verify.AreEqual("Dee1 Ann1- Eli2 Bob2 Cat2", actual, "Both the reparent and the sort edit must apply.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies turning live shaping on catches up a parent-key edit made while it was off.")]
	public async Task VerifyLiveTurnedOnAfterUntrackedReparent()
	{
		var list = MakeLiveTree();
		var source = MakeSource(list, s => s.ParentBy(LiveById, LiveByParent), live: false);
		var tableView = await Place(source, expandRows: true);

		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", await After(tableView, () => Named(list, "Eli").ParentId = 1),
			"Precondition: the untracked reparent stays stale.");

		var actual = await After(tableView, () =>
		{
			source.IsLiveShaping = true;
			source.Sort(LiveByScore, SortDirection.Ascending);
		});

		Verify.AreEqual("Dee1 Ann1- Eli2 Cat2 Bob2", actual, "The catch-up must include the parent axis.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies object parent keys live-reparent, and a later sort does not reuse the old tree.")]
	public async Task VerifyObjectKeyReparent()
	{
		var list = MakeLiveObjectTree();
		var source = MakeSource(list, s => s.ParentBy(LiveByObject, LiveByParentObject), live: true);
		var tableView = await Place(source, expandRows: true);

		Verify.AreEqual("Ann1- Bob2 Cat2 Eli2 Dee1", await After(tableView, () => Named(list, "Eli").ParentRow = Named(list, "Ann")),
			"Eli must move under Ann by key identity.");
		Verify.AreEqual("Dee1 Ann1- Eli2 Cat2 Bob2", await After(tableView, () => source.Sort(LiveByScore, SortDirection.Ascending)),
			"A later sort must use the new parent.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live filter match pulls in its ancestors.")]
	public async Task VerifyLiveFilterMatchBringsAncestor()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent).Filter(MinScore(35)), live: true),
			expandRows: true);
		Verify.AreEqual("Ann1", Labels(tableView), "Precondition: only Ann matches.");

		Verify.AreEqual("Ann1 Dee1- Eli2", await After(tableView, () => Named(list, "Eli").Score = 40),
			"Eli's live match must bring in Dee as its ancestor.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a root's live group-key edit moves its whole subtree.")]
	public async Task VerifyGroupedRootMovesWithSubtree()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent).GroupBy(LiveByTeam), live: true),
			expandRows: true);

		Verify.AreEqual("[Y] Ann1- Bob2 Cat2 Dee1- Eli2", await After(tableView, () => Named(list, "Ann").Team = "Y"),
			"Ann's subtree must move with it to [Y].");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a descendant's group key does not regroup it; only roots are grouped.")]
	public async Task VerifyGroupedDescendantKeyIgnored()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent).GroupBy(LiveByTeam), live: true),
			expandRows: true);

		Verify.AreEqual("[X] Ann1- Bob2 Cat2 [Y] Dee1- Eli2", await After(tableView, () => Named(list, "Bob").Team = "Z"),
			"Bob must stay under Ann.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live edit that creates a cycle keeps the last good projection and recovers once fixed.")]
	public async Task VerifyCycleKeepsProjectionThenRecovers()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeSource(list, s => s.ParentBy(LiveById, LiveByParent), live: true), expandRows: true);

		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", await After(tableView, () => Named(list, "Ann").ParentId = 2),
			"A cycle must keep the last good projection.");

		await After(tableView, () => Named(list, "Ann").ParentId = null);

		Verify.AreEqual("Ann1- Bob2 Cat2 Eli2 Dee1", await After(tableView, () => Named(list, "Eli").ParentId = 1),
			"Live shaping must recover once the cycle is fixed.");
	}

	#endregion

	#region Helpers

	private static TableViewSource MakeSource(object items, Func<TableViewSource, TableViewSource> shape, bool live)
	{
		TableViewSource source = null;
		RunOnUIThread.Execute(() =>
		{
			source = shape(TableViewSource.From(items));
			source.IsLiveShaping = live;
		});

		return source;
	}

	private async Task<TableView> Place(TableViewSource source, bool expandRows = false, bool editable = false)
	{
		TableView tableView = null;
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			tableView = CreateTreeTable(source, editable);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		if (expandRows)
		{
			RunOnUIThread.Execute(() => tableView.ExpandAllRows());
			await SettleLayout(tableView);
		}

		return tableView;
	}

	private static string Labels(TableView tableView)
	{
		string labels = null;
		RunOnUIThread.Execute(() => labels = TreeLabels(tableView));
		return labels;
	}

	// Mutates on the UI thread, lets the posted reshape run, and returns the projection.
	private static async Task<string> After(TableView tableView, Action mutate)
	{
		RunOnUIThread.Execute(mutate);
		await SettleLayout(tableView);
		return Labels(tableView);
	}

	// Adds a Score column that sorts only through a CustomSortComparer, then sorts by it.
	private static async Task SortByComparerColumn(TableView tableView, SortDirection direction)
	{
		TableViewTextColumn column = null;
		RunOnUIThread.Execute(() =>
		{
			column = MakeTextColumn("Score");
			column.CustomSortComparer = new LiveScoreComparer();
			tableView.Columns.Add(column);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(column, direction), "Precondition: the comparer column must sort.");
		});

		await SettleLayout(tableView);
	}

	#endregion
}
