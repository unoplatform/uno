#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Tabular;

[TestClass]
public class Given_ShapingPipeline
{
	private sealed record Entry(string Name, int Rank);

	private static readonly ShapingHelpers.KeySelector s_byRank = item => ((Entry)item!).Rank;
	private static readonly ShapingHelpers.KeySelector s_byName = item => ((Entry)item!).Name;

	private static List<object?> Rows(params Entry[] entries) => entries.Cast<object?>().ToList();

	private static string[] Names(List<object?> rows) => rows.Select(r => ((Entry)r!).Name).ToArray();

	[TestMethod]
	public void When_No_Filter_Passes_Everything()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();

		Assert.IsFalse(pipeline.HasFilter());
		Assert.IsTrue(pipeline.PassesFilter(new Entry("a", 1)));
		Assert.IsTrue(pipeline.PassesFilter(null));

		var rows = Rows(new("a", 1), new("b", 2));
		pipeline.ApplyFilter(rows);
		Assert.AreEqual(2, rows.Count);
	}

	[TestMethod]
	public void When_Filter_Keeps_Source_Order()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetFilter(item => ((Entry)item!).Rank % 2 == 0);

		var rows = Rows(new("a", 4), new("b", 1), new("c", 2), new("d", 3), new("e", 0));
		pipeline.ApplyFilter(rows);

		CollectionAssert.AreEqual(new[] { "a", "c", "e" }, Names(rows));
	}

	[TestMethod]
	public void When_Filter_Throws_Item_Is_Excluded()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetFilter(item => ((Entry)item!).Rank > 0 ? true : throw new InvalidOperationException());

		Assert.IsFalse(pipeline.PassesFilter(new Entry("bad", 0)), "a throwing predicate is fail-safe");
		Assert.IsTrue(pipeline.PassesFilter(new Entry("good", 1)));
	}

	[TestMethod]
	public void When_Filter_Axes_Are_Conjunctive()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetFilter("rank", item => ((Entry)item!).Rank > 1);
		pipeline.SetFilter("name", item => ((Entry)item!).Name != "c");

		var rows = Rows(new("a", 1), new("b", 2), new("c", 3), new("d", 4));
		pipeline.ApplyFilter(rows);
		CollectionAssert.AreEqual(new[] { "b", "d" }, Names(rows));

		// A null predicate removes the axis instead of declaring an always-false one.
		pipeline.SetFilter("name", null);
		rows = Rows(new("a", 1), new("b", 2), new("c", 3));
		pipeline.ApplyFilter(rows);
		CollectionAssert.AreEqual(new[] { "b", "c" }, Names(rows));

		pipeline.ClearFilter("rank");
		Assert.IsFalse(pipeline.HasFilter());
	}

	[TestMethod]
	public void When_Untokenized_Filter_Replaces()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetFilter(item => ((Entry)item!).Rank == 1);
		pipeline.SetFilter(item => ((Entry)item!).Rank == 2);

		var rows = Rows(new("a", 1), new("b", 2));
		pipeline.ApplyFilter(rows);
		CollectionAssert.AreEqual(new[] { "b" }, Names(rows));

		pipeline.ClearFilter();
		Assert.IsFalse(pipeline.HasFilter());
	}

	[TestMethod]
	[DataRow(SortDirection.Ascending, new[] { "b1", "b2", "b3", "a1", "a2" })]
	[DataRow(SortDirection.Descending, new[] { "a1", "a2", "b1", "b2", "b3" })]
	public void When_Sort_Is_Stable(SortDirection direction, string[] expected)
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetSort("", "rank", s_byRank, null, "Rank", direction);

		// Ties keep their source order in both directions.
		var rows = Rows(new("a1", 2), new("b1", 1), new("b2", 1), new("a2", 2), new("b3", 1));
		pipeline.ApplySort(rows);

		CollectionAssert.AreEqual(expected, Names(rows));
	}

	[TestMethod]
	public void When_Sort_Direction_None_Removes_Axis()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Ascending);
		Assert.IsTrue(pipeline.HasActiveSort());

		pipeline.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.None);
		Assert.IsFalse(pipeline.HasActiveSort());

		var rows = Rows(new("b", 2), new("a", 1));
		pipeline.ApplySort(rows);
		CollectionAssert.AreEqual(new[] { "b", "a" }, Names(rows), "no active axis leaves the order untouched");
	}

	[TestMethod]
	public void When_Sort_Precedence_Is_Declaration_Order()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Ascending);
		pipeline.SetSort("", "name", s_byName, null, "Name", SortDirection.Descending);

		var axes = pipeline.ActiveSortAxes(-1, -1);
		Assert.AreEqual(2, axes.Count);
		Assert.AreEqual("rank", axes[0].AxisToken, "the first declared axis is primary");
		Assert.AreEqual("Rank", axes[0].SortMemberPath);
		Assert.AreEqual("name", axes[1].AxisToken);

		var rows = Rows(new("a", 2), new("b", 1), new("c", 2), new("d", 1));
		pipeline.ApplySort(rows);
		CollectionAssert.AreEqual(new[] { "d", "b", "c", "a" }, Names(rows));

		// Re-sorting an existing axis keeps its precedence slot.
		pipeline.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Descending);
		axes = pipeline.ActiveSortAxes(-1, -1);
		Assert.AreEqual("rank", axes[0].AxisToken);
		Assert.AreEqual(SortDirection.Descending, axes[0].Direction);

		rows = Rows(new("a", 2), new("b", 1), new("c", 2), new("d", 1));
		pipeline.ApplySort(rows);
		CollectionAssert.AreEqual(new[] { "c", "a", "d", "b" }, Names(rows));
	}

	[TestMethod]
	public void When_Previous_Axis_Token_Is_Replaced()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetSort("", "column:1", s_byRank, null, "Rank", SortDirection.Ascending);

		// A column re-keying itself drops its previous axis first.
		pipeline.SetSort("column:1", "column:2", s_byName, null, "Name", SortDirection.Ascending);

		var axes = pipeline.ActiveSortAxes(-1, -1);
		Assert.AreEqual(1, axes.Count);
		Assert.AreEqual("column:2", axes[0].AxisToken);
	}

	[TestMethod]
	public void When_ClearSortsExcept()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetSort("", "a", s_byRank, null, "", SortDirection.Ascending);
		pipeline.SetSort("", "b", s_byName, null, "", SortDirection.Ascending);
		pipeline.SetSort("", "", s_byName, s_byName, "", SortDirection.Descending);

		pipeline.ClearSortsExcept("b");

		var axes = pipeline.ActiveSortAxes(-1, -1);
		Assert.AreEqual(1, axes.Count);
		Assert.AreEqual("b", axes[0].AxisToken);

		pipeline.ClearSort("b");
		Assert.IsFalse(pipeline.HasActiveSort());
	}

	[TestMethod]
	public void When_String_Keys_Sort_By_Current_Culture()
	{
		var pipeline = new ShapingHelpers.ShapingPipeline();
		pipeline.SetSort("", "name", s_byName, null, "Name", SortDirection.Ascending);

		var rows = Rows(new("delta", 0), new("Charlie", 0), new("bravo", 0), new("Alpha", 0));
		pipeline.ApplySort(rows);

		CollectionAssert.AreEqual(new[] { "Alpha", "bravo", "Charlie", "delta" }, Names(rows));
	}
}
