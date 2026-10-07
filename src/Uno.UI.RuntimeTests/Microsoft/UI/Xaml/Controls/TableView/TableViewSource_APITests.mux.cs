// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableViewSource_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewShapingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 8 of the TableView test plan: TableViewSource data shaping.
//
// The whole contract for this category lives in TableViewSource.idl, which is unusually explicit:
// the whole-class note fixes the null/E_INVALIDARG rule, the UI-thread affinity, and the
// object-identity row model; each verb's comment fixes what that verb replaces, clears, or
// refuses. These tests cite it rather than reasoning from the shaping engine.
//
// HOW THE PROJECTION IS OBSERVED. The test plan used to claim this category needs no TableView.
// That is wrong: TableViewSource projects only the static From and the fluent verbs, and
// GetItemsSourceView()/GetRowMetadata() are internal C++ members, so from managed code the only
// public route to the projection is a bound TableView. Every test here therefore binds one and
// reads PART_RowsRepeater:
//
//   * repeater.ItemsSourceView.Count - the projected row count INCLUDING group header rows. This
//     is the projection itself, so it is immune to virtualization.
//   * repeater.TryGetElement(i)      - the container at projected index i: a TableViewRow whose
//     DataContext is the item, or a TableViewGroupHeader whose Content is a TableViewGroupInfo.
//
// Item counts are small and the host is tall so every row realizes; a missing element fails the
// test rather than being skipped. Assertions are on the projected SEQUENCE, never on cell text -
// cell text belongs to Category 2.
[TestClass]
public class TableViewSourceShapingTests : TableViewApiTestBase
{
	#region 8.1 Construction and acceptance

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewSource.From accepts a List and projects every item in source order.")]
	public async Task VerifyFromProjectsAListInSourceOrder()
	{
		TableView tableView = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			tableView = CreateShapedTable(TableViewSource.From(items));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(items.Count, GetProjectedCount(tableView),
				"A list source should project one row per item.");
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(), "unshaped list");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies From accepts an iterable-only source, which has no change notification at all.")]
	public async Task VerifyFromProjectsAnIterableOnlySource()
	{
		TableView tableView = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();

			// Deliberately not a vector: TableViewSource.idl lists IIterable<Object> /
			// IBindableIterable among the accepted shapes, and a LINQ sequence is the shape an
			// app most often reaches for.
			IEnumerable<ShapedPerson> iterable = items.Select(p => p);

			tableView = CreateShapedTable(TableViewSource.From(iterable));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(), "iterable-only source");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an unshaped TableViewSource still tracks its ObservableCollection, i.e. From subscribed.")]
	public async Task VerifyFromTracksAnObservableCollection()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(6, GetProjectedCount(tableView), "Baseline: six items before the add.");
			items.Insert(2, new ShapedPerson("Nadia", "Engineer", "Platform"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// No shaping verb is in force, so the projection is a plain mirror and the new item
			// must appear at its SOURCE position.
			VerifyProjection(
				tableView,
				new List<string> { "Asha", "Diego", "Nadia", "Mei", "Rafa", "Ines", "Owen" },
				"after inserting into an unshaped observable source");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies From(null) throws rather than yielding a null or empty source.")]
	public void VerifyFromNullThrows()
	{
		RunOnUIThread.Execute(() =>
		{
			// TableViewSource.idl, whole-class note: "Null items, predicate, or keySelector throws
			// E_INVALIDARG. (Note this differs from TableView's command surface, which no-ops on
			// bad input.)"
			Verify.Throws<ArgumentException>(() => TableViewSource.From(null),
				"From(null) must throw E_INVALIDARG, not produce an empty source.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies From rejects values that implement none of the accepted collection interfaces.")]
	public void VerifyFromUnsupportedSourceThrows()
	{
		RunOnUIThread.Execute(() =>
		{
			// "Anything else throws E_INVALIDARG." Both shapes are things an app can plausibly
			// pass by mistake - a scalar, and a single item where a collection was meant.
			Verify.Throws<ArgumentException>(() => TableViewSource.From(42),
				"A boxed scalar is not an accepted source shape.");
			Verify.Throws<ArgumentException>(() => TableViewSource.From(new ShapedPerson("Asha", "Designer", "Studio")),
				"A single non-collection object is not an accepted source shape.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a source with no verb applied is a faithful mirror: source order, no groups.")]
	public async Task VerifyFreshSourceIsUnshaped()
	{
		TableView tableView = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			tableView = CreateShapedTable(TableViewSource.From(items));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// The source list is deliberately unsorted by every field the other tests sort on, so
			// an accidental default sort would show up here.
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(), "fresh source");
			Verify.AreEqual(0, GroupHeaderCount(tableView),
				"A source with no GroupBy must project no group header rows.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies verbs chained in one expression both take effect.")]
	public async Task VerifyVerbsChainAndBothShapesApply()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var source = TableViewSource
				.From(MakeShapedPeople())
				.Filter(o => Person(o).Role == "Engineer")
				.Sort("Name", SortDirection.Descending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Engineers are Diego, Rafa, Owen; descending by name is Rafa, Owen, Diego.
			VerifyProjection(tableView, new List<string> { "Rafa", "Owen", "Diego" }, "chained filter + sort");
		});
	}

	#endregion

	#region 8.2 Filtering

	[TestMethod]
	[TestProperty("Description", "Verifies a predicate reduces the projection to matching items without reordering them.")]
	public async Task VerifyFilterReducesProjectionToMatchingItems()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);
		RunOnUIThread.Execute(() => source.Filter(o => Person(o).Role == "Engineer"));
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Relative order is asserted too: nothing in the IDL lets a filter reorder, so a
			// filter that also sorts would be a bug this test has to catch.
			VerifyProjection(tableView, new List<string> { "Diego", "Rafa", "Owen" }, "filtered to engineers");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ClearFilter restores every item, since the IDL makes it the supported way to un-filter.")]
	public async Task VerifyClearFilterRestoresEveryItem()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			source = TableViewSource.From(items).Filter(o => Person(o).Role == "Engineer");
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GetProjectedCount(tableView), "Baseline: the filter is in force.");
			source.ClearFilter();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(), "after ClearFilter");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a second Filter replaces the first rather than conjoining with it.")]
	public async Task VerifyReplacingTheFilterReevaluatesEveryItem()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople()).Filter(o => Person(o).Role == "Engineer");
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GetProjectedCount(tableView), "Baseline: engineers only.");

			// The two predicates are disjoint. If Filter conjoined, this would project nothing;
			// TableViewSource.h states the replace rule: "Single, always-first predicate: each
			// Filter() replaces the previous predicate".
			source.Filter(o => Person(o).Role == "Designer");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Ines" }, "after replacing the predicate");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Filter(null) throws and leaves the source usable, rather than acting as ClearFilter.")]
	public async Task VerifyNullFilterPredicateThrows()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople()).Filter(o => Person(o).Role == "Engineer");
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.Throws<ArgumentException>(() => source.Filter(null),
				"Filter(null) must throw; the IDL routes 'remove the filter' to ClearFilter().");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Rafa", "Owen" },
				"the rejected call must leave the previous filter in force");

			source.Filter(o => Person(o).Role == "Designer");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Ines" },
				"a valid filter after the rejected one must still work");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a predicate that throws does not wedge or corrupt the source.")]
	public async Task VerifyThrowingFilterPredicateLeavesSourceUsable()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Deliberately NOT asserting what happens to the throwing item. Neither the IDL nor
			// the design spec says whether a throwing predicate excludes it, includes it, or
			// aborts the shape, and freezing today's answer would be exactly the mistake Step 3
			// of the test protocol forbids. What is asserted is only that the app's bug costs it
			// nothing beyond the rows it was asked about.
			source.Filter(o =>
			{
				if (Person(o).Name == "Mei")
				{
					throw new InvalidOperationException("predicate bug");
				}

				return Person(o).Role == "Engineer";
			});
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			source.ClearFilter();
			source.Filter(o => Person(o).Role == "Designer");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Ines" },
				"the source must still shape correctly after a predicate threw");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies items added after a filter is declared are tested against that predicate.")]
	public async Task VerifyFilterAppliesToItemsAddedLater()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Filter(o => Person(o).Role == "Engineer"));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GetProjectedCount(tableView), "Baseline: three engineers.");
			items.Add(new ShapedPerson("Nadia", "Engineer", "Platform"));
			items.Add(new ShapedPerson("Tomas", "Designer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Rafa", "Owen", "Nadia" },
				"only the matching addition should enter the projection");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing a filtered-out item does not disturb the projected rows at all.")]
	public async Task VerifyRemovingAFilteredOutItemLeavesProjectionUnchanged()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Filter(o => Person(o).Role == "Engineer"));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GetProjectedCount(tableView), "Baseline: three engineers.");

			// Asha is a designer, so she is not projected.
			var designer = items.First(p => p.Name == "Asha");
			items.Remove(designer);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Rafa", "Owen" },
				"removing an excluded item must not move or drop a visible row");
		});
	}

	#endregion

	#region 8.3 Grouping

	[TestMethod]
	[TestProperty("Description", "Verifies GroupBy projects one header row per distinct key, each followed by its members.")]
	public async Task VerifyGroupByProjectsOneHeaderPerDistinctKey()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var source = TableViewSource.From(MakeShapedPeople()).GroupBy(o => Person(o).Role);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Six people over three roles: three header rows plus six item rows.
			Verify.AreEqual(9, GetProjectedCount(tableView),
				"A grouped projection is headers plus items on one row axis.");

			VerifyProjection(
				tableView,
				new List<string>
				{
					"#Designer(2)", "Asha", "Ines",
					"#Engineer(3)", "Diego", "Rafa", "Owen",
					"#Architect(1)", "Mei",
				},
				"grouped by Role");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies groups are projected in order of first appearance, not sorted by key.")]
	public async Task VerifyGroupByPreservesFirstAppearanceGroupOrder()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// Keys first appear as Designer, Engineer, Architect - deliberately neither
			// alphabetical ascending nor descending, so a key-sorted implementation cannot pass
			// this by coincidence.
			var source = TableViewSource.From(MakeShapedPeople()).GroupBy(o => Person(o).Role);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var keys = GroupKeys(tableView);
			Verify.AreEqual(3, keys.Count, "Three distinct roles means three groups.");
			Verify.AreEqual("Designer", keys[0], "First group must be the first key seen in the source.");
			Verify.AreEqual("Engineer", keys[1], "Second group must be the second key seen in the source.");
			Verify.AreEqual("Architect", keys[2], "Third group must be the third key seen in the source.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a groupIdentitySelector makes reference-type group keys group by value.")]
	public async Task VerifyGroupByWithIdentitySelectorGroupsReferenceKeysByValue()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// A FRESH key instance per item: without the identity selector these six keys are six
			// distinct objects. TableViewSource.idl says the selector is what makes such a key
			// usable at all.
			var source = TableViewSource
				.From(MakeShapedPeople())
				.GroupBy(
					o => new Department { Name = Person(o).DepartmentName },
					key => ((Department)key).Name);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var keys = GroupKeys(tableView);
			Verify.AreEqual(3, keys.Count,
				"Groups must be counted by identity (three department names), not by key instance (six).");
			Verify.AreEqual("Studio", keys[0], "First department by first appearance.");
			Verify.AreEqual("Platform", keys[1], "Second department by first appearance.");
			Verify.AreEqual("Research", keys[2], "Third department by first appearance.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ClearGroupBy removes every header row and restores the flat projection.")]
	public async Task VerifyClearGroupByRestoresFlatRows()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			source = TableViewSource.From(items).GroupBy(o => Person(o).Role);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView), "Baseline: three group headers.");
			source.ClearGroupBy();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(0, GroupHeaderCount(tableView),
				"ClearGroupBy must leave no header row behind on the row axis.");
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(), "after ClearGroupBy");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies GroupBy(null) throws instead of silently ungrouping.")]
	public async Task VerifyNullGroupKeySelectorThrows()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// "keySelector: required, non-null (throws E_INVALIDARG when null; use ClearGroupBy()
			// to remove grouping)."
			Verify.Throws<ArgumentException>(() => source.GroupBy(null),
				"GroupBy(null) must throw; ClearGroupBy() is the documented way to ungroup.");

			source.GroupBy(o => Person(o).Role);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView),
				"A valid GroupBy after the rejected one must still group.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an add whose key matches an existing group joins it without minting a second header.")]
	public async Task VerifyGroupsSurviveAnAddToAnExistingGroup()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).GroupBy(o => Person(o).Role));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView), "Baseline: three groups.");
			items.Add(new ShapedPerson("Nadia", "Engineer", "Platform"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView),
				"An add into an existing group must not create a fourth header.");

			var keys = GroupKeys(tableView);
			Verify.AreEqual("Designer", keys[0], "Group order must survive the add.");
			Verify.AreEqual("Engineer", keys[1], "Group order must survive the add.");
			Verify.AreEqual("Architect", keys[2], "Group order must survive the add.");

			VerifyProjection(
				tableView,
				new List<string>
				{
					"#Designer(2)", "Asha", "Ines",
					"#Engineer(4)", "Diego", "Rafa", "Owen", "Nadia",
					"#Architect(1)", "Mei",
				},
				"after adding an engineer");
		});
	}

	#endregion

	#region 8.4 Sorting

	[TestMethod]
	[TestProperty("Description", "Verifies Sort by property path orders the projection and honours direction.")]
	public async Task VerifySortByPathOrdersRows()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			source = TableViewSource.From(items);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);
		RunOnUIThread.Execute(() => source.Sort("Name", SortDirection.Ascending));
		await SettleLayout(tableView);

		var ascending = items.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, ascending, "sorted ascending by Name");
			source.Sort("Name", SortDirection.Descending);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var descending = new List<string>(ascending);
			descending.Reverse();
			VerifyProjection(tableView, descending, "sorted descending by Name");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a dotted sort path resolves, as the IDL promises for the binding-based evaluator.")]
	public async Task VerifySortByDottedPathOrdersRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// "the same binding-based evaluator a column uses for its SortMemberPath, so a path
			// that displays also sorts (dotted paths and indexers included)".
			var source = TableViewSource.From(MakeShapedPeople()).Sort("Department.Name", SortDirection.Ascending);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Departments: Platform (Diego, Rafa, Owen), Research (Mei), Studio (Asha, Ines).
			// Within one key the sort is stable, so source order is preserved inside each block.
			VerifyProjection(
				tableView,
				new List<string> { "Diego", "Rafa", "Owen", "Mei", "Asha", "Ines" },
				"sorted ascending by Department.Name");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the key-selector Sort overload orders by a computed key no path expresses.")]
	public async Task VerifySortByKeySelectorOrdersRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// Name length is the canonical "key no property path expresses" case from the IDL.
			// Lengths: Asha 4, Diego 5, Mei 3, Rafa 4, Ines 4, Owen 4.
			var source = TableViewSource.From(MakeShapedPeople()).Sort(o => Person(o).Name.Length, SortDirection.Ascending);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(
				tableView,
				new List<string> { "Mei", "Asha", "Rafa", "Ines", "Owen", "Diego" },
				"sorted ascending by computed name length");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies SortDirection.None removes that axis rather than seeding a stage.")]
	public async Task VerifySortDirectionNoneRemovesThatAxis()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			source = TableViewSource.From(items).Sort("Name", SortDirection.Ascending);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha", GetProjectedLabels(tableView, includeCount: true)[0], "Baseline: sorted ascending.");
			source.Sort("Name", SortDirection.None);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// "SortDirection.None removes that key's sort axis rather than seeding a stage, so
			// sorting every axis to None leaves the source unsorted."
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(), "after sorting the only axis to None");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ClearSort drops every sort axis, not only the primary one.")]
	public async Task VerifyClearSortRestoresSourceOrder()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			source = TableViewSource
				.From(items)
				.Sort("Role", SortDirection.Ascending)
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Mei", GetProjectedLabels(tableView, includeCount: true)[0],
				"Baseline: two axes are in force, so Role-ascending puts the Architect first.");
			source.ClearSort();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(),
				"ClearSort must drop BOTH axes, restoring source order");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the first declared sort axis is primary and later axes break ties within it.")]
	public async Task VerifyFirstDeclaredSortAxisIsPrimary()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// "When several axes are active the FIRST one declared is the primary sort and each
			// later axis breaks ties within the previous, matching WPF DataGrid's
			// SortDescriptions order."
			var source = TableViewSource
				.From(MakeShapedPeople())
				.Sort("Role", SortDirection.Ascending)
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Roles ascending: Architect, Designer, Engineer. Names ascending inside each.
			VerifyProjection(
				tableView,
				new List<string> { "Mei", "Asha", "Ines", "Diego", "Owen", "Rafa" },
				"Role primary, Name breaking ties");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies re-sorting an existing path keeps its axis position instead of stacking a new axis.")]
	public async Task VerifyResortingAPathKeepsItsAxisPosition()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource
				.From(MakeShapedPeople())
				.Sort("Role", SortDirection.Ascending)
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// "re-sorting an existing axis keeps its position" + "Re-sorting the same path
			// replaces that axis in place rather than adding a second one."
			source.Sort("Role", SortDirection.Descending);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Role descending: Engineer, Designer, Architect - still PRIMARY, with Name still
			// ascending inside each. If Role had been demoted to last, the projection would lead
			// with Asha (Name ascending primary) instead.
			VerifyProjection(
				tableView,
				new List<string> { "Diego", "Owen", "Rafa", "Asha", "Ines", "Mei" },
				"Role stays primary after being re-sorted descending");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an empty sortMemberPath throws instead of creating an axis that can order nothing.")]
	public async Task VerifyEmptySortMemberPathThrows()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeShapedPeople();
			source = TableViewSource.From(items);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// "Throws E_INVALIDARG when sortMemberPath is empty."
			Verify.Throws<ArgumentException>(() => source.Sort(string.Empty, SortDirection.Ascending),
				"An empty sort path must be rejected.");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, items.Select(p => p.Name).ToList(),
				"the rejected sort must leave the projection untouched");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the key-selector Sort overload rejects a null selector like every other verb.")]
	public async Task VerifyNullSortKeySelectorThrows()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.Throws<ArgumentException>(() => source.Sort((TableViewKeySelector)null, SortDirection.Ascending),
				"The whole-class null contract covers the sort key selector too.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an anonymous key-selector sort clears every column's sort indicator.")]
	public async Task VerifySortByKeySelectorClearsColumnSortIndicators()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var nameColumn = tableView.Columns[0];
			Verify.IsTrue(tableView.SortByColumn(nameColumn, SortDirection.Ascending),
				"Baseline: the control must accept a programmatic column sort.");
			Verify.AreEqual(SortDirection.Ascending, nameColumn.SortDirection,
				"Baseline: the column carries the sort state.");

			// "The axis is ANONYMOUS: nothing names a property, so a bound TableView cannot
			// attribute it to a column and clears every sort indicator instead of leaving one
			// describing a sort that is no longer primary."
			source.Sort(o => Person(o).Name.Length, SortDirection.Ascending);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			foreach (var column in tableView.Columns)
			{
				Verify.AreEqual(SortDirection.None, column.SortDirection,
					"An anonymous source sort must leave no column claiming a sort direction.");
			}
		});
	}

	#endregion

	#region 8.5 Composition

	[TestMethod]
	[TestProperty("Description", "Verifies filter and sort compose: sorted rows drawn only from the filtered set.")]
	public async Task VerifyFilterAndSortCompose()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var source = TableViewSource
				.From(MakeShapedPeople())
				.Filter(o => Person(o).Role == "Engineer")
				.Sort("Name", SortDirection.Descending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Excluded items must influence neither membership nor order: if the sort ran before
			// the filter on the full set, the surviving order would still be this - so the count
			// assertion below carries the membership half.
			VerifyProjection(tableView, new List<string> { "Rafa", "Owen", "Diego" }, "filter + sort");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort declared before GroupBy establishes the order of the groups.")]
	public async Task VerifySortBeforeGroupByOrdersTheGroups()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var source = TableViewSource
				.From(MakeShapedPeople())
				.Sort("Role", SortDirection.Descending)
				.GroupBy(o => Person(o).Role);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var keys = GroupKeys(tableView);
			Verify.AreEqual(3, keys.Count, "Three roles, three groups.");
			Verify.AreEqual("Engineer", keys[0], "Role descending puts Engineer first.");
			Verify.AreEqual("Designer", keys[1], "Role descending puts Designer second.");
			Verify.AreEqual("Architect", keys[2], "Role descending puts Architect last.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort declared after GroupBy orders members inside each group only.")]
	public async Task VerifySortAfterGroupByOrdersWithinEachGroup()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var source = TableViewSource
				.From(MakeShapedPeople())
				.GroupBy(o => Person(o).Role)
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Group order stays first-appearance; only the members reorder. A post-group sort that
			// leaked across buckets would put Asha, Diego, Ines... in one run.
			VerifyProjection(
				tableView,
				new List<string>
				{
					"#Designer(2)", "Asha", "Ines",
					"#Engineer(3)", "Diego", "Owen", "Rafa",
					"#Architect(1)", "Mei",
				},
				"GroupBy then Sort");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies filter, group and sort compose into one projection, asserted element by element.")]
	public async Task VerifyFilterGroupAndSortCompose()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var source = TableViewSource
				.From(MakeShapedPeople())
				.Filter(o => Person(o).Role != "Architect")
				.GroupBy(o => Person(o).DepartmentName)
				.Sort("Name", SortDirection.Descending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Mei (Architect, Research) is filtered out, which also removes the Research group
			// entirely. Remaining: Studio (Asha, Ines) and Platform (Diego, Rafa, Owen), groups in
			// first-appearance order, members descending by name.
			VerifyProjection(
				tableView,
				new List<string>
				{
					"#Studio(2)", "Ines", "Asha",
					"#Platform(3)", "Rafa", "Owen", "Diego",
				},
				"filter + group + sort");
		});
	}

	#endregion

	#region 8.6 Live updates while shaped

	[TestMethod]
	[TestProperty("Description", "Verifies an add while filtered enters the projection only when it matches.")]
	public async Task VerifyAddWhileFilteredRespectsThePredicate()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Filter(o => Person(o).Role == "Engineer"));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			items.Insert(0, new ShapedPerson("Nadia", "Engineer", "Platform"));
			items.Insert(0, new ShapedPerson("Tomas", "Designer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Nadia", "Diego", "Rafa", "Owen" },
				"only the matching insert should appear, at its source position");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an add while sorted lands at its sorted position, not at the source index.")]
	public async Task VerifyAddWhileSortedLandsAtTheSortedPosition()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Sort("Name", SortDirection.Ascending));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Appended at the END of the source, but "Bea" belongs second in ascending order.
			items.Add(new ShapedPerson("Bea", "Engineer", "Platform"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(
				tableView,
				new List<string> { "Asha", "Bea", "Diego", "Ines", "Mei", "Owen", "Rafa" },
				"an append under an active sort must be placed by key");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an add with an unseen key creates its group without disturbing the others.")]
	public async Task VerifyAddWhileGroupedCreatesTheMissingGroup()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).GroupBy(o => Person(o).Role));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			items.Add(new ShapedPerson("Nadia", "Researcher", "Research"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var keys = GroupKeys(tableView);
			Verify.AreEqual(4, keys.Count, "An unseen key must mint exactly one new group.");
			Verify.AreEqual("Designer", keys[0], "Existing groups keep their relative order.");
			Verify.AreEqual("Engineer", keys[1], "Existing groups keep their relative order.");
			Verify.AreEqual("Architect", keys[2], "Existing groups keep their relative order.");
			Verify.AreEqual("Researcher", keys[3], "The new group is appended in first-appearance order.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a removal while shaped drops exactly that row and moves no other.")]
	public async Task VerifyRemoveWhileShapedDropsOnlyThatRow()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource
				.From(items)
				.Filter(o => Person(o).Role != "Architect")
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Diego", "Ines", "Owen", "Rafa" }, "baseline");
			items.Remove(items.First(p => p.Name == "Ines"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Diego", "Owen", "Rafa" },
				"a removal must be local: the surviving rows keep their order");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a replacement is re-evaluated as a new item, not given the old item's slot.")]
	public async Task VerifyReplaceWhileShapedReevaluatesTheNewItem()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource
				.From(items)
				.Filter(o => Person(o).Role == "Engineer")
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Owen", "Rafa" }, "baseline");

			// Replace the engineer Rafa with a designer: the replacement fails the filter, so the
			// slot must be vacated rather than inherited.
			var index = items.IndexOf(items.First(p => p.Name == "Rafa"));
			items[index] = new ShapedPerson("Rafa", "Designer", "Studio");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Owen" },
				"the replacement must be filtered on its own values");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a source move does not leak into a sorted projection.")]
	public async Task VerifyMoveWhileSortedDoesNotChangeProjectedOrder()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;
		List<string> before = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Sort("Name", SortDirection.Ascending));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			before = GetProjectedLabels(tableView, includeCount: true);
			items.Move(0, items.Count - 1);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, before, "a source move under an active sort must be invisible");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a Reset while shaped rebuilds the new contents under the same filter and sort.")]
	public async Task VerifyResetWhileShapedRebuildsUnderTheSameShape()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource
				.From(items)
				.Filter(o => Person(o).Role == "Engineer")
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Owen", "Rafa" }, "baseline");

			// Clear() raises Reset, which is the atomic-rebuild path rather than an incremental one.
			items.Clear();
			items.Add(new ShapedPerson("Zoe", "Engineer", "Platform"));
			items.Add(new ShapedPerson("Tomas", "Designer", "Studio"));
			items.Add(new ShapedPerson("Bea", "Engineer", "Platform"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Bea", "Zoe" },
				"the declared shape must survive a Reset");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: the sorted flat path applies a later collection change as an incremental insert, so a stale row is never re-shaped. Re-enable once that path re-shapes.
	[Ignore("WinUI ignores this test: Product bug: the sorted flat path applies a later collection change as an incremental insert, so a stale row is never re-shaped. Re-enable once that path re-shapes.")]
	[TestProperty("Description", "Verifies an INotifyPropertyChanged-only sort key change defers the move until the next collection change.")]
	public async Task VerifySortKeyPropertyChangeDoesNotMoveTheRowUntilACollectionChange()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Sort("Name", SortDirection.Ascending));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Diego", "Ines", "Mei", "Owen", "Rafa" }, "baseline");

			// ShapedItemsSource states this as an invariant: "An in-place mutation of a row's
			// sort-key field that raises only INotifyPropertyChanged leaves the row at its old
			// sort position until the next collection change, matching XAML ItemsControl sources."
			items.First(p => p.Name == "Asha").Name = "Zara";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Zara", "Diego", "Ines", "Mei", "Owen", "Rafa" },
				"the renamed row must stay put until a collection change");

			// Any collection change re-shapes.
			items.Add(new ShapedPerson("Bea", "Engineer", "Platform"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Bea", "Diego", "Ines", "Mei", "Owen", "Rafa", "Zara" },
				"the next collection change must re-shape the renamed row into position");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies filter membership follows the same deferred re-shape rule as sort position.")]
	public async Task VerifyFilterMembershipPropertyChangeFollowsTheSameInvariant()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Filter(o => Person(o).Role == "Engineer"));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Rafa", "Owen" }, "baseline");

			// Asha becomes an engineer through INPC only. Under the stated invariant this does not
			// change membership yet.
			items.First(p => p.Name == "Asha").Role = "Engineer";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Diego", "Rafa", "Owen" },
				"membership must not change on an INPC-only edit");

			items.Add(new ShapedPerson("Bea", "Engineer", "Platform"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new List<string> { "Asha", "Diego", "Rafa", "Owen", "Bea" },
				"the next collection change must re-evaluate membership");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group key change follows the same deferred re-shape rule.")]
	public async Task VerifyGroupKeyPropertyChangeFollowsTheSameInvariant()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).GroupBy(o => Person(o).Role));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView), "Baseline: three groups.");
			items.First(p => p.Name == "Mei").Role = "Engineer";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// A half-moved row - item relocated but header counts stale - is the corruption this
			// test exists to catch, so both halves are asserted.
			VerifyProjection(
				tableView,
				new List<string>
				{
					"#Designer(2)", "Asha", "Ines",
					"#Engineer(3)", "Diego", "Rafa", "Owen",
					"#Architect(1)", "Mei",
				},
				"an INPC-only group key change must not move the row yet");

			items.Add(new ShapedPerson("Bea", "Designer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(
				tableView,
				new List<string>
				{
					"#Designer(3)", "Asha", "Ines", "Bea",
					// The grouped path rebuilds every bucket from source order, so the re-bucketed
					// row takes its source position inside the group, it is not appended.
					"#Engineer(4)", "Diego", "Mei", "Rafa", "Owen",
				},
				"the next collection change must re-bucket the row and refresh the counts");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a burst of mutations under shaping settles to the shape of the final source contents.")]
	public async Task VerifyBurstOfMutationsWhileShapedSettlesCorrectly()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource
				.From(items)
				.Filter(o => Person(o).Role == "Engineer")
				.Sort("Name", SortDirection.Ascending);

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Twelve interleaved mutations in one UI-thread turn, exercising the coalescing and
			// re-entrancy guards rather than any single path.
			items.Add(new ShapedPerson("Zoe", "Engineer", "Platform"));
			items.Insert(0, new ShapedPerson("Bea", "Engineer", "Platform"));
			items.Add(new ShapedPerson("Tomas", "Designer", "Studio"));
			items.RemoveAt(items.Count - 1);
			items.Insert(3, new ShapedPerson("Cy", "Engineer", "Research"));
			items[0] = new ShapedPerson("Bea", "Designer", "Studio");
			items.Add(new ShapedPerson("Ada", "Engineer", "Research"));
			items.Remove(items.First(p => p.Name == "Rafa"));
			items.Move(0, items.Count - 1);
			items.Add(new ShapedPerson("Kit", "Architect", "Research"));
			items[1] = new ShapedPerson("Nadia", "Engineer", "Platform");
			items.Add(new ShapedPerson("Uma", "Engineer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Expected is computed from the FINAL source, so the test stays true if the burst
			// above is edited.
			var expected = items
				.Where(p => p.Role == "Engineer")
				.Select(p => p.Name)
				.OrderBy(n => n, StringComparer.Ordinal)
				.ToList();

			VerifyProjection(tableView, expected, "after a burst of twelve mutations");
		});
	}

	#endregion

	#region 8.7 Identity and state

	[TestMethod]
	[TestProperty("Description", "Verifies the same item object on two rows fails fast once a shaping verb needs identity.")]
	public async Task VerifyDuplicateItemObjectThrowsWhenShaped()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			var duplicate = new ShapedPerson("Asha", "Engineer", "Studio");
			var items = new List<ShapedPerson>
			{
				duplicate,
				new ShapedPerson("Diego", "Engineer", "Platform"),
				duplicate,
			};

			// From alone is a plain mirror and needs no identity, so the failure must come from the
			// verb - TableViewSource.idl: "the same object in two rows fails fast at
			// materialization".
			source = TableViewSource.From(items);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.Throws<ArgumentException>(() => source.Filter(o => Person(o).Role == "Engineer"),
				"Shaping a source whose rows share one object must fail fast, not project two indistinguishable rows.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies selection re-anchors to the same item object across a reshape.")]
	public async Task VerifySelectionReanchorsAcrossAReshape()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ShapedPerson selected = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Index 0 is Asha in source order; ascending-by-name she is still first, so sort
			// DESCENDING to guarantee the row actually moves.
			tableView.Select(0);
			selected = tableView.SelectedItem as ShapedPerson;
			Verify.IsNotNull(selected, "Baseline: selecting index 0 must report an item.");
			Verify.AreEqual("Asha", selected.Name, "Baseline: index 0 is the first source item.");

			source.Sort("Name", SortDirection.Descending);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(selected, tableView.SelectedItem,
				"Selection is anchored to the item OBJECT, so a reshape must not change which item is selected.");

			var labels = GetProjectedLabels(tableView, includeCount: true);
			Verify.AreEqual(labels.IndexOf("Asha"), tableView.SelectedIndex,
				"SelectedIndex must follow the item to its new projected position.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies selection does not follow an equal-valued replacement instance.")]
	public async Task VerifySelectionIsNotReanchoredAcrossItemRecreation()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;
		ShapedPerson replacement = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Sort("Name", SortDirection.Ascending));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(0);
			Verify.AreEqual("Asha", (tableView.SelectedItem as ShapedPerson)?.Name,
				"Baseline: the first ascending row is selected.");

			// Same values, different object. The IDL says identity does NOT survive an item being
			// re-created.
			replacement = new ShapedPerson("Asha", "Designer", "Studio");
			items[items.IndexOf(items.First(p => p.Name == "Asha"))] = replacement;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Only the negative is asserted: the IDL does not say whether the selection clears or
			// moves elsewhere, so pinning which would freeze an unspecified choice.
			Verify.IsFalse(ReferenceEquals(replacement, tableView.SelectedItem),
				"Row identity is object identity, so a re-created item must not inherit the selection.");
		});
	}

	#endregion
}

