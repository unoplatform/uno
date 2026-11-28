using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests
{
	/// <summary>
	/// Pins how bindings use <see cref="ManagedGCHandle"/>s: every instance is finalizable, so a binding that
	/// creates new ones on each DataContext change leaves work for the finalizer and promotes garbage to gen1.
	/// </summary>
	[UITestClass]
	public class Given_BindingWeakReferences
	{
		private const int Sources = 4;

		// An empty pool: a reference returned to a full pool (MaxReferences) isn't disposed, and the IsDisposed asserts
		// below would fail for that reason alone.
		[TestInitialize]
		public void Initialize() => WeakReferencePool.ClearCache();

		[TestCleanup]
		public void Cleanup()
		{
			WeakReferencePool.ClearCache();
		}

		[TestMethod]
		[DataRow("A.B")]
		[DataRow("A")]
		[DataRow("A.B.Length")]
		public void When_DataContext_Retargeted_Then_No_New_Handles(string path)
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath(path) });

			var sources = CreateSources();

			// Warm up: the first passes create the handles each binding object keeps for its lifetime,
			// and fill the pool with the ones that get recycled.
			for (var i = 0; i < 4 * Sources; i++)
			{
				sut.DataContext = sources[i % Sources];
			}

			var pooled = FillPool();

			for (var i = 0; i < 200; i++)
			{
				sut.DataContext = sources[i % Sources];
				AssertPoolUnchanged(pooled, $"ManagedGCHandles created by 200 DataContext changes (after change {i})");
			}
		}

		[TestMethod]
		public void When_DataContext_Retargeted_Through_Null_Then_No_New_Handles()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });

			var sources = CreateSources();

			for (var i = 0; i < 4 * Sources; i++)
			{
				sut.DataContext = sources[i % Sources];
				sut.DataContext = null;
			}

			var pooled = FillPool();

			for (var i = 0; i < 200; i++)
			{
				sut.DataContext = sources[i % Sources];
				sut.DataContext = null;
				AssertPoolUnchanged(pooled, $"Pooled handles (after change {i})");
			}
		}

		[TestMethod]
		public void When_Inner_Value_Retargeted_Then_No_New_Handles()
		{
			// Re-targets the second item of the chain through INPC rather than through DataContext.
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });

			var source = new Outer { A = new Inner { B = "0" } };
			sut.DataContext = source;

			var inners = Enumerable.Range(0, Sources).Select(i => new Inner { B = i.ToString() }).ToArray();

			for (var i = 0; i < 4 * Sources; i++)
			{
				source.A = inners[i % Sources];
			}

			var pooled = FillPool();

			for (var i = 0; i < 200; i++)
			{
				source.A = inners[i % Sources];
				Assert.AreEqual(inners[i % Sources].B, sut.Tag);
				AssertPoolUnchanged(pooled, $"Pooled handles (after change {i})");
			}
		}

		[TestMethod]
		public void When_DataContext_Retargeted_Then_Old_Source_Detached()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });

			var sources = CreateSources();

			for (var i = 0; i < 3 * Sources; i++)
			{
				sut.DataContext = sources[i % Sources];
			}

			var current = sources[(3 * Sources - 1) % Sources];
			Assert.AreEqual(current.A.B, sut.Tag);

			// Changes on sources that are no longer the DataContext must not reach the target,
			// whether they come from the root or from the inner item.
			foreach (var old in sources.Where(s => s != current))
			{
				old.A.B = "stale";
				old.A = new Inner { B = "stale too" };
			}

			Assert.AreEqual(current.A.B, sut.Tag);

			// The current source still updates the target, through both items of the path.
			current.A.B = "updated";
			Assert.AreEqual("updated", sut.Tag);

			current.A = new Inner { B = "replaced" };
			Assert.AreEqual("replaced", sut.Tag);

			current.A.B = "updated again";
			Assert.AreEqual("updated again", sut.Tag);

			Assert.AreEqual(1, current.PropertyChangedSubscribers, "One subscriber on the current root");
			Assert.IsTrue(sources.Where(s => s != current).All(s => s.PropertyChangedSubscribers == 0), "Old roots unsubscribed");
		}

		[TestMethod]
		public void When_Binding_Path_Disposed_Then_Rented_References_Returned()
		{
			var source = new Outer { A = new Inner { B = "0" } };

			var path = new BindingPath("A.B", null);
			path.DataContext = source;

			var pathStorage = GetWeakDataContext(path);
			var items = path.GetPathItems().ToArray();
			Assert.AreEqual(2, items.Length);

			var headStorage = GetWeakDataContext(items[0]);
			var tailStorage = GetWeakDataContext(items[1]);

			// The head item shares the path's reference: only the path may return it.
			Assert.AreSame(pathStorage, headStorage);
			Assert.AreNotSame(pathStorage, tailStorage);

			path.Dispose();

			Assert.IsTrue(pathStorage.IsDisposed, "The path returned its reference");
			Assert.IsTrue(tailStorage.IsDisposed, "The tail item returned its reference");
			Assert.AreEqual(0, source.PropertyChangedSubscribers);
			Assert.AreEqual(0, source.A.PropertyChangedSubscribers);
		}

		[TestMethod]
		public void When_DataContext_Set_After_Dispose_Then_Nothing_Rented()
		{
			var path = new BindingPath("A.B", null);
			path.DataContext = new Outer { A = new Inner { B = "0" } };
			path.Dispose();

			var pooled = WeakReferencePool.PooledReferences;
			path.DataContext = new Outer { A = new Inner { B = "1" } };

			Assert.AreEqual(pooled, WeakReferencePool.PooledReferences, "A disposed path rents nothing");
			Assert.IsNull(path.DataContext);
		}

		[TestMethod]
		public void When_Binding_Path_Retargeted_Then_Shared_Reference_Returned_Once()
		{
			var sources = CreateSources();
			var path = new BindingPath("A.B", null);

			path.DataContext = sources[0];
			var first = GetWeakDataContext(path);
			var firstTail = GetWeakDataContext(path.GetPathItems().ElementAt(1));

			path.DataContext = sources[1];

			Assert.IsTrue(first.IsDisposed, "The previous path reference is returned");
			Assert.IsTrue(firstTail.IsDisposed, "The previous tail reference is returned");
			Assert.AreSame(GetWeakDataContext(path), GetWeakDataContext(path.GetPathItems().First()));
			Assert.IsFalse(GetWeakDataContext(path).IsDisposed);
			Assert.AreEqual(sources[1].A.B, path.Value);

			// A returned reference reads as dead, rather than as whatever its recycled handle points to now.
			Assert.IsFalse(first.TryGetTarget<object>(out _));
			Assert.IsFalse(first.IsAlive);
		}

		[TestMethod]
		public void When_Binding_Replaced_Then_Old_Source_Detached()
		{
			var sut = new Border();
			var source = new Outer { A = new Inner { B = "0" } };
			sut.DataContext = source;

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });
			Assert.AreEqual("0", sut.Tag);
			Assert.AreEqual(1, source.PropertyChangedSubscribers);

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A") });
			Assert.AreSame(source.A, sut.Tag);

			// One subscriber left: the new binding's.
			Assert.AreEqual(1, source.PropertyChangedSubscribers);
			Assert.AreEqual(0, source.A.PropertyChangedSubscribers);
		}

		[TestMethod]
		public void When_XBind_On_DataContext_Replaced_Then_Source_Changes_Do_Not_Throw()
		{
			var sut = new Border();
			var source = new Outer { A = new Inner { B = "0" } };
			sut.DataContext = source;

			var xBind = new Binding { Path = new PropertyPath("A.B"), Mode = BindingMode.OneWay };
			xBind.SetBindingXBindProvider(null, s => (true, ((Outer)s).A?.B), null, new[] { "A.B" });
			sut.SetBinding(FrameworkElement.TagProperty, xBind);
			Assert.AreEqual("0", sut.Tag);

			// Disposes the x:Bind expression, which returns the DataContext reference its observed paths share.
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A") });

			source.A.B = "1";
			source.A = new Inner { B = "2" };

			Assert.AreSame(source.A, sut.Tag);
			Assert.AreEqual(1, source.PropertyChangedSubscribers, "Only the new binding observes the source");
		}

		[TestMethod]
		public void When_Source_Is_Not_A_DependencyObject_Then_One_Value_Handler_Kept()
		{
			_ = new Border(); // Registers the DependencyObject property-changed handler.
			var path = new BindingPath("A", null);
			for (var i = 0; i < 3; i++)
			{
				path.DataContext = new Outer { A = new Inner() };
				path.DataContext = null;
			}

			var item = path.GetPathItems().Single();
			var field = item.GetType().GetField("_valueHandlers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var handlers = (object[])field.GetValue(item);

			// Only the INotifyPropertyChanged registration applies; the DependencyObject one never subscribes.
			Assert.AreEqual(1, handlers.Count(h => h is not null));
			path.Dispose();
		}

		[TestMethod]
		public void When_Binding_Path_With_Three_Items_Disposed_Then_No_New_Handles()
		{
			var source = new Outer { A = new Inner { B = "0" } };

			var path = new BindingPath("A.B.Length", null);
			path.DataContext = source;
			Assert.AreEqual(1, path.Value);

			var items = path.GetPathItems().ToArray();
			Assert.AreEqual(3, items.Length);
			var middleStorage = GetWeakDataContext(items[1]);

			FillPool();
			var pooledHandles = PooledHandles();
			path.Dispose();

			AssertNoneRented(pooledHandles, "Disposing must not re-subscribe the middle item");
			Assert.IsTrue(middleStorage.IsDisposed, "The middle item returned its reference");
			Assert.IsTrue(items.All(i => GetWeakDataContext(i) is null), "Every item released its reference");
			Assert.AreEqual(0, source.PropertyChangedSubscribers);
			Assert.AreEqual(0, source.A.PropertyChangedSubscribers);
		}

		[TestMethod]
		public void When_DataContext_Cleared_Then_Previous_Values_Collectable()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });

			var (outer, inner) = BindThenClear(sut);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// The binding's value handlers live as long as the binding: they must not keep old sources alive.
			Assert.IsFalse(outer.IsAlive, "The old DataContext is collectable");
			Assert.IsFalse(inner.IsAlive, "The old inner value is collectable");
			GC.KeepAlive(sut);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference outer, WeakReference inner) BindThenClear(Border sut)
		{
			var outer = new Outer { A = new Inner { B = "0" } };
			sut.DataContext = outer;

			// An INPC change makes the head item's handler record the new value as its PreviousValue.
			var inner = new Inner { B = "1" };
			outer.A = inner;
			Assert.AreEqual("1", sut.Tag);

			sut.DataContext = null;
			Assert.IsNull(sut.Tag);

			return (new WeakReference(outer), new WeakReference(inner));
		}

		[TestMethod]
		public void When_Binding_Replaced_Then_Resume_Keeps_Target_And_Source()
		{
			var sut = new Border();
			var vm = new Pair { A = "a", B = "b" };
			sut.DataContext = vm;

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A") });

			// Disposes the first expression, which stays in the store's binding list.
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("B"), Mode = BindingMode.TwoWay });
			Assert.AreEqual("b", sut.Tag);

			sut.SuspendBindings();
			sut.ResumeBindings();

			Assert.AreEqual("b", vm.B, "The disposed expression's write must not reach the source through the live TwoWay binding");
			Assert.AreEqual("b", sut.Tag);
		}

		[TestMethod]
		public void When_Binding_Replaced_Then_Resume_Does_Not_Write_From_Disposed_Expression()
		{
			var sut = new Border();
			sut.DataContext = new Pair { A = "a", B = "b" };

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A") });
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("B") });

			var changes = 0;
			sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (s, e) => changes++);

			sut.SuspendBindings();
			sut.ResumeBindings();

			Assert.AreEqual(0, changes, "Tag changes during Resume");
			Assert.AreEqual("b", sut.Tag);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("fallback")]
		public void When_Binding_Cleared_Then_Local_Value_Survives_Resume_And_Reapply(string fallbackValue)
		{
			var sut = new Border();
			sut.DataContext = new Pair { A = "a" };

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A"), FallbackValue = fallbackValue });
			var expression = sut.GetBindingExpression(FrameworkElement.TagProperty);

			// ClearValue disposes the expression, which stays in the store's binding list.
			sut.ClearValue(FrameworkElement.TagProperty);
			sut.Tag = "local";

			sut.SuspendBindings();
			sut.ResumeBindings();
			Assert.AreEqual("local", sut.Tag, "After Resume");

			// What a theme change does to a binding with a {ThemeResource} FallbackValue or TargetNullValue.
			expression.Reapply();
			Assert.AreEqual("local", sut.Tag, "After Reapply");
		}

		[TestMethod]
		public void When_DependencyObject_And_Inpc_Sources_Alternate_Then_Only_Current_Source_Updates()
		{
			// The INotifyPropertyChanged and the DependencyProperty registrations each keep a handler.
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("Tag") });

			var doSources = new[] { new Border { Tag = "do0" }, new Border { Tag = "do1" } };
			var inpcSources = new[] { new TagHolder { Tag = "vm0" }, new TagHolder { Tag = "vm1" } };

			for (var i = 0; i < 8; i++)
			{
				sut.DataContext = (i % 2 == 0) ? doSources[(i / 2) % 2] : inpcSources[(i / 2) % 2];
			}

			Assert.AreEqual("vm1", sut.Tag);
			doSources[0].Tag = "stale";
			doSources[1].Tag = "stale";
			inpcSources[0].Tag = "stale";
			Assert.AreEqual("vm1", sut.Tag);

			sut.DataContext = doSources[0];
			doSources[0].Tag = "live";
			Assert.AreEqual("live", sut.Tag);
			inpcSources[1].Tag = "stale";
			doSources[1].Tag = "stale again";
			Assert.AreEqual("live", sut.Tag);
		}

		[TestMethod]
		public void When_DataContext_Retargeted_Then_TwoWay_Writes_Current_Source_Only()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B"), Mode = BindingMode.TwoWay });

			var sources = CreateSources();
			for (var i = 0; i < 2 * Sources; i++)
			{
				sut.DataContext = sources[i % Sources];
			}

			sut.Tag = "written";

			var current = sources[(2 * Sources - 1) % Sources];
			Assert.AreEqual("written", current.A.B);
			Assert.IsTrue(sources.Where(s => s != current).All(s => s.A.B != "written"));
		}

		[TestMethod]
		public void When_DataContext_Retargeted_Then_Indexer_Follows_Current_Collection()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("Items[0]") });

			var c0 = new ObservableCollection<string> { "a0" };
			var c1 = new ObservableCollection<string> { "a1" };
			var s0 = new ItemsHolder { Items = c0 };
			var s1 = new ItemsHolder { Items = c1 };

			for (var i = 0; i < 4; i++)
			{
				sut.DataContext = (i % 2 == 0) ? s0 : s1;
			}

			Assert.AreEqual("a1", sut.Tag);
			c0[0] = "stale";
			Assert.AreEqual("a1", sut.Tag);
			c1[0] = "live";
			Assert.AreEqual("live", sut.Tag);

			s1.Items = new ObservableCollection<string> { "replaced" };
			Assert.AreEqual("replaced", sut.Tag);
			c1[0] = "stale again";
			Assert.AreEqual("replaced", sut.Tag);
		}

		[TestMethod]
		public void When_Stale_Source_Raises_Then_Kept_Handlers_Ignore_It()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });

			var sources = CreateSources();
			for (var i = 0; i < 2 * Sources; i++)
			{
				sut.DataContext = sources[i % Sources];
			}

			var current = sources[(2 * Sources - 1) % Sources];
			var inner = new Inner { B = "new" };
			current.A = inner;
			Assert.AreEqual("new", sut.Tag);

			sources[0].A = new Inner { B = "stale" };
			Assert.AreEqual("new", sut.Tag);

			inner.B = "changed";
			Assert.AreEqual("changed", sut.Tag);
		}

		[TestMethod]
		public void When_Retargeted_From_Source_Callback_Then_Stale_Callback_Is_Ignored()
		{
			var first = new Border { Tag = "first" };
			var second = new Border { Tag = "second" };
			var sut = new Border();

			// Registered before the binding's own callback, so it runs first and retargets while the dispatch is under way.
			first.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (s, e) => sut.DataContext = second);

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("Tag") });
			sut.DataContext = first;
			Assert.AreEqual("first", sut.Tag);

			var seen = new System.Collections.Generic.List<object>();
			sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (s, e) => seen.Add(sut.Tag));

			first.Tag = "changed";

			Assert.AreEqual("second", sut.Tag);
			CollectionAssert.DoesNotContain(seen, "changed", "A callback of the replaced source must not reach the new subscription");

			first.Tag = "again";
			Assert.AreEqual("second", sut.Tag);

			second.Tag = "live";
			Assert.AreEqual("live", sut.Tag);
		}

		[TestMethod]
		public void When_Binding_Disposed_During_Source_Callback_Then_Late_Callback_Is_Ignored()
		{
			var source = new Border { Tag = "a" };
			var sut = new Border { DataContext = source };

			source.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (s, e) => sut.ClearValue(FrameworkElement.TagProperty));
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("Tag") });

			source.Tag = "b";

			Assert.IsNull(sut.Tag, "The cleared binding must not re-apply the late callback");
		}

		[TestMethod]
		public void When_XBind_With_Several_Paths_Replaced_Then_All_Paths_Unsubscribed()
		{
			var sut = new Border();
			var source = new Outer { A = new Inner { B = "0" } };
			sut.DataContext = source;

			var xBind = new Binding { Path = new PropertyPath("A.B"), Mode = BindingMode.OneWay };
			xBind.SetBindingXBindProvider(null, s => (true, ((Outer)s).A?.B), null, new[] { "A.B", "A" });
			sut.SetBinding(FrameworkElement.TagProperty, xBind);
			Assert.AreEqual("0", sut.Tag);
			Assert.AreEqual(2, source.PropertyChangedSubscribers, "One per observed path");
			Assert.AreEqual(1, source.A.PropertyChangedSubscribers);

			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A") });

			Assert.AreEqual(1, source.PropertyChangedSubscribers, "Only the new binding observes the root");
			Assert.AreEqual(0, source.A.PropertyChangedSubscribers, "No observed path left on the inner value");
		}

		[TestMethod]
		public void When_Binding_Replaced_By_Its_Own_Target_Callback_During_Retarget_Then_No_Exception()
		{
			var sut = new Border();
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A.B") });

			var sources = CreateSources();
			sut.DataContext = sources[0];

			var replaced = false;
			sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (s, e) =>
			{
				if (!replaced)
				{
					// Disposes the expression while its path is in the middle of applying the new DataContext.
					replaced = true;
					sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("A") });
				}
			});

			sut.DataContext = sources[1];

			Assert.IsTrue(replaced);
			Assert.AreSame(sources[1].A, sut.Tag);

			sut.DataContext = sources[2];
			Assert.AreSame(sources[2].A, sut.Tag);
			Assert.AreEqual(0, sources[0].PropertyChangedSubscribers + sources[1].PropertyChangedSubscribers);
		}

		[TestMethod]
		public void When_Reference_Returned_Then_TargetOrNull_Is_Null_And_Second_Return_Ignored()
		{
			var owner = new object();
			var target = new object();

			var reference = WeakReferencePool.RentWeakReference(owner, target);
			Assert.AreSame(target, reference.TargetOrNull);

			WeakReferencePool.ReturnWeakReference(owner, reference);
			var pooled = WeakReferencePool.PooledReferences;

			// A reader racing the return (an INotifyPropertyChanged raised off the UI thread) reads null rather than throwing.
			Assert.IsNull(reference.TargetOrNull);

			WeakReferencePool.ReturnWeakReference(owner, reference);
			Assert.AreEqual(pooled, WeakReferencePool.PooledReferences, "A second return pushes nothing");

			GC.KeepAlive(owner);
			GC.KeepAlive(target);
		}

		[TestMethod]
		public void When_Explicit_Source_Set_Again_To_Same_Element_Then_Reference_Kept()
		{
			// What ElementName bindings do on every Loaded.
			var sut = new Border();

			// Not a DependencyObject: those provide their own self reference, which is the same on every rent anyway.
			var element = new TagHolder { Tag = "element" };
			sut.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath("Tag"), Source = element });
			Assert.AreEqual("element", sut.Tag);

			var expression = sut.GetBindingExpression(FrameworkElement.TagProperty);
			var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
			var store = typeof(BindingExpression).GetField("_explicitSourceStore", flags);
			var explicitSource = typeof(BindingExpression).GetProperty("ExplicitSource", flags);

			var before = store.GetValue(expression);
			explicitSource.SetValue(expression, element);
			Assert.AreSame(before, store.GetValue(expression), "Same element: same reference");

			var other = new TagHolder();
			explicitSource.SetValue(expression, other);
			Assert.AreNotSame(before, store.GetValue(expression), "Another element: a new reference");
		}

		// A ManagedGCHandle is created only when a rent finds the pool empty (WeakReferencePool.RentFromPool). The tests
		// give the pool a cushion first: a path that returns every handle it rents leaves the pool at the same size, and
		// with the cushion the pool never runs dry, so nothing is created. A path that rents without returning drains it.
		private const int PoolCushion = 64;

		/// <summary>Puts at least <see cref="PoolCushion"/> handles in the pool and returns its size.</summary>
		private static int FillPool()
		{
			var rented = Enumerable.Range(0, PoolCushion / 2)
				.Select(_ => new object())
				.Select(owner => (owner, reference: WeakReferencePool.RentWeakReference(owner, new object())))
				.ToArray();

			foreach (var (owner, reference) in rented)
			{
				WeakReferencePool.ReturnWeakReference(owner, reference);
			}

			var pooled = WeakReferencePool.PooledReferences;
			Assert.IsTrue(pooled >= PoolCushion, $"premise: {pooled} pooled handles");
			return pooled;
		}

		private static void AssertPoolUnchanged(int pooled, string message)
			=> Assert.AreEqual(pooled, WeakReferencePool.PooledReferences, message);

		/// <summary>The handles in the pool (its private _weakReferencePool stack).</summary>
		private static ManagedGCHandle[] PooledHandles()
		{
			var field = typeof(WeakReferencePool).GetField("_weakReferencePool", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			Assert.IsNotNull(field, "No WeakReferencePool._weakReferencePool");
			return ((System.Collections.Generic.Stack<ManagedGCHandle>)field.GetValue(null)).ToArray();
		}

		/// <summary>A rent takes a handle out of the pool, so none was rented if every handle pooled before is still there.</summary>
		private static void AssertNoneRented(ManagedGCHandle[] pooledBefore, string message)
		{
			var pooledAfter = new System.Collections.Generic.HashSet<ManagedGCHandle>(PooledHandles(), System.Collections.Generic.ReferenceEqualityComparer.Instance);
			Assert.IsTrue(pooledBefore.All(pooledAfter.Contains), message);
		}

		private static ManagedWeakReference GetWeakDataContext(object pathOrItem)
		{
			var field = pathOrItem.GetType().GetField("_dataContextWeakStorage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.IsNotNull(field, $"No _dataContextWeakStorage on {pathOrItem.GetType()}");
			return (ManagedWeakReference)field.GetValue(pathOrItem);
		}

		private static Outer[] CreateSources()
			=> Enumerable.Range(0, Sources).Select(i => new Outer { A = new Inner { B = "value " + i } }).ToArray();

		public abstract class Notifier : INotifyPropertyChanged
		{
			private PropertyChangedEventHandler _propertyChanged;

			public event PropertyChangedEventHandler PropertyChanged
			{
				add => _propertyChanged += value;
				remove => _propertyChanged -= value;
			}

			public int PropertyChangedSubscribers => _propertyChanged?.GetInvocationList().Length ?? 0;

			protected void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
			{
				field = value;
				_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}
		}

		public class Outer : Notifier
		{
			private Inner _a;
			public Inner A { get => _a; set => Set(ref _a, value); }
		}

		public class Inner : Notifier
		{
			private string _b;
			public string B { get => _b; set => Set(ref _b, value); }
		}

		public class Pair : Notifier
		{
			private string _a, _b;
			public string A { get => _a; set => Set(ref _a, value); }
			public string B { get => _b; set => Set(ref _b, value); }
		}

		public class TagHolder : Notifier
		{
			private object _tag;
			public object Tag { get => _tag; set => Set(ref _tag, value); }
		}

		public class ItemsHolder : Notifier
		{
			private ObservableCollection<string> _items;
			public ObservableCollection<string> Items { get => _items; set => Set(ref _items, value); }
		}
	}
}
