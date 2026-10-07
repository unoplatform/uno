#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.Tabular;

// Flat projections only: the grouped projection goes through GroupedSourceAdapter, which must be
// constructed on a UI thread, so grouping is covered by the TableView runtime tests.
[TestClass]
public class Given_ShapedItemsSource
{
	private sealed class Entry
	{
		public Entry(string name, int rank)
		{
			Name = name;
			Rank = rank;
		}

		public string Name { get; }

		public int Rank { get; }
	}

	private static readonly ShapingHelpers.KeySelector s_byRank = item => ((Entry)item!).Rank;

	private static ShapedItemsSource Start(object source, out List<CollectionChange> changes)
	{
		var shaped = new ShapedItemsSource(source);
		shaped.Start();

		var recorded = new List<CollectionChange>();
		shaped.Rows()!.VectorChanged += (_, e) => recorded.Add(e.CollectionChange);
		changes = recorded;
		return shaped;
	}

	private static string[] Names(ShapedItemsSource shaped) => shaped.Rows()!.Select(r => ((Entry)r!).Name).ToArray();

	[TestMethod]
	public void When_No_Verb_Mirrors_Source()
	{
		var shaped = Start(new List<Entry> { new("a", 2), new("b", 1) }, out _);

		CollectionAssert.AreEqual(new[] { "a", "b" }, Names(shaped));
		Assert.IsFalse(shaped.IsProjectedAsGrouped());
	}

	[TestMethod]
	public void When_Filter_Single_Reset()
	{
		var shaped = Start(new List<Entry> { new("a", 2), new("b", 1), new("c", 3) }, out var changes);

		shaped.SetFilter(item => ((Entry)item!).Rank > 1);

		CollectionAssert.AreEqual(new[] { "a", "c" }, Names(shaped));
		CollectionAssert.AreEqual(new[] { CollectionChange.Reset }, changes, "one Reset per refresh");
	}

	[TestMethod]
	public void When_Sort_Is_Stable()
	{
		var shaped = Start(
			new List<Entry> { new("a1", 2), new("b1", 1), new("a2", 2), new("b2", 1) },
			out var changes);

		shaped.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Ascending);

		CollectionAssert.AreEqual(new[] { "b1", "b2", "a1", "a2" }, Names(shaped));
		Assert.AreEqual(1, changes.Count(c => c == CollectionChange.Reset));

		var info = shaped.ActiveSortAxisInfos().Single();
		Assert.AreEqual("rank", info.AxisToken);
		Assert.AreEqual("Rank", info.SortMemberPath);
		Assert.AreEqual(SortDirection.Ascending, info.Direction);

		// Clearing restores source order: the sort re-seats on the retained source-order membership.
		shaped.ClearSorts();
		CollectionAssert.AreEqual(new[] { "a1", "b1", "a2", "b2" }, Names(shaped));
	}

	[TestMethod]
	public void When_DeferRefresh_Batches_Verbs()
	{
		var shaped = Start(
			new List<Entry> { new("a", 3), new("b", 1), new("c", 2), new("d", 0) },
			out var changes);

		var shapingChanged = 0;
		shaped.SetShapingChangedHandler(_ => shapingChanged++);

		using (shaped.DeferRefresh())
		{
			shaped.SetFilter(item => ((Entry)item!).Rank > 0);
			shaped.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Descending);

			Assert.AreEqual(0, changes.Count, "nothing is published inside the batch");
		}

		CollectionAssert.AreEqual(new[] { "a", "c", "b" }, Names(shaped));
		CollectionAssert.AreEqual(new[] { CollectionChange.Reset }, changes, "the whole batch reads as one delta");
		Assert.AreEqual(1, shapingChanged);
	}

	[TestMethod]
	public void When_Clear_On_Nothing_Is_NoOp()
	{
		var shaped = Start(new List<Entry> { new("a", 1) }, out var changes);

		// The first commit has no baseline to diff against, so it always reshapes.
		shaped.ClearFilter();
		changes.Clear();

		shaped.ClearFilter();
		shaped.ClearSorts();

		Assert.AreEqual(0, changes.Count, "re-declaring the identical shape must not Reset");
	}

	[TestMethod]
	public void When_Source_Changes_Incrementally()
	{
		var source = new ObservableCollection<Entry> { new("a", 1), new("c", 3) };
		var shaped = Start(source, out var changes);

		// Unshaped: a single Add maps to the same index.
		source.Add(new Entry("d", 4));
		CollectionAssert.AreEqual(new[] { "a", "c", "d" }, Names(shaped));
		CollectionAssert.AreEqual(new[] { CollectionChange.ItemInserted }, changes);

		// Sorted: a single Add is placed by binary search instead of a rebuild.
		shaped.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Ascending);
		changes.Clear();

		source.Add(new Entry("b", 2));
		CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, Names(shaped));
		CollectionAssert.AreEqual(new[] { CollectionChange.ItemInserted }, changes);

		// Sorted: a single Remove drops the tracked row instead of a rebuild.
		changes.Clear();
		source.RemoveAt(0);
		CollectionAssert.AreEqual(new[] { "b", "c", "d" }, Names(shaped));
		CollectionAssert.AreEqual(new[] { CollectionChange.ItemRemoved }, changes);

		// Sorted: an Add that ties an existing key and is not the last source item cannot be placed
		// incrementally (source order decides the tie), so it falls back to one Reset.
		changes.Clear();
		source.Insert(0, new Entry("x", 3));
		CollectionAssert.AreEqual(new[] { "b", "x", "c", "d" }, Names(shaped), "the tie keeps source order");
		CollectionAssert.AreEqual(new[] { CollectionChange.Reset }, changes);
	}

	[TestMethod]
	public void When_Same_Object_On_Two_Rows_With_A_Verb()
	{
		var shared = new Entry("a", 1);
		var shaped = Start(new List<Entry> { shared, new("b", 2), shared }, out _);

		// No verb: a plain mirror needs no identity.
		Assert.AreEqual(3, shaped.Rows()!.Count);

		// A verb requires object identity, which two rows backed by one object cannot provide.
		Assert.ThrowsExactly<ArgumentException>(() => shaped.SetSort("", "rank", s_byRank, null, "Rank", SortDirection.Ascending));
	}

	private static readonly ShapingHelpers.KeySelector s_byValue = item => item;

	[TestMethod]
	public void When_Equal_Strings_On_Two_Rows_With_A_Verb()
	{
		var source = new ObservableCollection<string> { "b", "a", "b" };
		var shaped = Start(source, out _);

		// Each read of a string row through the WinRT ABI is a new IPropertyValue box, so equal strings
		// are distinct rows, unlike one object shared by two rows.
		shaped.SetSort("", "value", s_byValue, null, "", SortDirection.Ascending);

		CollectionAssert.AreEqual(new[] { "a", "b", "b" }, shaped.Rows()!.Cast<string>().ToArray());
	}

	[TestMethod]
	public void When_Sorted_String_Row_Removed()
	{
		var source = new ObservableCollection<string> { "c", "a", "b" };
		var shaped = Start(source, out var changes);
		shaped.SetSort("", "value", s_byValue, null, "", SortDirection.Ascending);
		changes.Clear();

		// The removed item is a fresh box with no tracked identity, so the splice falls back to a Reset.
		source.RemoveAt(1);

		CollectionAssert.AreEqual(new[] { "b", "c" }, shaped.Rows()!.Cast<string>().ToArray());
		CollectionAssert.AreEqual(new[] { CollectionChange.Reset }, changes);
	}
}