internal static class TableViewShapingTestHelpers
{
	// Six people over three roles and three departments, in an order that is sorted by none of
	// them, so an accidental default shape shows up immediately.
	internal static List<ShapedPerson> MakeShapedPeople() => new List<ShapedPerson>
	{
		new ShapedPerson("Asha", "Designer", "Studio"),
		new ShapedPerson("Diego", "Engineer", "Platform"),
		new ShapedPerson("Mei", "Architect", "Research"),
		new ShapedPerson("Rafa", "Engineer", "Platform"),
		new ShapedPerson("Ines", "Designer", "Studio"),
		new ShapedPerson("Owen", "Engineer", "Platform"),
	};

	internal static ShapedPerson Person(object item) => (ShapedPerson)item;

	// Tall enough that every row in these fixtures realizes, so TryGetElement never legitimately
	// returns null.
	internal static TableView CreateShapedTable(TableViewSource source)
		=> CreateTableView(source, headers: new[] { "Name", "Role" }, width: 500, height: 700);

	internal static int GroupHeaderCount(TableView tableView)
		=> GetProjectedLabels(tableView, includeCount: true).Count(label => label.StartsWith("#", StringComparison.Ordinal));

	// The group keys in projected order, without their counts.
	internal static List<string> GroupKeys(TableView tableView)
		=> GetProjectedLabels(tableView, includeCount: true)
			.Where(label => label.StartsWith("#", StringComparison.Ordinal))
			.Select(label => label.Substring(1, label.IndexOf('(') - 1))
			.ToList();

