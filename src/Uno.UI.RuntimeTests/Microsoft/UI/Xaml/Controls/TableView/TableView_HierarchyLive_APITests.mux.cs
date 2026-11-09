// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_HierarchyLive_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTreeTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewHierarchyLiveTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Live shaping on a ParentBy tree crossed with expansion intent, selection and row automation.
[TestClass]
public class TableViewHierarchyLiveTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a live sort key edit on an expanded parent moves its whole subtree, still expanded, among its siblings.")]
	public async Task VerifyLiveSortMovesExpandedSubtreeWithParent()
	{
		var list = MakeDeepLiveTree();
		var tableView = await Place(MakeLiveSource(list, s => s.ParentBy(LiveById, LiveByParent).Sort(LiveByScore, SortDirection.Ascending)), LoadContent);
		await Toggle(tableView, "Ann", true);
		await Toggle(tableView, "Bob", true);

		Verify.AreEqual("Dee1+ Ann1- Cat2 Bob2- Fin3", Labels(tableView),
			"Precondition: roots sorted by Score, Ann and Bob expanded, Dee collapsed.");
		Verify.AreEqual("Dee 1/2 Ann 2/2 Cat 1/2 Bob 2/2 Fin 1/1", Positions(tableView),
			"Precondition: Ann is the second root.");

		Verify.AreEqual("Ann1- Cat2 Bob2- Fin3 Dee1+", await After(tableView, () => Named(list, "Ann").Score = 1),
			"Ann's descendants must move with it, in order and still expanded, and Dee must stay collapsed.");
		Verify.AreEqual("Ann 1/2 Cat 1/2 Bob 2/2 Fin 1/1 Dee 2/2", Positions(tableView),
			"Root PositionInSet must follow the live move; descendant sibling sets are unchanged.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a selected descendant follows its ancestor when a live sort edit moves the ancestor's subtree.")]
	public async Task VerifyLiveSortMovingAncestorCarriesSelectedDescendant()
	{
		var list = MakeDeepLiveTree();
		var tableView = await Place(MakeLiveSource(list, s => s.ParentBy(LiveById, LiveByParent).Sort(LiveByScore, SortDirection.Ascending)), LoadContent);
		await Toggle(tableView, "Ann", true);
		await Toggle(tableView, "Bob", true);
		Verify.AreEqual("Dee1+ Ann1- Cat2 Bob2- Fin3", Labels(tableView), "Precondition: Fin is projected under Bob.");

		var fin = Named(list, "Fin");
		var changes = 0;
		RunOnUIThread.Execute(() =>
		{
			tableView.SelectionChanged += (s, e) => changes++;
			tableView.Select(ProjectedIndexOf(tableView, fin));
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(fin, tableView.SelectedItem, "Precondition: Fin is selected.");
			Verify.AreEqual(4, tableView.SelectedIndex, "Precondition: Fin is the last projected row.");
			Verify.AreEqual(1, changes, "Precondition: one SelectionChanged for the select.");
		});

		Verify.AreEqual("Ann1- Cat2 Bob2- Fin3 Dee1+", await After(tableView, () => Named(list, "Ann").Score = 1),
			"Precondition: Ann's subtree moved ahead of Dee.");

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(fin, tableView.SelectedItem, "Selection must stay on the moved descendant.");
			Verify.AreEqual(3, tableView.SelectedIndex, "SelectedIndex must report the descendant's new position.");
			Verify.IsTrue(FindTreeRow(tableView, "Fin").IsSelected, "The moved descendant's row must render selected.");
			Verify.AreEqual(1, changes, "Re-anchoring the same item must not raise SelectionChanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live filter exclusion of a selected descendant clears selection, and restoring the row does not reselect it.")]
	public async Task VerifyLiveFilterExclusionClearsSelectedDescendant()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeLiveSource(list, s => s.ParentBy(LiveById, LiveByParent).Filter(MinScore(5))), LoadContent, expandRows: true);
		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", Labels(tableView), "Precondition: every row passes Score >= 5, fully expanded.");

		var bob = Named(list, "Bob");
		var cat = Named(list, "Cat");
		var changes = 0;
		RunOnUIThread.Execute(() =>
		{
			tableView.SelectionChanged += (s, e) => changes++;
			tableView.Select(ProjectedIndexOf(tableView, cat));
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(cat, tableView.SelectedItem, "Precondition: Cat is selected.");
			Verify.AreEqual(2, tableView.SelectedIndex, "Precondition: Cat is the third projected row.");
			Verify.AreEqual(1, changes, "Precondition: one SelectionChanged for the select.");
		});

		Verify.AreEqual("Ann1- Cat2 Dee1- Eli2", await After(tableView, () => bob.Score = 0), "Bob must leave the filter.");
		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(cat, tableView.SelectedItem, "Excluding a sibling must keep the descendant selected.");
			Verify.AreEqual(1, tableView.SelectedIndex, "SelectedIndex must follow Cat up one row.");
			Verify.AreEqual(1, changes, "Re-anchoring the same item must not raise SelectionChanged.");
		});

		Verify.AreEqual("Ann1 Dee1- Eli2", await After(tableView, () => cat.Score = 0), "Cat must leave the filter, leaving Ann childless.");
		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(tableView.SelectedItem, "Excluding the selected descendant must clear SelectedItem.");
			Verify.AreEqual(-1, tableView.SelectedIndex, "SelectedIndex must agree with the cleared SelectedItem.");
			Verify.AreEqual(2, changes, "One SelectionChanged for the clear.");
		});

		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", await After(tableView, () =>
		{
			bob.Score = 30;
			cat.Score = 10;
		}), "Bob and Cat must rejoin the filter under an expanded Ann.");
		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(tableView.SelectedItem, "A row rejoining the filter must not resurrect the cleared selection.");
			Verify.AreEqual(-1, tableView.SelectedIndex, "SelectedIndex must stay cleared.");
			Verify.IsFalse(FindTreeRow(tableView, "Cat").IsSelected, "Cat's row must not render selected.");
			Verify.AreEqual(2, changes, "No further SelectionChanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an expanded branch that a live filter edit removes comes back expanded with its children projected.")]
	public async Task VerifyLiveFilterRestoresExpandedBranchExpanded()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeLiveSource(list, s => s.ParentBy(LiveById, LiveByParent).Filter(MinScore(5))), LoadContent);
		Verify.AreEqual("Ann1+ Dee1+", Labels(tableView), "Precondition: roots start collapsed.");

		await Toggle(tableView, "Dee", true);
		Verify.AreEqual("Ann1+ Dee1- Eli2", Labels(tableView), "Precondition: Dee expanded by the user.");

		var dee = Named(list, "Dee");
		var eli = Named(list, "Eli");
		Verify.AreEqual("Ann1+", await After(tableView, () =>
		{
			dee.Score = 0;
			eli.Score = 0;
		}), "The whole Dee branch must leave the filter.");

		// Dee matches on its own again, so its expansion comes from saved intent, not the context overlay.
		Verify.AreEqual("Ann1+ Dee1- Eli2", await After(tableView, () =>
		{
			dee.Score = 20;
			eli.Score = 5;
		}), "Dee must return expanded with Eli projected, and Ann must stay collapsed.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live parent-key edit keeps the moved row selected and its peer reports the new Level, PositionInSet and SizeOfSet.")]
	public async Task VerifyLiveReparentKeepsSelectionAndReportsNewLevel()
	{
		var list = MakeLiveTree();
		var tableView = await Place(MakeLiveSource(list, s => s.ParentBy(LiveById, LiveByParent)), LoadContent, expandRows: true);
		Verify.AreEqual("Ann1- Bob2 Cat2 Dee1- Eli2", Labels(tableView), "Precondition: fully expanded.");

		var eli = Named(list, "Eli");
		var changes = 0;
		RunOnUIThread.Execute(() =>
		{
			tableView.SelectionChanged += (s, e) => changes++;
			tableView.Select(ProjectedIndexOf(tableView, eli));
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(eli, tableView.SelectedItem, "Precondition: Eli is selected.");
			Verify.AreEqual(4, tableView.SelectedIndex, "Precondition: Eli is the last projected row.");
			Verify.AreEqual(2, GetRowPeer(FindTreeRow(tableView, "Eli")).GetLevel(), "Precondition: Eli is a child of Dee.");
			Verify.AreEqual("Ann 1/2 Bob 1/2 Cat 2/2 Dee 2/2 Eli 1/1", PositionLabels(tableView), "Precondition: sibling sets.");
		});

		// ExpandAllRows moved the expansion baseline, so Bob shows expanded once it gains a child.
		Verify.AreEqual("Ann1- Bob2- Eli3 Cat2 Dee1", await After(tableView, () => eli.ParentId = 2),
			"Eli must move under Bob.");

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(eli, tableView.SelectedItem, "Selection must stay on the reparented row.");
			Verify.AreEqual(2, tableView.SelectedIndex, "SelectedIndex must report Eli's new position.");
			Verify.AreEqual(1, changes, "Re-anchoring the same item must not raise SelectionChanged.");

			var row = FindTreeRow(tableView, "Eli");
			Verify.IsTrue(row.IsSelected, "The reparented row must render selected.");
			Verify.AreEqual(3, GetRowPeer(row).GetLevel(), "The row peer must report Eli's new depth.");
			Verify.AreEqual("Ann 1/2 Bob 1/2 Eli 1/1 Cat 2/2 Dee 2/2", PositionLabels(tableView),
				"PositionInSet and SizeOfSet must be relative to Eli's new sibling set.");
		});
	}
}

