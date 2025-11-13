// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Derived from https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/System.Collections.NonGeneric/tests/HashtableTests.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Collections;

namespace Uno.UI.Tests.Collections
{
	[TestClass]
	public class Given_HashtableEx
	{
		[TestMethod]
		public void Ctor_Empty()
		{
			var hash = new ComparableHashtable();
			VerifyHashtable(hash, null, null);
		}

		[TestMethod]
		public void Ctor_IDictionary()
		{
			// No exception
			var hash1 = new ComparableHashtable(new HashtableEx());
			Assert.AreEqual(0, hash1.Count);

			hash1 = new ComparableHashtable(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx())))));
			Assert.AreEqual(0, hash1.Count);

			HashtableEx hash2 = Helpers.CreateIntHashtable(100);
			hash1 = new ComparableHashtable(hash2);

			VerifyHashtable(hash1, hash2, null);
		}

		[TestMethod]
		public void Ctor_IDictionary_NullDictionary_ThrowsArgumentNullException()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new HashtableEx((HashtableEx)null)); // Dictionary is null
		}

		[TestMethod]
		public void Ctor_IDictionary_HashCodeProvider_Comparer_NullDictionary_ThrowsArgumentNullException()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new HashtableEx(null, StringComparer.OrdinalIgnoreCase)); // Dictionary is null
		}

		[TestMethod]
		public void Ctor_IEqualityComparer()
		{
			// Null comparer
			var hash = new ComparableHashtable((IEqualityComparer)null);
			VerifyHashtable(hash, null, null);

			// Custom comparer
			IEqualityComparer comparer = StringComparer.CurrentCulture;
			hash = new ComparableHashtable(comparer);
			VerifyHashtable(hash, null, comparer);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(10)]
		[DataRow(100)]
		public void Ctor_Int(int capacity)
		{
			var hash = new ComparableHashtable(capacity);
			VerifyHashtable(hash, null, null);
		}

		[TestMethod]
		public void Ctor_Int_Invalid()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(-1)); // Capacity < 0
			Assert.ThrowsExactly<ArgumentException>(() => new HashtableEx(int.MaxValue)); // Capacity / load factor > int.MaxValue
		}

		[TestMethod]
		public void Ctor_IDictionary_Int()
		{
			// No exception
			var hash1 = new ComparableHashtable(new HashtableEx(), 1f, StringComparer.OrdinalIgnoreCase);
			Assert.AreEqual(0, hash1.Count);

			hash1 = new ComparableHashtable(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(), 1f), 1f), 1f), 1f), 1f,
				StringComparer.OrdinalIgnoreCase);
			Assert.AreEqual(0, hash1.Count);

			HashtableEx hash2 = Helpers.CreateIntHashtable(100);
			hash1 = new ComparableHashtable(hash2, 1f, StringComparer.OrdinalIgnoreCase);

			VerifyHashtable(hash1, hash2, hash1.EqualityComparer);
		}

		[TestMethod]
		public void Ctor_IDictionary_Int_Invalid()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new HashtableEx(null, 1f)); // Dictionary is null

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), 0.09f)); // Load factor < 0.1f
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), 1.01f)); // Load factor > 1f

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), float.NaN)); // Load factor is NaN
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), float.PositiveInfinity)); // Load factor is infinity
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), float.NegativeInfinity)); // Load factor is infinity
		}

		[TestMethod]
		public void Ctor_IDictionary_Int_HashCodeProvider_Comparer()
		{
			// No exception
			var hash1 = new ComparableHashtable(new HashtableEx(), 1f);
			Assert.AreEqual(0, hash1.Count);

			hash1 = new ComparableHashtable(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(), 1f), 1f), 1f), 1f), 1f);
			Assert.AreEqual(0, hash1.Count);

			HashtableEx hash2 = Helpers.CreateIntHashtable(100);
			hash1 = new ComparableHashtable(hash2, 1f);

			VerifyHashtable(hash1, hash2, null);
		}

		[TestMethod]
		public void Ctor_IDictionary_IEqualityComparer()
		{
			// No exception
			var hash1 = new ComparableHashtable(new HashtableEx(), null);
			Assert.AreEqual(0, hash1.Count);

			hash1 = new ComparableHashtable(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(new HashtableEx(), null), null), null), null), null);
			Assert.AreEqual(0, hash1.Count);

			// Null comparer
			HashtableEx hash2 = Helpers.CreateIntHashtable(100);
			hash1 = new ComparableHashtable(hash2, null);
			VerifyHashtable(hash1, hash2, null);

			// Custom comparer
			hash2 = Helpers.CreateIntHashtable(100);
			IEqualityComparer comparer = StringComparer.CurrentCulture;
			hash1 = new ComparableHashtable(hash2, comparer);
			VerifyHashtable(hash1, hash2, comparer);
		}

		[TestMethod]
		public void Ctor_IDictionary_IEqualityComparer_NullDictionary_ThrowsArgumentNullException()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new HashtableEx((HashtableEx)null, null)); // Dictionary is null
		}

		[TestMethod]
		[DataRow(0, 0.1f)]
		[DataRow(10, 0.2f)]
		[DataRow(100, 0.3f)]
		[DataRow(1000, 1f)]
		public void Ctor_Int_Int(int capacity, float loadFactor)
		{
			var hash = new ComparableHashtable(capacity, loadFactor);
			VerifyHashtable(hash, null, null);
		}

		[TestMethod]
		[DataRow(0, 0.1f)]
		[DataRow(10, 0.2f)]
		[DataRow(100, 0.3f)]
		[DataRow(1000, 1f)]
		public void Ctor_Int_Int_HashCodeProvider_Comparer(int capacity, float loadFactor)
		{
			var hash = new ComparableHashtable(capacity, loadFactor, StringComparer.OrdinalIgnoreCase);
			VerifyHashtable(hash, null, hash.EqualityComparer);
		}

		[TestMethod]
		public void Ctor_Int_Int_GenerateNewPrime()
		{
			// The ctor for HashtableEx performs the following calculation:
			// rawSize = capacity / (loadFactor * 0.72)
			// If rawSize is > 3, then it calls HashHelpers.GetPrime(rawSize) to generate a prime.
			// Then, if the rawSize > 7,199,369 (the largest number in a list of known primes), we have to generate a prime programatically
			// This test makes sure this works.
			int capacity = 8000000;
			float loadFactor = 0.1f / 0.72f;
			try
			{
				var hash = new ComparableHashtable(capacity, loadFactor);
			}
			catch (OutOfMemoryException)
			{
				// On memory constrained devices, we can get an OutOfMemoryException, which we can safely ignore.
			}
		}

		[TestMethod]
		public void Ctor_Int_Int_Invalid()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(-1, 1f)); // Capacity < 0
			Assert.ThrowsExactly<ArgumentException>(() => new HashtableEx(int.MaxValue, 0.1f)); // Capacity / load factor > int.MaxValue

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, 0.09f)); // Load factor < 0.1f
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, 1.01f)); // Load factor > 1f

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, float.NaN)); // Load factor is NaN
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, float.PositiveInfinity)); // Load factor is infinity
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, float.NegativeInfinity)); // Load factor is infinity
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(10)]
		[DataRow(100)]
		[DataRow(1000)]
		public void Ctor_Int_IEqualityComparer(int capacity)
		{
			// Null comparer
			var hash = new ComparableHashtable(capacity, null);
			VerifyHashtable(hash, null, null);

			// Custom comparer
			IEqualityComparer comparer = StringComparer.CurrentCulture;
			hash = new ComparableHashtable(capacity, comparer);
			VerifyHashtable(hash, null, comparer);
		}

		[TestMethod]
		public void Ctor_Int_IEqualityComparer_Invalid()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(-1, null)); // Capacity < 0
			Assert.ThrowsExactly<ArgumentException>(() => new HashtableEx(int.MaxValue, null)); // Capacity / load factor > int.MaxValue
		}

		[TestMethod]
		public void Ctor_IDictionary_Int_IEqualityComparer()
		{
			// No exception
			var hash1 = new ComparableHashtable(new HashtableEx(), 1f, null);
			Assert.AreEqual(0, hash1.Count);

			hash1 = new ComparableHashtable(new HashtableEx(new HashtableEx(
				new HashtableEx(new HashtableEx(new HashtableEx(), 1f, null), 1f, null), 1f, null), 1f, null), 1f, null);
			Assert.AreEqual(0, hash1.Count);

			// Null comparer
			HashtableEx hash2 = Helpers.CreateIntHashtable(100);
			hash1 = new ComparableHashtable(hash2, 1f, null);
			VerifyHashtable(hash1, hash2, null);

			hash2 = Helpers.CreateIntHashtable(100);
			// Custom comparer
			IEqualityComparer comparer = StringComparer.CurrentCulture;
			hash1 = new ComparableHashtable(hash2, 1f, comparer);
			VerifyHashtable(hash1, hash2, comparer);
		}

		[TestMethod]
		public void Ctor_IDictionary_LoadFactor_IEqualityComparer_Invalid()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new HashtableEx(null, 1f, null)); // Dictionary is null

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), 0.09f, null)); // Load factor < 0.1f
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), 1.01f, null)); // Load factor > 1f

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), float.NaN, null)); // Load factor is NaN
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), float.PositiveInfinity, null)); // Load factor is infinity
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(new HashtableEx(), float.NegativeInfinity, null)); // Load factor is infinity
		}

		[TestMethod]
		[DataRow(0, 0.1f)]
		[DataRow(10, 0.2f)]
		[DataRow(100, 0.3f)]
		[DataRow(1000, 1f)]
		public void Ctor_Int_Int_IEqualityComparer(int capacity, float loadFactor)
		{
			// Null comparer
			var hash = new ComparableHashtable(capacity, loadFactor, null);
			VerifyHashtable(hash, null, null);
			Assert.IsNull(hash.EqualityComparer);

			// Custom compare
			IEqualityComparer comparer = StringComparer.CurrentCulture;
			hash = new ComparableHashtable(capacity, loadFactor, comparer);
			VerifyHashtable(hash, null, comparer);
		}

		[TestMethod]
		public void Ctor_Capacity_LoadFactor_IEqualityComparer_Invalid()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(-1, 1f, null)); // Capacity < 0
			Assert.ThrowsExactly<ArgumentException>(() => new HashtableEx(int.MaxValue, 0.1f, null)); // Capacity / load factor > int.MaxValue

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, 0.09f, null)); // Load factor < 0.1f
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, 1.01f, null)); // Load factor > 1f

			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, float.NaN, null)); // Load factor is NaN
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, float.PositiveInfinity, null)); // Load factor is infinity
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashtableEx(100, float.NegativeInfinity, null)); // Load factor is infinity
		}

		[TestMethod]
		public void Add_ReferenceType()
		{
			var hash1 = new HashtableEx();
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				// Value is a reference
				var foo = new Foo();
				hash2.Add("Key", foo);

				Assert.AreEqual("Hello World", ((Foo)hash2["Key"]).StringValue);

				// Changing original object should change the object stored in the HashtableEx
				foo.StringValue = "Goodbye";
				Assert.AreEqual("Goodbye", ((Foo)hash2["Key"]).StringValue);
			});
		}

		[TestMethod]
		public void Add_ClearRepeatedly()
		{
			const int Iterations = 2;
			const int Count = 2;

			var hash = new HashtableEx();
			for (int i = 0; i < Iterations; i++)
			{
				for (int j = 0; j < Count; j++)
				{
					string key = "Key: i=" + i + ", j=" + j;
					string value = "Value: i=" + i + ", j=" + j;
					hash.Add(key, value);
				}

				Assert.AreEqual(Count, hash.Count);
				hash.Clear();
			}
		}

		[TestMethod]
		public void AddRemove_LargeAmountNumbers()
		{
			// Generate a random 100,000 array of ints as test data
			var inputData = new int[100000];
			var random = new Random(341553);
			for (int i = 0; i < inputData.Length; i++)
			{
				inputData[i] = random.Next(7500000, int.MaxValue);
			}

			var hash = new HashtableEx();

			int count = 0;
			foreach (long number in inputData)
			{
				hash.Add(number, count++);
			}

			count = 0;
			foreach (long number in inputData)
			{
				Assert.AreEqual(hash[number], count);
				Assert.IsTrue(hash.ContainsKey(number));

				count++;
			}

			foreach (long number in inputData)
			{
				hash.Remove(number);
			}

			Assert.AreEqual(0, hash.Count);
		}

		[TestMethod]
		public void DuplicatedKeysWithInitialCapacity()
		{
			// Make rehash get called because to many items with duplicated keys have been added to the hashtable
			var hash = new HashtableEx(200);

			const int Iterations = 1600;
			for (int i = 0; i < Iterations; i += 2)
			{
				hash.Add(new BadHashCode(i), i.ToString());
				hash.Add(new BadHashCode(i + 1), (i + 1).ToString());

				hash.Remove(new BadHashCode(i));
				hash.Remove(new BadHashCode(i + 1));
			}

			for (int i = 0; i < Iterations; i++)
			{
				hash.Add(i.ToString(), i);
			}

			for (int i = 0; i < Iterations; i++)
			{
				Assert.AreEqual(i, hash[i.ToString()]);
			}
		}

		[TestMethod]
		public void DuplicatedKeysWithDefaultCapacity()
		{
			// Make rehash get called because to many items with duplicated keys have been added to the hashtable
			var hash = new HashtableEx();

			const int Iterations = 1600;
			for (int i = 0; i < Iterations; i += 2)
			{
				hash.Add(new BadHashCode(i), i.ToString());
				hash.Add(new BadHashCode(i + 1), (i + 1).ToString());

				hash.Remove(new BadHashCode(i));
				hash.Remove(new BadHashCode(i + 1));
			}

			for (int i = 0; i < Iterations; i++)
			{
				hash.Add(i.ToString(), i);
			}

			for (int i = 0; i < Iterations; i++)
			{
				Assert.AreEqual(i, hash[i.ToString()]);
			}
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(100)]
		public void Clone(int count)
		{
			HashtableEx hash1 = Helpers.CreateStringHashtable(count);
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				HashtableEx clone = (HashtableEx)hash2.Clone();

				Assert.AreEqual(hash2.Count, clone.Count);
				Assert.AreEqual(hash2.IsSynchronized, clone.IsSynchronized);
				Assert.AreEqual(hash2.IsFixedSize, clone.IsFixedSize);
				Assert.AreEqual(hash2.IsReadOnly, clone.IsReadOnly);

				for (int i = 0; i < clone.Count; i++)
				{
					string key = "Key_" + i;
					string value = "Value_" + i;

					Assert.IsTrue(clone.ContainsKey(key));
					Assert.IsTrue(clone.ContainsValue(value));
					Assert.AreEqual(value, clone[key]);
				}
			});
		}

		[TestMethod]
		public void Clone_IsShallowCopy()
		{
			var hash = new HashtableEx();
			for (int i = 0; i < 10; i++)
			{
				hash.Add(i, new Foo());
			}

			HashtableEx clone = (HashtableEx)hash.Clone();
			for (int i = 0; i < clone.Count; i++)
			{
				Assert.AreEqual("Hello World", ((Foo)clone[i]).StringValue);
				Assert.AreEqual(hash[i], clone[i]);
			}

			// Change object in original hashtable
			((Foo)hash[1]).StringValue = "Goodbye";
			Assert.AreEqual("Goodbye", ((Foo)clone[1]).StringValue);

			// Removing an object from the original hashtable doesn't change the clone
			hash.Remove(0);
			Assert.IsTrue(clone.Contains(0));
		}


		[TestMethod]
		public void ContainsKey()
		{
			HashtableEx hash1 = Helpers.CreateStringHashtable(100);
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				for (int i = 0; i < hash2.Count; i++)
				{
					string key = "Key_" + i;
					Assert.IsTrue(hash2.ContainsKey(key));
					Assert.IsTrue(hash2.Contains(key));
				}

				Assert.IsFalse(hash2.ContainsKey("Non Existent Key"));
				Assert.IsFalse(hash2.Contains("Non Existent Key"));

				Assert.IsFalse(hash2.ContainsKey(101));
				Assert.IsFalse(hash2.Contains("Non Existent Key"));

				string removedKey = "Key_1";
				hash2.Remove(removedKey);
				Assert.IsFalse(hash2.ContainsKey(removedKey));
				Assert.IsFalse(hash2.Contains(removedKey));
			});
		}

		[TestMethod]
		public void ContainsKey_EqualObjects()
		{
			var hash1 = new HashtableEx();
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				var foo1 = new Foo() { StringValue = "Goodbye" };
				var foo2 = new Foo() { StringValue = "Goodbye" };

				hash2.Add(foo1, 101);

				Assert.IsTrue(hash2.ContainsKey(foo2));
				Assert.IsTrue(hash2.Contains(foo2));

				int i1 = 0x10;
				int i2 = 0x100;
				long l1 = (((long)i1) << 32) + i2; // Create two longs with same hashcode
				long l2 = (((long)i2) << 32) + i1;

				hash2.Add(l1, 101);
				hash2.Add(l2, 101);      // This will cause collision bit of the first entry to be set
				Assert.IsTrue(hash2.ContainsKey(l1));
				Assert.IsTrue(hash2.Contains(l1));

				hash2.Remove(l1);         // Remove the first item
				Assert.IsFalse(hash2.ContainsKey(l1));
				Assert.IsFalse(hash2.Contains(l1));

				Assert.IsTrue(hash2.ContainsKey(l2));
				Assert.IsTrue(hash2.Contains(l2));
			});
		}

		[TestMethod]
		public void ContainsKey_NullKey_ThrowsArgumentNullException()
		{
			var hash1 = new HashtableEx();
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				Assert.ThrowsExactly<ArgumentNullException>(() => hash2.ContainsKey(null)); // Key is null
				Assert.ThrowsExactly<ArgumentNullException>(() => hash2.Contains(null)); // Key is null
			});
		}

		[TestMethod]
		public void ContainsValue()
		{
			HashtableEx hash1 = Helpers.CreateStringHashtable(100);
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				for (int i = 0; i < hash2.Count; i++)
				{
					string value = "Value_" + i;
					Assert.IsTrue(hash2.ContainsValue(value));
				}

				Assert.IsFalse(hash2.ContainsValue("Non Existent Value"));
				Assert.IsFalse(hash2.ContainsValue(101));
				Assert.IsFalse(hash2.ContainsValue(null));

				hash2.Add("Key_101", null);
				Assert.IsTrue(hash2.ContainsValue(null));

				string removedKey = "Key_1";
				string removedValue = "Value_1";
				hash2.Remove(removedKey);
				Assert.IsFalse(hash2.ContainsValue(removedValue));
			});
		}

		[TestMethod]
		public void ContainsValue_EqualObjects()
		{
			var hash1 = new HashtableEx();
			Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
			{
				var foo1 = new Foo() { StringValue = "Goodbye" };
				var foo2 = new Foo() { StringValue = "Goodbye" };

				hash2.Add(101, foo1);

				Assert.IsTrue(hash2.ContainsValue(foo2));
			});
		}

		[TestMethod]
		public void Keys_ModifyingHashtable_ModifiesCollection()
		{
			HashtableEx hash = Helpers.CreateStringHashtable(100);
			ICollection keys = hash.Keys;

			// Removing a key from the hashtable should update the Keys ICollection.
			// This means that the Keys ICollection no longer contains the key.
			hash.Remove("Key_0");

			IEnumerator enumerator = keys.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Assert.AreNotEqual("Key_0", enumerator.Current);
			}
		}

		[TestMethod]
		public void Remove_SameHashcode()
		{
			// We want to add and delete items (with the same hashcode) to the hashtable in such a way that the hashtable
			// does not expand but have to tread through collision bit set positions to insert the new elements. We do this
			// by creating a default hashtable of size 11 (with the default load factor of 0.72), this should mean that
			// the hashtable does not expand as long as we have at most 7 elements at any given time?

			var hash = new HashtableEx();
			var arrList = new ArrayList();
			for (int i = 0; i < 7; i++)
			{
				var hashConfuse = new BadHashCode(i);
				arrList.Add(hashConfuse);
				hash.Add(hashConfuse, i);
			}

			var rand = new Random(-55);

			int iCount = 7;
			for (int i = 0; i < 100; i++)
			{
				for (int j = 0; j < 7; j++)
				{
					Assert.AreEqual(hash[arrList[j]], ((BadHashCode)arrList[j]).Value);
				}

				// Delete 3 elements from the hashtable
				for (int j = 0; j < 3; j++)
				{
					int iElement = rand.Next(6);
					hash.Remove(arrList[iElement]);
					Assert.IsFalse(hash.ContainsValue(null));
					arrList.RemoveAt(iElement);

					int testInt = iCount++;
					var hashConfuse = new BadHashCode(testInt);
					arrList.Add(hashConfuse);
					hash.Add(hashConfuse, testInt);
				}
			}
		}

		[TestMethod]
		public void Values_ModifyingHashtable_ModifiesCollection()
		{
			HashtableEx hash = Helpers.CreateStringHashtable(100);
			ICollection values = hash.Values;

			// Removing a value from the hashtable should update the Values ICollection.
			// This means that the Values ICollection no longer contains the value.
			hash.Remove("Key_0");

			IEnumerator enumerator = values.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Assert.AreNotEqual("Value_0", enumerator.Current);
			}
		}

		private static void VerifyHashtable(ComparableHashtable hash1, HashtableEx hash2, IEqualityComparer ikc)
		{
			if (hash2 == null)
			{
				Assert.AreEqual(0, hash1.Count);
			}
			else
			{
				// Make sure that construtor imports all keys and values
				Assert.AreEqual(hash2.Count, hash1.Count);
				for (int i = 0; i < 100; i++)
				{
					Assert.IsTrue(hash1.ContainsKey(i));
					Assert.IsTrue(hash1.ContainsValue(i));
				}

				// Make sure the new and old hashtables are not linked
				hash2.Clear();
				for (int i = 0; i < 100; i++)
				{
					Assert.IsTrue(hash1.ContainsKey(i));
					Assert.IsTrue(hash1.ContainsValue(i));
				}
			}

			Assert.AreEqual(ikc, hash1.EqualityComparer);

			Assert.IsFalse(hash1.IsFixedSize);
			Assert.IsFalse(hash1.IsReadOnly);
			Assert.IsFalse(hash1.IsSynchronized);

			// Make sure we can add to the hashtable
			int count = hash1.Count;
			for (int i = count; i < count + 100; i++)
			{
				hash1.Add(i, i);
				Assert.IsTrue(hash1.ContainsKey(i));
				Assert.IsTrue(hash1.ContainsValue(i));
			}
		}

		private class ComparableHashtable : HashtableEx
		{
			public ComparableHashtable() : base() { }

			public ComparableHashtable(HashtableEx h) : base(h) { }

			public ComparableHashtable(int capacity) : base(capacity) { }

			public ComparableHashtable(int capacity, float loadFactor) : base(capacity, loadFactor) { }

			public ComparableHashtable(int capacity, IEqualityComparer ikc) : base(capacity, ikc) { }

			public ComparableHashtable(IEqualityComparer ikc) : base(ikc) { }

			public ComparableHashtable(HashtableEx d, float loadFactor) : base(d, loadFactor) { }

			public ComparableHashtable(HashtableEx d, IEqualityComparer ikc) : base(d, ikc) { }

			public ComparableHashtable(HashtableEx d, float loadFactor, IEqualityComparer ikc) : base(d, loadFactor, ikc) { }

			public ComparableHashtable(int capacity, float loadFactor, IEqualityComparer ikc) : base(capacity, loadFactor, ikc) { }

			public new IEqualityComparer EqualityComparer => base.EqualityComparer;
		}

		private class BadHashCode
		{
			public BadHashCode(int value)
			{
				Value = value;
			}

			public int Value { get; private set; }

			public override bool Equals(object o)
			{
				BadHashCode rhValue = o as BadHashCode;

				if (rhValue != null)
				{
					return Value.Equals(rhValue.Value);
				}
				else
				{
					throw new ArgumentException("is not BadHashCode type actual " + o.GetType(), nameof(o));
				}
			}

			// Return 0 for everything to force hash collisions.
			public override int GetHashCode() => 0;

			public override string ToString() => Value.ToString();
		}

		private class Foo
		{
			public string StringValue { get; set; } = "Hello World";

			public override bool Equals(object obj)
			{
				Foo foo = obj as Foo;
				return foo != null && StringValue == foo.StringValue;
			}

			public override int GetHashCode() => StringValue.GetHashCode();
		}
	}
}
