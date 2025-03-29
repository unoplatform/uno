using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
#if !IS_UNIT_TESTS
	[RunsOnUIThread]
#endif
	[TestClass]
	public partial class Given_ListViewBase_Items
	{
		[TestMethod]
		public void When_Items_Added_Count_Updated()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		public void When_Items_Added_ItemsSource_Stays_Null()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			Assert.IsNull(listView.ItemsSource);
		}

		[TestMethod]
		public void When_ItemsSource_Used_Items_NotNull()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1, 2 };
			Assert.IsNotNull(listView.Items);
		}

		[TestMethod]
		public void When_ItemsSource_Unset_Items_NotNull()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1, 2 };
			Assert.IsNotNull(listView.Items);
			listView.ItemsSource = null;
			Assert.IsNotNull(listView.Items);
		}

		[TestMethod]
		public void When_ItemsSource_Set_To_Empty_Items_Cleared()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			listView.ItemsSource = new List<int>();
			Assert.AreEqual(0, listView.Items.Count);
		}

		[TestMethod]
		public void When_ItemsSource_Unset_Items_Not_Cleared()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			listView.ItemsSource = new List<int>() { 1, 3 };
			listView.ItemsSource = null;
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		public void When_ItemsSource_Unset_When_Already_Null_Items_Not_Cleared()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			listView.ItemsSource = null;
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		public void When_ItemsSource_Set_Items_Not_Modifiable()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1 };

			var thrown = false;
			try
			{
				listView.Items.Add(2);
			}
			catch (Exception) // Instead of Assert.ThrowsExactly as Uno throws InvalidOperationException, while UWP throws a generic Exception
			{
				thrown = true;
			}

			Assert.IsTrue(thrown);
		}

		[TestMethod]
		public void When_ItemsSource_Unset_Items_Modifiable()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1 };
			listView.ItemsSource = null;
			listView.Items.Add(2);
		}

		[TestMethod]
#if !WINAPPSDK
		[Ignore("#12183: ItemsControl.Items no longer map to ItemsSource for simple collection.")]
#endif
		public void When_ItemsSource_List_Modified_Change_Is_Reflected()
		{
			var items = new List<int>() { 1 };
			var listView = new ListView { ItemsSource = items };

			Assert.AreEqual(1, listView.Items.Count);
			items.Add(2);
			Assert.AreEqual(2, listView.Items.Count);
		}

		[TestMethod]
		public void When_ItemsSource_List_Modified_VectorChange_Not_Triggered()
		{
			var listView = new ListView();
			var items = new List<int>() { 1 };
			listView.ItemsSource = items;
			Assert.AreEqual(1, listView.Items.Count);
			var notified = false;
			listView.Items.VectorChanged += (s, e) =>
			{
				notified = true;
			};
			items.Add(2);
#if WINAPPSDK // #12183: ItemsControl.Items no longer map to ItemsSource for simple collection.
			Assert.AreEqual(2, listView.Items.Count);
#endif
			Assert.IsFalse(notified);
		}

		[TestMethod]
		public void When_ItemsSource_Resets_ItemsCollection_Reference_Does_Not_Change()
		{
			var listView = new ListView();
			var oldItems = listView.Items;

			listView.ItemsSource = new List<int>() { 1 };

			Assert.AreEqual(oldItems, listView.Items);

			listView.ItemsSource = new ObservableCollection<int>() { 3, 1 };

			Assert.AreEqual(oldItems, listView.Items);
		}

		[TestMethod]
		public void When_ItemsSource_ObservableCollection_Modified_VectorChange_Triggered()
		{
			var listView = new ListView();
			var items = new ObservableCollection<int>() { 1 };
			listView.ItemsSource = items;
			Assert.AreEqual(1, listView.Items.Count);
			var notified = false;
			listView.Items.VectorChanged += (s, e) =>
			{
				notified = true;
			};
			items.Add(2);
			Assert.AreEqual(2, listView.Items.Count);
			Assert.IsTrue(notified);
		}

		[TestMethod]
		public void When_ItemsSource_Set_Items_Sync()
		{
			var listView = new ListView();

			Assert.AreEqual(0, listView.Items.Count);

			listView.ItemsSource = new List<int>() { 1, 2, 3 };

			Assert.AreEqual(2, listView.Items[1]);
		}

		[TestMethod]
#if !WINAPPSDK
		[Ignore("#12183: ItemsControl.Items no longer map to ItemsSource for simple collection.")]