	internal static void VerifyProjection(TableView tableView, IList<string> expected, string context)
		=> VerifySequence(expected, GetProjectedLabels(tableView, includeCount: true), context);
}

// A reference-type group key, used to prove the groupIdentitySelector overload.
internal sealed class Department
{
	public string Name { get; set; }

	public override string ToString() => Name;
}

internal sealed class ShapedPerson : INotifyPropertyChanged
{
	private string m_name;
	private string m_role;
	private string m_departmentName;

	public ShapedPerson(string name, string role, string departmentName)
	{
		m_name = name;
		m_role = role;
		m_departmentName = departmentName;
		Department = new Department { Name = departmentName };
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public string Name
	{
		get => m_name;
		set
		{
			if (m_name != value)
			{
				m_name = value;
				Raise(nameof(Name));
			}
		}
	}

	public string Role
	{
		get => m_role;
		set
		{
			if (m_role != value)
			{
				m_role = value;
				Raise(nameof(Role));
			}
		}
	}

	public string DepartmentName
	{
		get => m_departmentName;
		set
		{
			if (m_departmentName != value)
			{
				m_departmentName = value;
				Department = new Department { Name = value };
				Raise(nameof(DepartmentName));
				Raise(nameof(Department));
			}
		}
	}

	// Nested object, so "Department.Name" exercises the dotted-path evaluator.
	public Department Department { get; private set; }

	private void Raise(string propertyName)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
