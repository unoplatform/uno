using System;
using System.Collections.Generic;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace UnitTests
{
	[TestClass]
	public class Given_Deque
	{
		[TestMethod]
		public void Capacity_SetTo0_ActsLikeList()
		{
			var list = new List<int>();
			list.Capacity = 0;
			Assert.AreEqual(0, list.Capacity);

			var deque = new Deque<int>();
			deque.Capacity = 0;
			Assert.AreEqual(0, deque.Capacity);
		}

		[TestMethod]
		public void Capacity_SetNegative_ActsLikeList()
		{
			var list = new List<int>();
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { list.Capacity = -1; });

			var deque = new Deque<int>();
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { deque.Capacity = -1; });
		}

		[TestMethod]
		public void Capacity_SetLarger_UsesSpecifiedCapacity()
		{
			var deque = new Deque<int>(1);
			Assert.AreEqual(1, deque.Capacity);
			deque.Capacity = 17;
			Assert.AreEqual(17, deque.Capacity);
		}

		[TestMethod]
		public void Capacity_SetSmaller_UsesSpecifiedCapacity()
		{
			var deque = new Deque<int>(13);
			Assert.AreEqual(13, deque.Capacity);
			deque.Capacity = 7;
			Assert.AreEqual(7, deque.Capacity);
		}

		[TestMethod]
		public void Capacity_Set_PreservesData()
		{
			var deque = new Deque<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, deque.Capacity);
			deque.Capacity = 7;
			Assert.AreEqual(7, deque.Capacity);
			Assert.IsTrue(new[] { 1, 2, 3 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Capacity_Set_WhenSplit_PreservesData()
		{
			var deque = new Deque<int>(new int[] { 1, 2, 3 });
			deque.RemoveFromFront();
			deque.AddToBack(4);
			Assert.AreEqual(3, deque.Capacity);
			deque.Capacity = 7;
			Assert.AreEqual(7, deque.Capacity);
			Assert.IsTrue(new[] { 2, 3, 4 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Capacity_Set_SmallerThanCount_ActsLikeList()
		{
			var list = new List<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, list.Capacity);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { list.Capacity = 2; });

			var deque = new Deque<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, deque.Capacity);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { deque.Capacity = 2; });
		}

		[TestMethod]
		public void Capacity_Set_ToItself_DoesNothing()
		{
			var deque = new Deque<int>(13);
			Assert.AreEqual(13, deque.Capacity);
			deque.Capacity = 13;
			Assert.AreEqual(13, deque.Capacity);
		}

		// Implementation detail: the default capacity.
		const int DefaultCapacity = 8;

		[TestMethod]
		public void Constructor_WithoutExplicitCapacity_UsesDefaultCapacity()
		{
			var deque = new Deque<int>();
			Assert.AreEqual(DefaultCapacity, deque.Capacity);
		}

		[TestMethod]
		public void Constructor_CapacityOf0_ActsLikeList()
		{
			var list = new List<int>(0);
			Assert.AreEqual(0, list.Capacity);

			var deque = new Deque<int>(0);
			Assert.AreEqual(0, deque.Capacity);
		}

		[TestMethod]
		public void Constructor_CapacityOf0_PermitsAdd()
		{
			var deque = new Deque<int>(0);
			deque.AddToBack(13);
			Assert.IsTrue(new[] { 13 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Constructor_NegativeCapacity_ActsLikeList()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new List<int>(-1));

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Deque<int>(-1));
		}

		[TestMethod]
		public void Constructor_CapacityOf1_UsesSpecifiedCapacity()
		{
			var deque = new Deque<int>(1);
			Assert.AreEqual(1, deque.Capacity);
		}

		[TestMethod]
		public void Constructor_FromEmptySequence_UsesDefaultCapacity()
		{
			var deque = new Deque<int>(new int[] { });
			Assert.AreEqual(DefaultCapacity, deque.Capacity);
		}

		[TestMethod]
		public void Constructor_FromSequence_InitializesFromSequence()
		{
			var deque = new Deque<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, deque.Capacity);
			Assert.AreEqual(3, deque.Count);
			Assert.IsTrue(new int[] { 1, 2, 3 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void IndexOf_ItemPresent_ReturnsItemIndex()
		{
			var deque = new Deque<int>(new[] { 1, 2 });
			var result = deque.IndexOf(2);
			Assert.AreEqual(1, result);
		}

		[TestMethod]
		public void IndexOf_ItemNotPresent_ReturnsNegativeOne()
		{
			var deque = new Deque<int>(new[] { 1, 2 });
			var result = deque.IndexOf(3);
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		public void IndexOf_ItemPresentAndSplit_ReturnsItemIndex()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.RemoveFromBack();
			deque.AddToFront(0);
			Assert.AreEqual(0, deque.IndexOf(0));
			Assert.AreEqual(1, deque.IndexOf(1));
			Assert.AreEqual(2, deque.IndexOf(2));
		}

		[TestMethod]
		public void Contains_ItemPresent_ReturnsTrue()
		{
			var deque = new Deque<int>(new[] { 1, 2 }) as ICollection<int>;
			Assert.IsTrue(deque.Contains(2));
		}

		[TestMethod]
		public void Contains_ItemNotPresent_ReturnsFalse()
		{
			var deque = new Deque<int>(new[] { 1, 2 }) as ICollection<int>;
			Assert.IsFalse(deque.Contains(3));
		}

		[TestMethod]
		public void Contains_ItemPresentAndSplit_ReturnsTrue()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.RemoveFromBack();
			deque.AddToFront(0);
			var deq = deque as ICollection<int>;
			Assert.IsTrue(deq.Contains(0));
			Assert.IsTrue(deq.Contains(1));
			Assert.IsTrue(deq.Contains(2));
			Assert.IsFalse(deq.Contains(3));
		}

		[TestMethod]
		public void Add_IsAddToBack()
		{
			var deque1 = new Deque<int>(new[] { 1, 2 });
			var deque2 = new Deque<int>(new[] { 1, 2 });
			((ICollection<int>)deque1).Add(3);
			deque2.AddToBack(3);
			Assert.IsTrue(deque1.SequenceEqual(deque2));
		}

		[TestMethod]
		public void NonGenericEnumerator_EnumeratesItems()
		{
			var deque = new Deque<int>(new[] { 1, 2 });
			var results = new List<int>();
			var objEnum = ((System.Collections.IEnumerable)deque).GetEnumerator();
			while (objEnum.MoveNext())
			{
				results.Add((int)objEnum.Current);
			}
			Assert.IsTrue(results.SequenceEqual(deque));
		}

		[TestMethod]
		public void IsReadOnly_ReturnsFalse()
		{
			var deque = new Deque<int>();
			Assert.IsFalse(((ICollection<int>)deque).IsReadOnly);
		}

		[TestMethod]
		public void CopyTo_CopiesItems()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			var results = new int[3];
			((ICollection<int>)deque).CopyTo(results, 0);
		}

		[TestMethod]
		public void CopyTo_NullArray_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentNullException>(() => ((ICollection<int>)list).CopyTo(null, 0));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentNullException>(() => ((ICollection<int>)deque).CopyTo(null, 0));
		}

		[TestMethod]
		public void CopyTo_NegativeOffset_ActsLikeList()
		{
			var destination = new int[3];
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ((ICollection<int>)list).CopyTo(destination, -1));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ((ICollection<int>)deque).CopyTo(destination, -1));
		}

		[TestMethod]
		public void CopyTo_InsufficientSpace_ActsLikeList()
		{
			var destination = new int[3];
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentException>(() => ((ICollection<int>)list).CopyTo(destination, 1));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentException>(() => ((ICollection<int>)deque).CopyTo(destination, 1));
		}

		[TestMethod]
		public void Clear_EmptiesAllItems()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.Clear();
			Assert.IsEmpty(deque);
			Assert.IsTrue(new int[] { }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Clear_DoesNotChangeCapacity()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.AreEqual(3, deque.Capacity);
			deque.Clear();
			Assert.AreEqual(3, deque.Capacity);
		}

		[TestMethod]
		public void RemoveFromFront_Empty_ActsLikeStack()
		{
			var stack = new Stack<int>();
			Assert.ThrowsExactly<InvalidOperationException>(() => stack.Pop());

			var deque = new Deque<int>();
			Assert.ThrowsExactly<InvalidOperationException>(() => deque.RemoveFromFront());
		}

		[TestMethod]
		public void RemoveFromBack_Empty_ActsLikeQueue()
		{
			var queue = new Queue<int>();
			Assert.ThrowsExactly<InvalidOperationException>(() => queue.Dequeue());

			var deque = new Deque<int>();
			Assert.ThrowsExactly<InvalidOperationException>(() => deque.RemoveFromBack());
		}

		[TestMethod]
		public void Remove_ItemPresent_RemovesItemAndReturnsTrue()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3, 4 });
			var result = deque.Remove(3);
			Assert.IsTrue(result);
			Assert.IsTrue(new[] { 1, 2, 4 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Remove_ItemNotPresent_KeepsItemsReturnsFalse()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3, 4 });
			var result = deque.Remove(5);
			Assert.IsFalse(result);
			Assert.IsTrue(new[] { 1, 2, 3, 4 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Insert_InsertsElementAtIndex()
		{
			var deque = new Deque<int>(new[] { 1, 2 });
			deque.Insert(1, 13);
			Assert.IsTrue(new[] { 1, 13, 2 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Insert_AtIndex0_IsSameAsAddToFront()
		{
			var deque1 = new Deque<int>(new[] { 1, 2 });
			var deque2 = new Deque<int>(new[] { 1, 2 });
			deque1.Insert(0, 0);
			deque2.AddToFront(0);
			Assert.IsTrue(deque1.SequenceEqual(deque2));
		}

		[TestMethod]
		public void Insert_AtCount_IsSameAsAddToBack()
		{
			var deque1 = new Deque<int>(new[] { 1, 2 });
			var deque2 = new Deque<int>(new[] { 1, 2 });
			deque1.Insert(deque1.Count, 0);
			deque2.AddToBack(0);
			Assert.IsTrue(deque1.SequenceEqual(deque2));
		}

		[TestMethod]
		public void Insert_NegativeIndex_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.Insert(-1, 0));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque.Insert(-1, 0));
		}

		[TestMethod]
		public void Insert_IndexTooLarge_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, 0));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque.Insert(deque.Count + 1, 0));
		}

		[TestMethod]
		public void RemoveAt_RemovesElementAtIndex()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.RemoveFromBack();
			deque.AddToFront(0);
			deque.RemoveAt(1);
			Assert.IsTrue(new[] { 0, 2 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void RemoveAt_Index0_IsSameAsRemoveFromFront()
		{
			var deque1 = new Deque<int>(new[] { 1, 2 });
			var deque2 = new Deque<int>(new[] { 1, 2 });
			deque1.RemoveAt(0);
			deque2.RemoveFromFront();
			Assert.IsTrue(deque1.SequenceEqual(deque2));
		}

		[TestMethod]
		public void RemoveAt_LastIndex_IsSameAsRemoveFromBack()
		{
			var deque1 = new Deque<int>(new[] { 1, 2 });
			var deque2 = new Deque<int>(new[] { 1, 2 });
			deque1.RemoveAt(1);
			deque2.RemoveFromBack();
			Assert.IsTrue(deque1.SequenceEqual(deque2));
		}

		[TestMethod]
		public void RemoveAt_NegativeIndex_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque.RemoveAt(-1));
		}

		[TestMethod]
		public void RemoveAt_IndexTooLarge_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque.RemoveAt(deque.Count));
		}

		[TestMethod]
		public void InsertMultiple()
		{
			InsertTest(new[] { 1, 2, 3 }, new[] { 7, 13 });
			InsertTest(new[] { 1, 2, 3, 4 }, new[] { 7, 13 });
		}

		private void InsertTest(IReadOnlyCollection<int> initial, IReadOnlyCollection<int> items)
		{
			var totalCapacity = initial.Count + items.Count;
			for (int rotated = 0; rotated <= totalCapacity; ++rotated)
			{
				for (int index = 0; index <= initial.Count; ++index)
				{
					// Calculate the expected result using the slower List<int>.
					var result = new List<int>(initial);
					for (int i = 0; i != rotated; ++i)
					{
						var item = result[0];
						result.RemoveAt(0);
						result.Add(item);
					}
					result.InsertRange(index, items);

					// First, start off the deque with the initial items.
					var deque = new Deque<int>(initial);

					// Ensure there's enough room for the inserted items.
					deque.Capacity += items.Count;

					// Rotate the existing items.
					for (int i = 0; i != rotated; ++i)
					{
						var item = deque[0];
						deque.RemoveFromFront();
						deque.AddToBack(item);
					}

					// Do the insert.
					deque.InsertRange(index, items);

					// Ensure the results are as expected.
					Assert.IsTrue(result.SequenceEqual(deque));
				}
			}
		}

		[TestMethod]
		public void Insert_RangeOfZeroElements_HasNoEffect()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.InsertRange(1, new int[] { });
			Assert.IsTrue(new[] { 1, 2, 3 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void InsertMultiple_MakesRoomForNewElements()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.InsertRange(1, new[] { 7, 13 });
			Assert.IsTrue(new[] { 1, 7, 13, 2, 3 }.SequenceEqual(deque));
			Assert.AreEqual(5, deque.Capacity);
		}

		[TestMethod]
		public void RemoveMultiple()
		{
			RemoveTest(new[] { 1, 2, 3 });
			RemoveTest(new[] { 1, 2, 3, 4 });
		}

		private void RemoveTest(IReadOnlyCollection<int> initial)
		{
			for (int count = 0; count <= initial.Count; ++count)
			{
				for (int rotated = 0; rotated <= initial.Count; ++rotated)
				{
					for (int index = 0; index <= initial.Count - count; ++index)
					{
						// Calculated the expected result using the slower List<int>.
						var result = new List<int>(initial);
						for (int i = 0; i != rotated; ++i)
						{
							var item = result[0];
							result.RemoveAt(0);
							result.Add(item);
						}
						result.RemoveRange(index, count);

						// First, start off the deque with the initial items.
						var deque = new Deque<int>(initial);

						// Rotate the existing items.
						for (int i = 0; i != rotated; ++i)
						{
							var item = deque[0];
							deque.RemoveFromFront();
							deque.AddToBack(item);
						}

						// Do the remove.
						deque.RemoveRange(index, count);

						// Ensure the results are as expected.
						Assert.IsTrue(result.SequenceEqual(deque));
					}
				}
			}
		}

		[TestMethod]
		public void Remove_RangeOfZeroElements_HasNoEffect()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.RemoveRange(1, 0);
			Assert.IsTrue(new[] { 1, 2, 3 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void Remove_NegativeCount_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveRange(1, -1));

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque.RemoveRange(1, -1));
		}

		[TestMethod]
		public void GetItem_ReadsElements()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.AreEqual(1, deque[0]);
			Assert.AreEqual(2, deque[1]);
			Assert.AreEqual(3, deque[2]);
		}

		[TestMethod]
		public void GetItem_Split_ReadsElements()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.RemoveFromBack();
			deque.AddToFront(0);
			Assert.AreEqual(0, deque[0]);
			Assert.AreEqual(1, deque[1]);
			Assert.AreEqual(2, deque[2]);
		}

		[TestMethod]
		public void GetItem_IndexTooLarge_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list[3]);

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque[3]);
		}

		[TestMethod]
		public void GetItem_NegativeIndex_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list[-1]);

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque[-1]);
		}

		[TestMethod]
		public void SetItem_WritesElements()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque[0] = 7;
			deque[1] = 11;
			deque[2] = 13;
			Assert.IsTrue(new[] { 7, 11, 13 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void SetItem_Split_WritesElements()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 });
			deque.RemoveFromBack();
			deque.AddToFront(0);
			deque[0] = 7;
			deque[1] = 11;
			deque[2] = 13;
			Assert.IsTrue(new[] { 7, 11, 13 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void SetItem_IndexTooLarge_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { list[3] = 13; });

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { deque[3] = 13; });
		}

		[TestMethod]
		public void SetItem_NegativeIndex_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { list[-1] = 13; });

			var deque = new Deque<int>(new[] { 1, 2, 3 });
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { deque[-1] = 13; });
		}

		[TestMethod]
		public void NongenericIndexOf_ItemPresent_ReturnsItemIndex()
		{
			var deque = new Deque<int>(new[] { 1, 2 }) as IList;
			var result = deque.IndexOf(2);
			Assert.AreEqual(1, result);
		}

		[TestMethod]
		public void NongenericIndexOf_ItemNotPresent_ReturnsNegativeOne()
		{
			var deque = new Deque<int>(new[] { 1, 2 }) as IList;
			var result = deque.IndexOf(3);
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		public void NongenericIndexOf_ItemPresentAndSplit_ReturnsItemIndex()
		{
			var deque_ = new Deque<int>(new[] { 1, 2, 3 });
			deque_.RemoveFromBack();
			deque_.AddToFront(0);
			var deque = deque_ as IList;
			Assert.AreEqual(0, deque.IndexOf(0));
			Assert.AreEqual(1, deque.IndexOf(1));
			Assert.AreEqual(2, deque.IndexOf(2));
		}

		[TestMethod]
		public void NongenericIndexOf_WrongItemType_ReturnsNegativeOne()
		{
			var list = new List<int>(new[] { 1, 2 }) as IList;
			Assert.AreEqual(-1, list.IndexOf(this));

			var deque = new Deque<int>(new[] { 1, 2 }) as IList;
			Assert.AreEqual(-1, deque.IndexOf(this));
		}

		[TestMethod]
		public void NongenericContains_WrongItemType_ReturnsFalse()
		{
			var list = new List<int>(new[] { 1, 2 }) as IList;
			Assert.IsFalse(list.Contains(this));

			var deque = new Deque<int>(new[] { 1, 2 }) as IList;
			Assert.IsFalse(deque.Contains(this));
		}

		[TestMethod]
		public void NongenericContains_ItemPresent_ReturnsTrue()
		{
			var deque = new Deque<int>(new[] { 1, 2 }) as IList;
			Assert.IsTrue(deque.Contains(2));
		}

		[TestMethod]
		public void NongenericContains_ItemNotPresent_ReturnsFalse()
		{
			var deque = new Deque<int>(new[] { 1, 2 }) as IList;
			Assert.IsFalse(deque.Contains(3));
		}

		[TestMethod]
		public void NongenericContains_ItemPresentAndSplit_ReturnsTrue()
		{
			var deque_ = new Deque<int>(new[] { 1, 2, 3 });
			deque_.RemoveFromBack();
			deque_.AddToFront(0);
			var deque = deque_ as IList;
			Assert.IsTrue(deque.Contains(0));
			Assert.IsTrue(deque.Contains(1));
			Assert.IsTrue(deque.Contains(2));
			Assert.IsFalse(deque.Contains(3));
		}

		[TestMethod]
		public void NongenericIsReadOnly_ReturnsFalse()
		{
			var deque = new Deque<int>() as IList;
			Assert.IsFalse(deque.IsReadOnly);
		}

		[TestMethod]
		public void NongenericCopyTo_CopiesItems()
		{
			var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
			var results = new int[3];
			deque.CopyTo(results, 0);
		}

		[TestMethod]
		public void NongenericCopyTo_NullArray_ActsLikeList()
		{
			var list = new List<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => list.CopyTo(null, 0));

			var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => deque.CopyTo(null, 0));
		}

		[TestMethod]
		public void NongenericCopyTo_NegativeOffset_ActsLikeList()
		{
			var destination = new int[3];
			var list = new List<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.CopyTo(destination, -1));

			var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => deque.CopyTo(destination, -1));
		}

		[TestMethod]
		public void NongenericCopyTo_InsufficientSpace_ActsLikeList()
		{
			var destination = new int[3];
			var list = new List<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => list.CopyTo(destination, 1));

			var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => deque.CopyTo(destination, 1));
		}

		[TestMethod]
		public void NongenericCopyTo_WrongType_ActsLikeList()
		{
			var destination = new IList[3];
			var list = new List<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => list.CopyTo(destination, 0));

			var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => deque.CopyTo(destination, 0));
		}

		[TestMethod]
		public void NongenericCopyTo_MultidimensionalArray_ActsLikeList()
		{
			var destination = new int[3, 3];
			var list = new List<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => list.CopyTo(destination, 0));

			var deque = new Deque<int>(new[] { 1, 2, 3 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => deque.CopyTo(destination, 0));
		}

		[TestMethod]
		public void NongenericAdd_WrongType_ActsLikeList()
		{
			var list = new List<int>() as IList;
			Assert.ThrowsExactly<ArgumentException>(() => list.Add(this));

			var deque = new Deque<int>() as IList;
			Assert.ThrowsExactly<ArgumentException>(() => deque.Add(this));
		}

		[TestMethod]
		public void NongenericNullableType_AllowsInsertingNull()
		{
			var deque = new Deque<int?>();
			var list = deque as IList;
			var result = list.Add(null);
			Assert.AreEqual(0, result);
			Assert.IsTrue(new int?[] { null }.SequenceEqual(deque));
		}

		[TestMethod]
		public void NongenericClassType_AllowsInsertingNull()
		{
			var deque = new Deque<object>();
			var list = deque as IList;
			var result = list.Add(null);
			Assert.AreEqual(0, result);
			Assert.IsTrue(new object[] { null }.SequenceEqual(deque));
		}

		[TestMethod]
		public void NongenericInterfaceType_AllowsInsertingNull()
		{
			var deque = new Deque<IList>();
			var list = deque as IList;
			var result = list.Add(null);
			Assert.AreEqual(0, result);
			Assert.IsTrue(new IList[] { null }.SequenceEqual(deque));
		}

		[TestMethod]
		public void NongenericStruct_AddNull_ActsLikeList()
		{
			var list = new List<int>() as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => list.Add(null));

			var deque = new Deque<int>() as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => deque.Add(null));
		}

		[TestMethod]
		public void NongenericGenericStruct_AddNull_ActsLikeList()
		{
			var list = new List<KeyValuePair<int, int>>() as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => list.Add(null));

			var deque = new Deque<KeyValuePair<int, int>>() as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => deque.Add(null));
		}

		[TestMethod]
		public void NongenericIsFixedSize_IsFalse()
		{
			var deque = new Deque<int>() as IList;
			Assert.IsFalse(deque.IsFixedSize);
		}

		[TestMethod]
		public void NongenericIsSynchronized_IsFalse()
		{
			var deque = new Deque<int>() as IList;
			Assert.IsFalse(deque.IsSynchronized);
		}

		[TestMethod]
		public void NongenericSyncRoot_IsSelf()
		{
			var deque = new Deque<int>() as IList;
			Assert.AreSame(deque, deque.SyncRoot);
		}

		[TestMethod]
		public void NongenericInsert_InsertsItem()
		{
			var deque = new Deque<int>();
			var list = deque as IList;
			list.Insert(0, 7);
			Assert.IsTrue(new[] { 7 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void NongenericInsert_WrongType_ActsLikeList()
		{
			var list = new List<int>() as IList;
			Assert.ThrowsExactly<ArgumentException>(() => list.Insert(0, this));

			var deque = new Deque<int>() as IList;
			Assert.ThrowsExactly<ArgumentException>(() => deque.Insert(0, this));
		}

		[TestMethod]
		public void NongenericStruct_InsertNull_ActsMostlyLikeList()
		{
			var list = new List<int>() as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => list.Insert(0, null)); // Should probably be "value".

			var deque = new Deque<int>() as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => deque.Insert(0, null));
		}

		[TestMethod]
		public void NongenericRemove_RemovesItem()
		{
			var deque = new Deque<int>(new[] { 13 });
			var list = deque as IList;
			list.Remove(13);
			Assert.IsTrue(new int[] { }.SequenceEqual(deque));
		}

		[TestMethod]
		public void NongenericRemove_WrongType_DoesNothing()
		{
			var list = new List<int>(new[] { 13 }) as IList;
			list.Remove(this);
			list.Remove(null);
			Assert.AreEqual(1, list.Count);

			var deque = new Deque<int>(new[] { 13 }) as IList;
			deque.Remove(this);
			deque.Remove(null);
			Assert.AreEqual(1, deque.Count);
		}

		[TestMethod]
		public void NongenericGet_GetsItem()
		{
			var deque = new Deque<int>(new[] { 13 }) as IList;
			var value = (int)deque[0];
			Assert.AreEqual(13, value);
		}

		[TestMethod]
		public void NongenericSet_SetsItem()
		{
			var deque = new Deque<int>(new[] { 13 });
			var list = deque as IList;
			list[0] = 7;
			Assert.IsTrue(new[] { 7 }.SequenceEqual(deque));
		}

		[TestMethod]
		public void NongenericSet_WrongType_ActsLikeList()
		{
			var list = new List<int>(new[] { 13 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => { list[0] = this; });

			var deque = new Deque<int>(new[] { 13 }) as IList;
			Assert.ThrowsExactly<ArgumentException>(() => { deque[0] = this; });
		}

		[TestMethod]
		public void NongenericStruct_SetNull_ActsLikeList()
		{
			var list = new List<int>(new[] { 13 }) as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => { list[0] = null; });

			var deque = new Deque<int>(new[] { 13 }) as IList;
			Assert.ThrowsExactly<ArgumentNullException>(() => { deque[0] = null; });
		}

		[TestMethod]
		public void ToArray_CopiesToNewArray()
		{
			var deque = new Deque<int>(new[] { 0, 1 });
			deque.AddToBack(13);
			var result = deque.ToArray();
			Assert.IsTrue(new[] { 0, 1, 13 }.SequenceEqual(result));
		}

		[TestMethod]
		public void When_Clear_Then_Memory_Released()
		{
			var deque = new Deque<object>();

			WeakReference AddItem()
			{
				var o = new object();
				deque.AddToBack(o);
				return new(o);
			}

			var r = AddItem();
			deque.Clear();

			ValidateCollectedReference(r);
		}

		[TestMethod]
		public void When_RemoveFront_Then_Memory_Released()
		{
			var deque = new Deque<object>();

			WeakReference AddItem()
			{
				var o = new object();
				deque.AddToBack(o);
				return new(o);
			}

			void Remove()
				=> deque.RemoveFromFront();

			var r = AddItem();
			Remove();

			ValidateCollectedReference(r);
		}

		[TestMethod]
		public void When_RemoveBack_Then_Memory_Released()
		{
			var deque = new Deque<object>();

			WeakReference AddItem()
			{
				var o = new object();
				deque.AddToBack(o);
				return new(o);
			}

			void Remove()
				=> deque.RemoveFromBack();

			var r = AddItem();
			Remove();

			ValidateCollectedReference(r);
		}

		[TestMethod]
		public void When_Remove_Then_Memory_Released()
		{
			var deque = new Deque<object>();

			WeakReference AddItem()
			{
				var o = new object();
				deque.AddToBack(o);
				return new(o);
			}

			void Remove()
				=> deque.Remove(deque[0]);

			var r = AddItem();
			Remove();

			ValidateCollectedReference(r);
		}

		[TestMethod]
		public void When_RemoveAt_Then_Memory_Released()
		{
			var deque = new Deque<object>();

			WeakReference AddItem()
			{
				var o = new object();
				deque.AddToBack(o);
				return new(o);
			}

			void Remove()
				=> deque.RemoveAt(0);

			var r = AddItem();
			Remove();

			ValidateCollectedReference(r);
		}

		private void ValidateCollectedReference(WeakReference reference)
		{
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed < TimeSpan.FromSeconds(3))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();

				if (!reference.IsAlive)
				{
					return;
				}

				Thread.Sleep(100);
			}

			Assert.IsFalse(reference.IsAlive);
		}
	}
}
