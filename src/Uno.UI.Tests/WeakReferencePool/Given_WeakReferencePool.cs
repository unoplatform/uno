using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests
{
	[TestClass]
	public class Given_WeakReferencePool
	{
		[TestInitialize]
		public void Initialize()
		{
			WeakReferencePool.ClearCache();
		}

		[TestCleanup]
		public void Cleanup()
		{
			WeakReferencePool.ClearCache();
		}

		[TestMethod]
		public void When_Empty()
		{
			var target = new object();

			var mr1 = WeakReferencePool.RentWeakReference(this, target);

			Assert.AreEqual(target, mr1.GetUnsafeTargetHandle().Target);
			Assert.AreEqual(this, mr1.Owner);
		}

		[TestMethod]
		public void When_Returned_Is_Reused()
		{
			var target1 = new object();
			var target2 = new object();

			var mr1 = WeakReferencePool.RentWeakReference(this, target1);

			var wr = mr1.GetUnsafeTargetHandle();

			Assert.AreEqual(target1, mr1.GetUnsafeTargetHandle().Target);
			Assert.AreEqual(this, mr1.Owner);

			WeakReferencePool.ReturnWeakReference(this, mr1);

			var mr2 = WeakReferencePool.RentWeakReference(this, target2);

			Assert.AreEqual(target2, mr2.GetUnsafeTargetHandle().Target);
			Assert.AreEqual(this, mr2.Owner);
			Assert.AreSame(wr.Target, mr2.GetUnsafeTargetHandle().Target);
		}

		[TestMethod]
		public void When_Not_Owned_Returned()
		{
			var target1 = new object();
			var target2 = new object();

			var mr1 = WeakReferencePool.RentWeakReference(target1, target1);

			var wr = mr1.GetUnsafeTargetHandle();

			Assert.AreEqual(target1, mr1.GetUnsafeTargetHandle().Target);
			Assert.AreEqual(target1, mr1.Owner);

			WeakReferencePool.ReturnWeakReference(this, mr1);

			var mr2 = WeakReferencePool.RentWeakReference(this, target2);

			Assert.AreEqual(target2, mr2.GetUnsafeTargetHandle().Target);
			Assert.AreEqual(this, mr2.Owner);
			Assert.AreNotSame(wr, mr2.GetUnsafeTargetHandle());
		}

		[TestMethod]
		public void When_IWeakReferenceProvider()
		{
			var target = new MyProvider();

			var mr1 = WeakReferencePool.RentWeakReference(this, target);

			Assert.AreEqual(target, mr1.GetUnsafeTargetHandle().Target);
			Assert.AreSame(target.WeakReference.GetUnsafeTargetHandle(), mr1.GetUnsafeTargetHandle());
			Assert.AreEqual(target, mr1.Owner);
		}

		[TestMethod]
		public void When_WeakReferenceProvider_Reused()
		{
			var o1 = new Object();

			var mr1 = WeakReferencePool.RentWeakReference(this, o1);
			WeakReferencePool.ReturnWeakReference(this, mr1);

			var target = new MyProvider();
			Assert.AreEqual(target, target.WeakReference.Target);

			var mr2 = WeakReferencePool.RentWeakReference(target, o1);
			WeakReferencePool.ReturnWeakReference(target, mr2);

			Assert.AreEqual(target, target.WeakReference.Target);
		}

		[TestMethod]
		public void When_WeakReferenceProvider_Collected()
		{
			var o1 = new Object();

			void test1()
			{
				var mr1 = WeakReferencePool.RentWeakReference(this, o1);
				WeakReferencePool.ReturnWeakReference(this, mr1);
			}

			test1();

			Assert.AreEqual(2, WeakReferencePool.PooledReferences);

			void test2()
			{
				var mr2 = WeakReferencePool.RentWeakReference(this, o1);
				WeakReferencePool.ReturnWeakReference(this, mr2);
			}

			test2();

			Assert.AreEqual(2, WeakReferencePool.PooledReferences);

			(WeakReference refref, WeakReference ownerRef, WeakReference targetRef) CreateUnreferenced()
			{
				var o2 = new Object();
				var mr2 = WeakReferencePool.RentWeakReference(o1, o2);

				return (new WeakReference(mr2), new WeakReference(mr2.GetUnsafeOwnerHandle()), new WeakReference(mr2.GetUnsafeTargetHandle()));
			}

			var r = CreateUnreferenced();

			void ScopedTest()
			{
				// This change is needed after some unknown scoped references 
				// changes in .NET Framework. Adding an explicit method to contain
				// the references makes the test work properly.

				Assert.IsNotNull(r.ownerRef.Target);
				Assert.IsNotNull(r.targetRef.Target);
				Assert.IsNotNull(r.refref.Target);

				Assert.AreEqual(0, WeakReferencePool.PooledReferences);
			}

			ScopedTest();

			GCCondition(50, () => r.refref.Target == null);

			Assert.IsNull(r.refref.Target);

			GCCondition(5, () => r.ownerRef.Target == null && r.targetRef.Target == null);

			Assert.IsNull(r.ownerRef.Target);
			Assert.IsNull(r.targetRef.Target);

			Assert.AreEqual(0, WeakReferencePool.PooledReferences);
		}

		[TestMethod]
		public void When_Null_Target()
		{
			var mr1 = WeakReferencePool.RentWeakReference(this, null);
			WeakReferencePool.ReturnWeakReference(this, mr1);
			Assert.AreEqual(1, WeakReferencePool.PooledReferences);
		}

		[TestMethod]
		public void When_Null_Owner()
		{
			var o1 = new Object();

			var mr1 = WeakReferencePool.RentWeakReference(null, o1);
			WeakReferencePool.ReturnWeakReference(null, mr1);
			Assert.AreEqual(1, WeakReferencePool.PooledReferences);
		}

		[TestMethod]
		public void When_Null_Target_And_Owner()
		{
			var o1 = new Object();

			var mr1 = WeakReferencePool.RentWeakReference(null, null);
			WeakReferencePool.ReturnWeakReference(null, mr1);
			Assert.AreEqual(0, WeakReferencePool.PooledReferences);
		}

		[TestMethod]
		public void When_TryGetTarget_With_Alive_Target()
		{
			var target = new MyClass { Value = 42 };
			var mr = WeakReferencePool.RentWeakReference(this, target);

			var result = mr.TryGetTarget<MyClass>(out var retrieved);

			Assert.IsTrue(result);
			Assert.IsNotNull(retrieved);
			Assert.AreEqual(42, retrieved.Value);
			Assert.AreSame(target, retrieved);
		}

		[TestMethod]
		public void When_TryGetTarget_With_Wrong_Type()
		{
			var target = new MyClass { Value = 42 };
			var mr = WeakReferencePool.RentWeakReference(this, target);

			var result = mr.TryGetTarget<MyOtherClass>(out var retrieved);

			Assert.IsFalse(result);
			Assert.IsNull(retrieved);
		}

		[TestMethod]
		public void When_TryGetTarget_With_Base_Type()
		{
			var target = new MyDerivedClass { Value = 42, DerivedValue = 100 };
			var mr = WeakReferencePool.RentWeakReference(this, target);

			var result = mr.TryGetTarget<MyClass>(out var retrieved);

			Assert.IsTrue(result);
			Assert.IsNotNull(retrieved);
			Assert.AreEqual(42, retrieved.Value);
		}

		[TestMethod]
		public void When_TryGetTarget_With_Disposed_Reference()
		{
			var target = new MyClass { Value = 42 };
			var mr = WeakReferencePool.RentWeakReference(this, target);
			mr.Dispose();

			var result = mr.TryGetTarget<MyClass>(out var retrieved);

			Assert.IsFalse(result);
			Assert.IsNull(retrieved);
		}

		[TestMethod]
		public void When_TryGetTarget_With_Collected_Target()
		{
			var mr = CreateWeakReferenceToCollectedObject();

			GCCondition(50, () => !mr.IsAlive);

			var result = mr.TryGetTarget<MyClass>(out var retrieved);

			Assert.IsFalse(result);
			Assert.IsNull(retrieved);
		}

		[TestMethod]
		public void When_TryGetTarget_With_Null_Target()
		{
			var mr = WeakReferencePool.RentWeakReference(this, null);

			var result = mr.TryGetTarget<MyClass>(out var retrieved);

			Assert.IsFalse(result);
			Assert.IsNull(retrieved);
		}

		private ManagedWeakReference CreateWeakReferenceToCollectedObject()
		{
			var target = new MyClass { Value = 99 };
			return WeakReferencePool.RentWeakReference(this, target);
		}

		private void GCCondition(int count, Func<bool> predicate)
		{
			int i = count;

			while (i-- > 0)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				if (predicate())
				{
					break;
				}

				Thread.Sleep(50);
			}
		}

		class MyProvider : IWeakReferenceProvider
		{
			public MyProvider()
			{
				WeakReference = WeakReferencePool.RentSelfWeakReference(this);
			}

			public ManagedWeakReference WeakReference { get; }
		}

		private class MyClass
		{
			public int Value { get; set; }
		}

		private class MyOtherClass
		{
			public string Text { get; set; }
		}

		private class MyDerivedClass : MyClass
		{
			public int DerivedValue { get; set; }
		}
	}
}