internal static class TableViewHierarchyLiveTestHelpers
{
	// MakeLiveTree plus Fin under Bob: Ann > { Bob > { Fin }, Cat }, Dee > { Eli }.
	internal static ObservableCollection<LiveRow> MakeDeepLiveTree()
	{
		var list = MakeLiveTree();
		list.Add(new LiveRow { Id = 6, Name = "Fin", Score = 1, Team = "X", ParentId = 2 });
		return list;
	}

	internal static TableViewSource MakeLiveSource(object items, Func<TableViewSource, TableViewSource> shape)
	{
		TableViewSource source = null;
		RunOnUIThread.Execute(() =>
		{
			source = shape(TableViewSource.From(items));
			source.IsLiveShaping = true;
		});

		return source;
	}

	// load is the test class's LoadContent, which is protected on the base.
	internal static async Task<TableView> Place(TableViewSource source, Action<UIElement> load, bool expandRows = false)
	{
		TableView tableView = null;
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			tableView = CreateTreeTable(source);
			load(tableView);
		});

		await SettleLayout(tableView);

		if (expandRows)
		{
			RunOnUIThread.Execute(() => tableView.ExpandAllRows());
			await SettleLayout(tableView);
		}

		return tableView;
	}

	internal static string Labels(TableView tableView)
	{
		string labels = null;
		RunOnUIThread.Execute(() => labels = TreeLabels(tableView));
		return labels;
	}

	internal static string Positions(TableView tableView)
	{
		string positions = null;
		RunOnUIThread.Execute(() => positions = PositionLabels(tableView));
		return positions;
	}

	// Mutates on the UI thread, lets the posted live reshape run, and returns the projection.
	internal static async Task<string> After(TableView tableView, Action mutate)
	{
		RunOnUIThread.Execute(mutate);
		await SettleLayout(tableView);
		return Labels(tableView);
	}

	internal static async Task Toggle(TableView tableView, string name, bool expand)
	{
		RunOnUIThread.Execute(() => ToggleTreeRow(tableView, name, expand));
		await SettleLayout(tableView);
	}

	// Call on the UI thread.
	internal static int ProjectedIndexOf(TableView tableView, object item)
	{
		var index = GetProjectedItems(tableView).IndexOf(item);
		if (index < 0)
		{
			Verify.Fail($"'{NameOf(item)}' is not projected. Projection: {TreeLabels(tableView)}");
		}

		return index;
	}
}
