using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Tests.Helpers;

namespace Uno.UI.Tests.CollectionViewTests
{
	[TestClass]
	public class Given_CollectionViewSource
	{
		[TestMethod]
		public void When_Backed_By_List()
		{
			const int initialCount = 10;
			var list = Enumerable.Range(0, initialCount).ToList();
			var source = new CollectionViewSource
			{
				Source = list,
				IsSourceGrouped = false
			};

			var view = source.View;
			Assert.AreEqual(initialCount, view.Count);

			list.Add(27);
			Assert.AreEqual(initialCount + 1, view.Count);

			view.Add(32);
			Assert.AreEqual(initialCount + 2, list.Count);
			Assert.AreEqual(32, list[initialCount + 1]);
		}

		[TestMethod]
		public void When_Empty()
		{
			var collection = Enumerable.Empty<int>();
			var source = new CollectionViewSource
			{
				Source = collection,
				IsSourceGrouped = false
			};

			var view = source.View;

			Assert.AreEqual(0, view.Count);
			Assert.IsNull(view.CurrentItem);
			Assert.AreEqual(0, view.CurrentPosition); //Not -1
		}

		[TestMethod]
		public void When_Current_Changed()
		{
			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array,
				IsSourceGrouped = false
			};

			var view = source.View;

			Assert.AreEqual(0, view.CurrentItem);
			Assert.AreEqual(0, view.CurrentPosition);

			view.MoveCurrentTo(30);
			Assert.AreEqual(30, view.CurrentItem);
			Assert.AreEqual(3, view.CurrentPosition);

			var eventRaised = false;
			view.CurrentChanging += (o, e) =>
			{
				eventRaised = true;
				e.Cancel = true;
			};

			view.MoveCurrentTo(50);
			Assert.IsTrue(eventRaised);
			Assert.AreEqual(30, view.CurrentItem);
		}

		[TestMethod]
		public void When_Grouped()
		{
			var ungrouped = new[] { "Arg", "Aought", "Crab", "Crump", "Dart", "Fish", "Flash", "Fork", "Lion", "Louse", "Lemur" };
			var items = ungrouped.GroupBy(s => s.First().ToString()).ToArray();
			var source = new CollectionViewSource()
			{
				Source = items,
				IsSourceGrouped = true,
			};
			var view = source.View;

			Assert.AreEqual(ungrouped.Length, view.Count);
			Assert.AreEqual(ungrouped[0], view.CurrentItem);
			Assert.AreEqual(ungrouped[3], view[3]);
		}

		[TestMethod]
		public void When_Grouped_CollectionGroups()
		{
			var ungrouped = new[] { "Arg", "Aought", "Crab", "Crump", "Dart", "Fish", "Flash", "Fork", "Lion", "Louse", "Lemur" };
			var items = ungrouped.GroupBy(s => s.First().ToString()).ToArray();
			var source = new CollectionViewSource()
			{
				Source = items,
				IsSourceGrouped = true,
			};
			var view = source.View;

			var groups = view.CollectionGroups.Cast<ICollectionViewGroup>().ToArray();
			var group = groups.First();

			Assert.AreEqual(items.First(), group.Group);
			Assert.IsTrue(group.GroupItems.SequenceEqual(items.First()));
			"".ToString();
		}