#endif
		public void When_ItemsSource_Updated_Items_Sync()
		{
			var listView = new ListView();

			Assert.AreEqual(0, listView.Items.Count);

			var items = new List<int>() { 1, 2, 3 };
			listView.ItemsSource = items;

			Assert.AreEqual(2, listView.Items[1]);

			items[1] = 6;

			Assert.AreEqual(6, listView.Items[1]);
		}


		[TestMethod]
		public void When_ItemsSource_Changes_Items_VectorChanged_Triggered()
		{
			var listView = new ListView();

			var triggerCount = 0;
			listView.Items.VectorChanged += (s, e) =>
			{
				triggerCount++;
			};

			listView.ItemsSource = new List<int>() { 1, 2 };

			Assert.AreEqual(1, triggerCount);

			listView.ItemsSource = new List<int>() { 3, 4 };

			Assert.AreEqual(2, triggerCount);

			listView.ItemsSource = null;

			Assert.AreEqual(3, triggerCount);
		}

		[TestMethod]
		public void When_ItemsSource_Enumerable_Changes_Items_Do_Not_Sync()
		{
			var listView = new ListView();

			var items = new List<int>() { 0, 1, 2, 3, 4 };

			listView.ItemsSource = items.Where(i => i % 2 == 0);

			Assert.AreEqual(3, listView.Items.Count);

			items.Add(5);
			items.Add(6);

			Assert.AreEqual(3, listView.Items.Count);
		}


		[TestMethod]
		public void When_ItemsSource_ReadOnly_Items_ReadOnly_Does_Not_Change()
		{
			var listView = new ListView();
			Assert.IsFalse(listView.Items.IsReadOnly);
			listView.ItemsSource = new ReadOnlyCollection<int>(new List<int>());
			Assert.IsFalse(listView.Items.IsReadOnly);
		}

		[TestMethod]
		public void When_ItemsSource_ICollection_Items_Do_Not_Sync()
		{
			var listView = new ListView();
			var items = new Dictionary<int, string>() { { 1, "Hello" } };

			listView.ItemsSource = items;
			Assert.AreEqual(1, listView.Items.Count);

			items.Add(2, "World");
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		public void When_ItemsSource_Is_CollectionViewSource_Ungrouped_Observable()
		{
			var listView = new ListView();
			var source = new ObservableCollection<int>(Enumerable.Range(0, 10));

			var cvs = new CollectionViewSource { Source = source };
			listView.ItemsSource = cvs.View;

			Assert.AreEqual(10, listView.Items.Count);

			var timesRaised = 0;
			CollectionChange change = default;
			var index = -1;
			listView.Items.VectorChanged += (o, e) =>
			{
				timesRaised++; //1
				change = e.CollectionChange; //insert
				index = (int)e.Index;
			};

			source.Add(10);

#if WINAPPSDK // CollectionView doesn't implement VectorChanged
			Assert.AreEqual(11, listView.Items.Count);
			Assert.AreEqual(1, timesRaised);
			Assert.AreEqual(CollectionChange.ItemInserted, change);
			Assert.AreEqual(10, index);
#endif
		}

		[TestMethod]
		public void When_ItemsSource_Grouped_Simple()
		{
			var listView = new ListView();
			var source = CountriesABC.GroupBy(s => s[0].ToString()).ToArray();

			var cvs = new CollectionViewSource
			{
				Source = source,
				IsSourceGrouped = true
			};
			listView.ItemsSource = cvs.View;

			Assert.AreEqual(CountriesABC.Length, listView.Items.Count);

			var sourceFlattened = source.SelectMany(g => g).ToArray();
			CollectionAssert.AreEqual(sourceFlattened, listView.Items.ToArray());
		}

		[TestMethod]
		public void When_ItemsSource_Grouped_Observables_Inner_Groups_Modified()
		{
			var listView = new ListView();
			var source = GetGroupedObservable(CountriesABC, s => s[0].ToString());

			var cvs = new CollectionViewSource
			{
				Source = source,
				IsSourceGrouped = true
			};
			listView.ItemsSource = cvs.View;

			Assert.AreEqual(CountriesABC.Length, listView.Items.Count);

			var timesRaised = 0;
			CollectionChange change = default;
			var index = -1;
			listView.Items.VectorChanged += (o, e) =>
			  {
				  timesRaised++; //1
				  change = e.CollectionChange; //insert
				  index = (int)e.Index; //11
			  };

			var a = source.Single(g => g.Key == "A");
			a.Add("Arendelle");

#if WINAPPSDK // CollectionView doesn't implement VectorChanged
			Assert.AreEqual(CountriesABC.Length + 1, listView.Items.Count);
			Assert.AreEqual(1, timesRaised);
			Assert.AreEqual(CollectionChange.ItemInserted, change);
			Assert.AreEqual(11, index);
#endif
		}

		[TestMethod]
		public void When_ItemsSource_Grouped_Observables_Outer_Modified()
		{
			var listView = new ListView();
			var source = GetGroupedObservable(CountriesABC, s => s[0].ToString());

			var cvs = new CollectionViewSource
			{
				Source = source,
				IsSourceGrouped = true
			};
			listView.ItemsSource = cvs.View;

			Assert.AreEqual(CountriesABC.Length, listView.Items.Count);

			var timesRaised = 0;
			var changeWasExpected = true;
			var expectedIndex = 49;
			var indexWasExpected = true;
			listView.Items.VectorChanged += (o, e) =>
			{
				timesRaised++;
				changeWasExpected &= e.CollectionChange == CollectionChange.ItemInserted;
				var index = (int)e.Index; //49, 48, 47, 46
				indexWasExpected &= index == expectedIndex;
				expectedIndex--;
			};

			var d = new GroupingObservableCollection<string, string>("D", CountriesD);
			source.Add(d);

#if WINAPPSDK // CollectionView doesn't implement VectorChanged
			Assert.AreEqual(CountriesABC.Length + CountriesD.Length, listView.Items.Count);
			Assert.AreEqual(4, timesRaised);
			Assert.IsTrue(changeWasExpected);
			Assert.IsTrue(indexWasExpected);
#endif
		}

		[TestMethod]
		public void When_ItemsSource_ObservableCollection_Selection()
		{
			var itemsSource = new ObservableCollection<string>(Enumerable.Range(0, 80).Select(i => $"Item {i}"));
			var SUT = new ListView { ItemsSource = itemsSource };

			SUT.SelectedIndex = 5;

			Assert.AreEqual(5, SUT.SelectedIndex);
			Assert.AreEqual("Item 5", SUT.SelectedItem);

			itemsSource.RemoveAt(2);

			Assert.AreEqual(4, SUT.SelectedIndex);
			Assert.AreEqual("Item 5", SUT.SelectedItem);

			itemsSource.RemoveAt(22);

			Assert.AreEqual(4, SUT.SelectedIndex);
			Assert.AreEqual("Item 5", SUT.SelectedItem);

			itemsSource.Insert(1, "Item 1.5");

			Assert.AreEqual(5, SUT.SelectedIndex);
			Assert.AreEqual("Item 5", SUT.SelectedItem);
		}

		private static ObservableCollection<GroupingObservableCollection<TKey, TElement>> GetGroupedObservable<TKey, TElement>(IEnumerable<TElement> source, Func<TElement, TKey> keySelector)
		{
			var observables = source.GroupBy(keySelector).Select(g => new GroupingObservableCollection<TKey, TElement>(g.Key, g));
			return new ObservableCollection<GroupingObservableCollection<TKey, TElement>>(observables);
		}

		private readonly string[] CountriesABC = new[]
{
			"ALGERIA",
			"ANDORRA",
			"AZERBAIJAN",
			"BRUNEI",
			"CENTRAL AFRICAN REPUBLIC",
			"CHINA",
			"BELIZE",
			"COLOMBIA",
			"BRAZIL",
			"CONGO, REPUBLIC OF THE",
			"BARBADOS",
			"BELGIUM",
			"ARGENTINA",
			"BURUNDI",
			"AUSTRALIA",
			"BANGLADESH",
			"BOTSWANA",
			"CUBA",
			"AFGHANISTAN",
			"CONGO, DEMOCRATIC REPUBLIC OF THE",
			"ANGOLA",
			"BAHRAIN",
			"BELARUS",
			"COMOROS",
			"BAHAMAS, THE",
			"CHAD",
			"CYPRUS",
			"CANADA",
			"BURKINA FASO",
			"CAMBODIA",
			"BENIN",
			"CZECH REPUBLIC",
			"CABO VERDE",
			"ANTIGUA AND BARBUDA",
			"COSTA RICA",
			"CHILE",
			"BOSNIA AND HERZEGOVINA",
			"BULGARIA",
			"BOLIVIA",
			"BHUTAN",
			"ALBANIA",
			"AUSTRIA",
			"CÔTE D'IVOIRE ",
			"CAMEROON",
			"ARMENIA",
			"CROATIA",
		};

		private readonly string[] CountriesD = new[]
		{
			"DENMARK",
			"DJIBOUTI",
			"DOMINICA",
			"DOMINICAN REPUBLIC"
		};
	}
}
