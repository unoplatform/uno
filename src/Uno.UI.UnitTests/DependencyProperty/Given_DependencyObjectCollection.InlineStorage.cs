#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.BinderTests
{
	public partial class Given_DependencyObjectCollection
	{
		private static MyDependencyObject[] CreateItems(int count)
			=> Enumerable.Range(0, count).Select(_ => new MyDependencyObject()).ToArray();

		private static DependencyObjectCollection CreateCollection(params DependencyObject[] items)
		{
			var collection = new DependencyObjectCollection();

			foreach (var item in items)
			{
				collection.Add(item);
			}

			return collection;
		}

		private static List<DependencyObject> ToListFast(DependencyObjectCollection collection)
		{
			var result = new List<DependencyObject>();
			var enumerator = collection.GetEnumeratorFast();

			while (enumerator.MoveNext())
			{
				result.Add(enumerator.Current);
			}

			return result;
		}

		private static void AssertContent(DependencyObjectCollection SUT, params DependencyObject[] expected)
		{
			Assert.AreEqual(expected.Length, SUT.Count);
			Assert.AreEqual((uint)expected.Length, SUT.Size);

			for (var i = 0; i < expected.Length; i++)
			{
				Assert.AreSame(expected[i], SUT[i], $"Item at index {i}");
				Assert.AreEqual(i, SUT.IndexOf(expected[i]));
				Assert.IsTrue(SUT.Contains(expected[i]));
			}

			Assert.IsNull(SUT[expected.Length]);

			CollectionAssert.AreEqual(expected, SUT.ToArray());
			CollectionAssert.AreEqual(expected, ToListFast(SUT));

			var nonGeneric = new List<object>();
			foreach (var item in (IEnumerable)SUT)
			{
				nonGeneric.Add(item);
			}

			CollectionAssert.AreEqual(expected, nonGeneric);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		[DataRow(7)]
		public void When_Add_Items_Of_Any_Size(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			AssertContent(SUT, items);
			Assert.IsFalse(SUT.IsReadOnly);
		}

		[TestMethod]
		public void When_Empty()
		{
			var SUT = new DependencyObjectCollection();

			Assert.AreEqual(0, SUT.Count);
			Assert.AreEqual(0u, SUT.Size);
			Assert.IsNull(SUT[0]);
			Assert.IsNull(SUT[int.MaxValue]);
			Assert.AreEqual(-1, SUT.IndexOf(new MyDependencyObject()));
			Assert.IsFalse(SUT.Contains(new MyDependencyObject()));
			Assert.IsFalse(SUT.Remove(new MyDependencyObject()));
			Assert.AreEqual(0, ToListFast(SUT).Count);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = SUT[-1]);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		[DataRow(5)]
		public void When_Insert_At_Start(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			var inserted = new MyDependencyObject();
			SUT.Insert(0, inserted);

			AssertContent(SUT, new DependencyObject[] { inserted }.Concat(items).ToArray());
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		[DataRow(5)]
		public void When_Insert_In_Middle(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			var inserted = new MyDependencyObject();
			SUT.Insert(1, inserted);

			var expected = items.Take(1).Cast<DependencyObject>().Append(inserted).Concat(items.Skip(1)).ToArray();
			AssertContent(SUT, expected);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		public void When_Insert_At_End(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			var inserted = new MyDependencyObject();
			SUT.Insert(count, inserted);

			AssertContent(SUT, items.Cast<DependencyObject>().Append(inserted).ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2)]
		[DataRow(4)]
		public void When_Insert_Invalid_Index(int count)
		{
			var SUT = CreateCollection(CreateItems(count));

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT.Insert(-1, new MyDependencyObject()));
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT.Insert(count + 1, new MyDependencyObject()));
			Assert.AreEqual(count, SUT.Count);
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		[DataRow(6)]
		public void When_RemoveAt_Each_Index(int count)
		{
			for (var indexToRemove = 0; indexToRemove < count; indexToRemove++)
			{
				var items = CreateItems(count);
				var SUT = CreateCollection(items);

				SUT.RemoveAt(indexToRemove);

				var expected = items.Where((_, i) => i != indexToRemove).ToArray();
				AssertContent(SUT, expected);
				Assert.IsNull(items[indexToRemove].GetParent());
			}
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2)]
		[DataRow(4)]
		public void When_RemoveAt_Invalid_Index(int count)
		{
			var SUT = CreateCollection(CreateItems(count));

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT.RemoveAt(-1));
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT.RemoveAt(count));
			Assert.AreEqual(count, SUT.Count);
		}

		[TestMethod]
		public void When_Remove_Unknown_Item()
		{
			var items = CreateItems(3);
			var SUT = CreateCollection(items);

			Assert.IsFalse(SUT.Remove(new MyDependencyObject()));
			AssertContent(SUT, items);
		}

		[TestMethod]
		public void When_Spilled_Then_Removed_Back_To_Inline_Size()
		{
			var items = CreateItems(5);
			var SUT = CreateCollection(items);

			for (var i = 0; i < 4; i++)
			{
				Assert.IsTrue(SUT.Remove(items[0 + i]));
			}

			AssertContent(SUT, items[4]);

			var added = new MyDependencyObject();
			SUT.Add(added);

			AssertContent(SUT, items[4], added);

			SUT.Clear();
			Assert.AreEqual(0, SUT.Count);

			var reAdded = CreateItems(3);
			foreach (var item in reAdded)
			{
				SUT.Add(item);
			}

			AssertContent(SUT, reAdded);
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(4)]
		public void When_Replace_Through_Indexer(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			var replacement = new MyDependencyObject();
			SUT[count - 1] = replacement;

			var expected = items.Take(count - 1).Cast<DependencyObject>().Append(replacement).ToArray();
			AssertContent(SUT, expected);

			Assert.IsNull(items[count - 1].GetParent());
			Assert.AreSame(SUT, replacement.GetParent());
		}

		[TestMethod]
		public void When_Replace_With_Same_Item()
		{
			var items = CreateItems(2);
			var SUT = CreateCollection(items);

			var changes = 0;
			SUT.VectorChanged += (s, e) => changes++;

			SUT[1] = items[1];

			Assert.AreEqual(0, changes);
			AssertContent(SUT, items);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2)]
		[DataRow(4)]
		public void When_Replace_Invalid_Index(int count)
		{
			var SUT = CreateCollection(CreateItems(count));

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT[-1] = new MyDependencyObject());
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT[count] = new MyDependencyObject());
			Assert.AreEqual(count, SUT.Count);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(5)]
		public void When_Clear(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			SUT.Clear();

			AssertContent(SUT);

			foreach (var item in items)
			{
				Assert.IsNull(item.GetParent());
			}
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		public void When_CopyTo(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			var array = new DependencyObject[count + 2];
			SUT.CopyTo(array, 1);

			Assert.IsNull(array[0]);

			for (var i = 0; i < count; i++)
			{
				Assert.AreSame(items[i], array[i + 1]);
			}

			Assert.IsNull(array[count + 1]);
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(3)]
		public void When_CopyTo_Invalid_Arguments(int count)
		{
			var SUT = CreateCollection(CreateItems(count));

			Assert.ThrowsExactly<ArgumentNullException>(() => SUT.CopyTo(null!, 0));
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SUT.CopyTo(new DependencyObject[count], -1));
			Assert.ThrowsExactly<ArgumentException>(() => SUT.CopyTo(new DependencyObject[count], 1));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(4)]
		public void When_Modified_During_Enumeration(int count)
		{
			var SUT = CreateCollection(CreateItems(count));

			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				foreach (var item in SUT)
				{
					SUT.Add(new MyDependencyObject());
				}
			});

			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				var enumerator = SUT.GetEnumeratorFast();
				enumerator.MoveNext();
				SUT.RemoveAt(0);
				enumerator.MoveNext();
			});
		}

		[TestMethod]
		public void When_Enumerating_Fast_Then_No_Allocation()
		{
			var SUT = CreateCollection(CreateItems(2));

			static int Enumerate(DependencyObjectCollection collection)
			{
				var count = 0;
				var enumerator = collection.GetEnumeratorFast();

				while (enumerator.MoveNext())
				{
					count += enumerator.Current is null ? 0 : 1;
				}

				return count;
			}

			// Warm-up, so that the measurement below is not polluted by first-call initialization.
			Assert.AreEqual(2, Enumerate(SUT));

			var before = GC.GetAllocatedBytesForCurrentThread();

			for (var i = 0; i < 100; i++)
			{
				Enumerate(SUT);
			}

			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.IsTrue(allocated < 100, $"Fast enumeration allocated {allocated} bytes");
		}

		[TestMethod]
		public void When_OnCollectionChanged_Without_VectorChanged_Handlers()
		{
			var SUT = new MyDependencyObjectCollection();

			SUT.Add(new MyDependencyObject());
			SUT.Insert(0, new MyDependencyObject());
			SUT[0] = new MyDependencyObject();
			SUT.RemoveAt(0);
			SUT.Clear();

			Assert.AreEqual(5, SUT.CollectionChangedCount);
		}

		[TestMethod]
		public void When_OnCollectionChanged_Then_Invoked_Before_Handlers()
		{
			var order = new List<string>();
			var SUT = new MyDependencyObjectCollection();
			SUT.CollectionChangedCallback = () => order.Add("internal");
			SUT.VectorChanged += (s, e) => order.Add("external");

			SUT.Add(new MyDependencyObject());

			CollectionAssert.AreEqual(new[] { "internal", "external" }, order);
		}

		[TestMethod]
		public void When_Multiple_Handlers_Then_Invoked_In_Subscription_Order()
		{
			var SUT = new DependencyObjectCollection();
			var invocations = new List<string>();

			void One(IObservableVector<DependencyObject> s, IVectorChangedEventArgs e) => invocations.Add("One");
			void Two(IObservableVector<DependencyObject> s, IVectorChangedEventArgs e) => invocations.Add("Two");

			SUT.VectorChanged += One;
			SUT.VectorChanged += Two;
			SUT.VectorChanged += One;

			// Only the last occurrence of the handler is removed.
			SUT.VectorChanged -= One;

			SUT.Add(new MyDependencyObject());

			CollectionAssert.AreEqual(new[] { "One", "Two" }, invocations);

			invocations.Clear();
			SUT.VectorChanged -= One;
			SUT.VectorChanged -= Two;

			// Removing an unknown handler is a no-op.
			SUT.VectorChanged -= One;

			SUT.Add(new MyDependencyObject());

			Assert.AreEqual(0, invocations.Count);
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(3)]
		public void When_Mutating_Then_Vector_Events_Are_Raised(int initialCount)
		{
			var items = CreateItems(initialCount);
			var SUT = CreateCollection(items);

			var changes = new List<(CollectionChange Change, uint Index)>();
			SUT.VectorChanged += (s, e) =>
			{
				Assert.AreSame(SUT, s);
				changes.Add((e.CollectionChange, e.Index));
			};

			SUT.Add(new MyDependencyObject());
			SUT.Insert(0, new MyDependencyObject());
			SUT[1] = new MyDependencyObject();
			SUT.RemoveAt(1);
			SUT.Clear();

			CollectionAssert.AreEqual(
				new[]
				{
					(CollectionChange.ItemInserted, (uint)initialCount),
					(CollectionChange.ItemInserted, 0u),
					(CollectionChange.ItemChanged, 1u),
					(CollectionChange.ItemRemoved, 1u),
					(CollectionChange.Reset, 0u),
				},
				changes);
		}

		[TestMethod]
		public void When_Handler_Mutates_Collection_During_Event()
		{
			var SUT = new DependencyObjectCollection();
			var changes = new List<CollectionChange>();
			var reentered = false;

			SUT.VectorChanged += (s, e) =>
			{
				changes.Add(e.CollectionChange);

				if (!reentered)
				{
					reentered = true;

					// Re-entrant mutations are applied immediately, and raise their own events.
					SUT.Add(new MyDependencyObject());
					SUT.Add(new MyDependencyObject());
				}
			};

			SUT.Add(new MyDependencyObject());

			Assert.AreEqual(3, SUT.Count);
			CollectionAssert.AreEqual(
				new[] { CollectionChange.ItemInserted, CollectionChange.ItemInserted, CollectionChange.ItemInserted },
				changes);
		}

		[TestMethod]
		public void When_Handler_Removes_Itself_During_Event()
		{
			var SUT = new DependencyObjectCollection();
			var invocations = 0;

			VectorChangedEventHandler<DependencyObject> handler = null!;
			handler = (s, e) =>
			{
				invocations++;
				SUT.VectorChanged -= handler;
			};

			SUT.VectorChanged += handler;

			SUT.Add(new MyDependencyObject());
			SUT.Add(new MyDependencyObject());

			Assert.AreEqual(1, invocations);
		}

		[TestMethod]
		public void When_Handler_Clears_Collection_During_Event()
		{
			var SUT = new DependencyObjectCollection();
			var items = CreateItems(3);

			foreach (var item in items)
			{
				SUT.Add(item);
			}

			var cleared = false;
			SUT.VectorChanged += (s, e) =>
			{
				if (!cleared)
				{
					cleared = true;
					SUT.Clear();
				}
			};

			SUT.Add(new MyDependencyObject());

			Assert.AreEqual(0, SUT.Count);

			foreach (var item in items)
			{
				Assert.IsNull(item.GetParent());
			}
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(4)]
		public void When_Parent_Is_Updated(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			// Without an explicit parent, the collection is its own items parent.
			foreach (var item in items)
			{
				Assert.AreSame(SUT, item.GetParent());
			}

			var parent = new MyDependencyObject();
			SUT.SetParent(parent);

			foreach (var item in items)
			{
				Assert.AreSame(parent, item.GetParent());
			}

			var added = new MyDependencyObject();
			SUT.Add(added);
			Assert.AreSame(parent, added.GetParent());

			SUT.SetParent(null);

			foreach (var item in items)
			{
				Assert.AreSame(SUT, item.GetParent());
			}
		}

		[TestMethod]
		public void When_Created_With_Parent()
		{
			var parent = new MyDependencyObject();
			var SUT = new DependencyObjectCollection(parent);

			var item = new MyDependencyObject();
			SUT.Add(item);

			Assert.AreSame(parent, item.GetParent());

			SUT.Remove(item);

			Assert.IsNull(item.GetParent());
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(3)]
		public void When_Locked(int count)
		{
			var items = CreateItems(count);
			var SUT = CreateCollection(items);

			SUT.Lock();

			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Add(new MyDependencyObject()));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Insert(0, new MyDependencyObject()));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.RemoveAt(0));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Remove(items[0]));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Clear());
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT[0] = new MyDependencyObject());

			// Reads and no-op mutations are still allowed while locked.
			AssertContent(SUT, items);
			SUT[0] = items[0];

			SUT.Unlock();

			var added = new MyDependencyObject();
			SUT.Add(added);

			AssertContent(SUT, items.Cast<DependencyObject>().Append(added).ToArray());
		}

		[TestMethod]
		public void When_Locked_Multiple_Times()
		{
			var SUT = CreateCollection(CreateItems(2));

			SUT.Lock();
			SUT.Lock();
			SUT.Unlock();

			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Add(new MyDependencyObject()));

			SUT.Unlock();

			SUT.Add(new MyDependencyObject());
			Assert.AreEqual(3, SUT.Count);
		}

		[TestMethod]
		public void When_ValidateItem_Rejects_Item()
		{
			var SUT = new SetterBaseCollection();
			SUT.Add(new Setter());

			SUT.Seal();

			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Add(new Setter()));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Insert(0, new Setter()));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT[0] = new Setter());
			Assert.AreEqual(1, SUT.Count);
		}
	}
}