		[TestMethod]
		public void When_Grouped_Observable()
		{
			var ungrouped = new[] { "Arg", "Aought", "Crab", "Crump", "Dart", "Fish", "Flash", "Fork", "Lion", "Louse", "Lemur" };
			var groups = ungrouped.GroupBy(s => s.First().ToString()).ToArray();
			var obsGroups = groups.Select(g => new GroupedObservableCollection<string>(g)).ToArray();
			var source = new ObservableCollection<object>(obsGroups);

			var collectionView = new CollectionViewSource
			{
				Source = source,
				IsSourceGrouped = true
			}.View;

			var timesRootCalled = 0;
			collectionView.VectorChanged += (o, e) =>
			{
				timesRootCalled++;
				switch (timesRootCalled)
				{
					case 1:
						Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
						Assert.AreEqual(2, (int)e.Index);
						break;
					case 2:
						Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
						Assert.AreEqual(13, (int)e.Index);
						break;
					case 3:
						Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
						Assert.AreEqual(12, (int)e.Index);
						break;
				}
				"".ToString();
			};

			var timesGroupsCalled = 0;
			collectionView.CollectionGroups.VectorChanged += (o, e) =>
			{
				timesGroupsCalled++;
				//Assert.AreEqual(1, timesRootCalled);
				Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
				Assert.AreEqual(5, (int)e.Index);
				"".ToString();
			};

			var timesGroupItemsCalled = 0;
			(collectionView.CollectionGroups.First() as ICollectionViewGroup).GroupItems.VectorChanged += (o, e) =>
			{
				timesGroupItemsCalled++;
				Assert.AreEqual(1, timesGroupItemsCalled);
				Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
				Assert.AreEqual(2, (int)e.Index);
				"".ToString();
			};

			obsGroups[0].Add("Alibi");

			var newItems = new[] { "Nugget", "Numbat" };
			var newGroup = newItems.GroupBy(s => s.First().ToString()).Single();
			var newObsGroup = new GroupedObservableCollection<string>(newGroup);
			source.Add(newObsGroup);

			//Assert.AreEqual(3, timesRootCalled); //For the moment VectorChanged isn't called on the parent, unlike Windows
			Assert.AreEqual(1, timesGroupsCalled);
			Assert.AreEqual(1, timesGroupItemsCalled);
		}

		[TestMethod]
		public void When_Set_As_ItemsSource_And_Current_Initially_Set()
		{
			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array,
				IsSourceGrouped = false
			};

			var view = source.View;

			Assert.AreEqual(0, view.CurrentItem);
			Assert.AreEqual(0, view.CurrentPosition);

			view.MoveCurrentTo(30);
			Assert.AreEqual(30, view.CurrentItem);
			Assert.AreEqual(3, view.CurrentPosition);

			var list = new ListView();

			list.ItemsSource = view;

			Assert.AreEqual(3, list.SelectedIndex);
		}

		[TestMethod]
		public void When_Set_As_ItemsSource_And_Current_Initially_Set_Without_IsSynchronizedWithCurrentItem()
		{
			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array,
				IsSourceGrouped = false
			};

			var view = source.View;

			Assert.AreEqual(0, view.CurrentItem);
			Assert.AreEqual(0, view.CurrentPosition);

			view.MoveCurrentTo(30);
			Assert.AreEqual(30, view.CurrentItem);
			Assert.AreEqual(3, view.CurrentPosition);

			var list = new ListView() { IsSynchronizedWithCurrentItem = false };
			list.ItemsSource = view;

			Assert.AreEqual(-1, list.SelectedIndex);
		}

		[TestMethod]
		public void When_Set_As_ItemsSource_And_Current_Initially_Set_With_Changing_IsSynchronizedWithCurrentItem()
		{
			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array,
				IsSourceGrouped = false
			};

			var view = source.View;

			Assert.AreEqual(0, view.CurrentItem);
			Assert.AreEqual(0, view.CurrentPosition);

			view.MoveCurrentTo(30);
			Assert.AreEqual(30, view.CurrentItem);
			Assert.AreEqual(3, view.CurrentPosition);

			var list = new ListView() { IsSynchronizedWithCurrentItem = false };
			list.ItemsSource = view;

			Assert.AreEqual(-1, list.SelectedIndex);

			list.IsSynchronizedWithCurrentItem = null;

			Assert.AreEqual(3, list.SelectedIndex);

			list.IsSynchronizedWithCurrentItem = false;
		}

		[TestMethod]
		public void When_Set_As_ItemsSource_And_Current_Initially_Set_With_Disabled_IsSynchronizedWithCurrentItem()
		{
			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array,
				IsSourceGrouped = false
			};

			var view = source.View;

			Assert.AreEqual(0, view.CurrentItem);
			Assert.AreEqual(0, view.CurrentPosition);

			view.MoveCurrentTo(30);
			Assert.AreEqual(30, view.CurrentItem);
			Assert.AreEqual(3, view.CurrentPosition);

			var list = new ListView();
			list.ItemsSource = view;

			Assert.AreEqual(3, list.SelectedIndex);

			list.IsSynchronizedWithCurrentItem = false;

			Assert.AreEqual(-1, list.SelectedIndex);
		}

		[TestMethod]
		public void When_IsSynchronizedWithCurrentItem_Is_True()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ListView() { IsSynchronizedWithCurrentItem = true });
		}
	}
}
