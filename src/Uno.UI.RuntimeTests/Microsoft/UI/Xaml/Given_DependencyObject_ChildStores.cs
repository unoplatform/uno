using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// The children a DependencyObject propagates its inherited properties to: what registering them costs, and which
	// children a propagation reaches when its callbacks add or remove children.
	[TestClass]
	[RunsOnUIThread]
	public class Given_DependencyObject_ChildStores
	{
		private static long AllocatedByAddingChildren(int count)
		{
			var children = Enumerable.Range(0, count).Select(_ => new Border()).ToArray();
			var panel = new StackPanel();

			var before = GC.GetAllocatedBytesForCurrentThread();
			foreach (var child in children)
			{
				panel.Children.Add(child);
			}

			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		[TestMethod]
		public void When_Children_Added_Allocations_Grow_Linearly()
		{
			// Registering a child used to copy the parent's whole list of children: 8 bytes for each pair of children.
			// With bytes(n) = a·n + q·n²/2, bytes(2N) - 2·bytes(N) = q·N².
			const int N = 2000;
			AllocatedByAddingChildren(50); // Warm up.
			var single = AllocatedByAddingChildren(N);
			var twice = AllocatedByAddingChildren(2 * N);

			var perPair = (double)(twice - 2 * single) / ((double)N * N);
			Assert.IsTrue(perPair < 2, $"Adding {2 * N} children allocated {twice:N0} bytes against {single:N0} for {N}: {perPair:0.00} bytes per pair of children beyond linear growth.");
		}

		private static (StackPanel panel, Border[] children, List<Border> reached) CreatePanel(int count, object marker)
		{
			var panel = new StackPanel();
			var children = Enumerable.Range(0, count).Select(i => new Border { Tag = i }).ToArray();
			var reached = new List<Border>();
			foreach (var child in children)
			{
				panel.Children.Add(child);
				child.DataContextChanged += (s, e) =>
				{
					if (ReferenceEquals(e.NewValue, marker))
					{
						reached.Add((Border)s);
					}
				};
			}

			return (panel, children, reached);
		}

		[TestMethod]
		[DataRow("append")]
		[DataRow("remove-visited-then-append")]
		[DataRow("remove-unvisited-then-append")]
		// WinUI's DataContext walk reads the count once and indexes the live children collection
		// (FrameworkElement::PropagateDataContextChanged), so there a child removed during the walk makes it skip one.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Propagation_Callback_Adds_And_Removes_Children(string change)
		{
			// A propagation visits the children it started with, in order, each once, and a removal doesn't shift the
			// loop's other children. Ten children leave spare slots, so the append-only case writes into the array the
			// loop is reading; the removal cases append into the new array's spare slot. Whether the loop also reaches
			// the appended child can't be seen here (it would push the value the child already holds, which raises
			// nothing): Given_SnapshotArray's snapshot tests guard that.
			var marker = new object();
			var (panel, children, reached) = CreatePanel(10, marker);

			var added = new Border();
			var addedReached = 0;
			added.DataContextChanged += (_, e) =>
			{
				if (ReferenceEquals(e.NewValue, marker))
				{
					addedReached++;
				}
			};

			var done = false;
			children[1].DataContextChanged += (_, e) =>
			{
				if (!done && ReferenceEquals(e.NewValue, marker))
				{
					done = true;
					switch (change)
					{
						case "remove-visited-then-append":
							panel.Children.Remove(children[0]);
							break;
						case "remove-unvisited-then-append":
							panel.Children.Remove(children[5]);
							break;
					}

					panel.Children.Add(added);
				}
			};

			panel.DataContext = marker;

			CollectionAssert.AreEqual(children, reached.ToArray(), "The children the propagation started with, in order, each once");
			Assert.AreEqual(1, addedReached, "The child added during the propagation gets the value once");
			Assert.AreSame(marker, added.DataContext);
			Assert.AreSame(marker, children[9].DataContext);
		}

		[TestMethod]
		public void When_Children_Removed_The_Others_Keep_Their_Order()
		{
			var marker = new object();
			var (panel, children, reached) = CreatePanel(10, marker);

			panel.Children.Remove(children[0]);
			panel.Children.Remove(children[5]);
			panel.Children.Remove(children[9]);
			panel.DataContext = marker;

			CollectionAssert.AreEqual(
				new[] { 1, 2, 3, 4, 6, 7, 8 }.Select(i => children[i]).ToArray(),
				reached.ToArray());
			Assert.IsNull(children[5].DataContext);
		}

		[TestMethod]
		public void When_All_Children_Removed_Then_Added_Again()
		{
			var marker = new object();
			var (panel, children, reached) = CreatePanel(3, marker);

			panel.Children.Clear();
			panel.DataContext = marker;
			Assert.AreEqual(0, reached.Count, "No removed child is reached");

			panel.Children.Add(children[2]);
			panel.Children.Add(children[0]);
#if HAS_UNO
			// Uno pushes the inherited value as a child registers; WinUI raises no DataContextChanged when a child joins a parent outside the live tree.
			CollectionAssert.AreEqual(new[] { children[2], children[0] }, reached.ToArray(), "Children added again get the value as they join");
#endif

			reached.Clear();
			panel.DataContext = null;
			panel.DataContext = marker;
			CollectionAssert.AreEqual(new[] { children[2], children[0] }, reached.ToArray(), "and are reached in their new order");
		}
	}
}
