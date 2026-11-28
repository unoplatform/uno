#nullable enable

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Helpers;

[UITestClass]
public class Given_SnapshotArray
{
	private sealed class Item(int id)
	{
		public int Id { get; } = id;

		public override string ToString() => Id.ToString();
	}

	private static Item[] Items(Item?[] array) => array.Take(SnapshotArray.CountOf(array)).Select(i => i!).ToArray();

	[TestMethod]
	public void When_Added_Then_Counted_In_Order()
	{
		var field = Array.Empty<Item?>();
		var items = Enumerable.Range(0, 100).Select(i => new Item(i)).ToArray();

		for (var i = 0; i < items.Length; i++)
		{
			SnapshotArray.Add(ref field, items[i]);
			Assert.AreEqual(i + 1, SnapshotArray.CountOf(field));
		}

		CollectionAssert.AreEqual(items, Items(field));
	}

	[TestMethod]
	public void When_Few_Items_Then_The_Array_Is_Exact()
	{
		var field = Array.Empty<Item?>();
		for (var i = 1; i <= 8; i++)
		{
			SnapshotArray.Add(ref field, new Item(i));
			Assert.AreEqual(i, field.Length, "Up to eight items, as a copy per append would hold");
		}

		SnapshotArray.Add(ref field, new Item(9));
		Assert.AreEqual(16, field.Length, "Doubling after");
	}

	[TestMethod]
	public void When_Appended_After_A_Snapshot_Then_The_Snapshot_Is_Unchanged()
	{
		var field = Array.Empty<Item?>();
		for (var i = 0; i < 10; i++)
		{
			SnapshotArray.Add(ref field, new Item(i));
		}

		var snapshot = field;
		var count = SnapshotArray.CountOf(snapshot);
		var expected = Items(snapshot);

		SnapshotArray.Add(ref field, new Item(10));

		Assert.AreSame(snapshot, field, "Appended in place");
		Assert.AreEqual(10, count);
		CollectionAssert.AreEqual(expected, snapshot.Take(count).ToArray());
		Assert.AreEqual(11, SnapshotArray.CountOf(field));
	}

	[TestMethod]
	public void When_Removed_After_A_Snapshot_Then_The_Snapshot_Is_Unchanged()
	{
		var field = Array.Empty<Item?>();
		var items = Enumerable.Range(0, 10).Select(i => new Item(i)).ToArray();
		foreach (var item in items)
		{
			SnapshotArray.Add(ref field, item);
		}

		var snapshot = field;
		SnapshotArray.Remove(ref field, items[3]);

		Assert.AreNotSame(snapshot, field, "A removal publishes a new array");
		CollectionAssert.AreEqual(items, Items(snapshot));
		CollectionAssert.AreEqual(items.Where((_, i) => i != 3).ToArray(), Items(field));
	}

	[TestMethod]
	public void When_Removed_Past_Eight_Then_The_Next_Add_Fills_The_Spare_Slot()
	{
		// A list recycling its containers removes one and adds one, over and over.
		var field = Array.Empty<Item?>();
		var items = Enumerable.Range(0, 10).Select(i => new Item(i)).ToArray();
		foreach (var item in items)
		{
			SnapshotArray.Add(ref field, item);
		}

		SnapshotArray.Remove(ref field, items[4]);
		var afterRemoval = field;
		Assert.AreEqual(9, SnapshotArray.CountOf(field));
		Assert.AreEqual(10, field.Length, "One spare slot kept");

		var added = new Item(10);
		SnapshotArray.Add(ref field, added);
		Assert.AreSame(afterRemoval, field, "The add fills the spare slot in place");
		CollectionAssert.AreEqual(items.Where((_, i) => i != 4).Append(added).ToArray(), Items(field));
	}

	[TestMethod]
	public void When_Removed_From_Nine_Then_One_Slot_Is_Kept()
	{
		var field = Array.Empty<Item?>();
		var items = Enumerable.Range(0, 9).Select(i => new Item(i)).ToArray();
		foreach (var item in items)
		{
			SnapshotArray.Add(ref field, item);
		}

		SnapshotArray.Remove(ref field, items[0]);
		Assert.AreEqual(8, SnapshotArray.CountOf(field));
		Assert.AreEqual(9, field.Length);
	}

	[TestMethod]
	public void When_Removed_Up_To_Eight_Then_The_Array_Is_Exact()
	{
		var field = Array.Empty<Item?>();
		var items = Enumerable.Range(0, 8).Select(i => new Item(i)).ToArray();
		foreach (var item in items)
		{
			SnapshotArray.Add(ref field, item);
		}

		SnapshotArray.Remove(ref field, items[0]);
		Assert.AreEqual(7, field.Length);
	}

	[TestMethod]
	public void When_Last_Item_Removed_Then_Empty()
	{
		var field = Array.Empty<Item?>();
		var item = new Item(0);
		SnapshotArray.Add(ref field, item);
		SnapshotArray.Remove(ref field, item);

		Assert.AreSame(Array.Empty<Item?>(), field);
		Assert.AreEqual(0, SnapshotArray.CountOf(field));
	}

	[TestMethod]
	public void When_Removing_An_Absent_Item_Then_Unchanged()
	{
		var field = Array.Empty<Item?>();
		SnapshotArray.Add(ref field, new Item(0));
		var before = field;

		SnapshotArray.Remove(ref field, new Item(1));

		Assert.AreSame(before, field);
	}

	[TestMethod]
	public void When_Appending_To_A_Snapshot_Taken_Before_A_Removal_Then_It_Is_Not_Written()
	{
		var field = Array.Empty<Item?>();
		var items = Enumerable.Range(0, 12).Select(i => new Item(i)).ToArray();
		foreach (var item in items)
		{
			SnapshotArray.Add(ref field, item);
		}

		var snapshot = field;
		SnapshotArray.Remove(ref field, items[0]);
		SnapshotArray.Add(ref field, new Item(12));

		CollectionAssert.AreEqual(items, Items(snapshot), "The array a loop still reads is never written again");
	}
}
