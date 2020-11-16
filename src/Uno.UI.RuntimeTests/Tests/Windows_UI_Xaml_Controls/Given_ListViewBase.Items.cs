using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[Ignore("Items-ItemsSource sync not implemented yet")]
	public partial class Given_ListViewBase_Items
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Added_Count_Updated()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Added_ItemsSource_Stays_Null()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			Assert.IsNull(listView.ItemsSource);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Used_Items_NotNull()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1, 2 };
			Assert.IsNotNull(listView.Items);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Unset_Items_NotNull()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1, 2 };
			Assert.IsNotNull(listView.Items);
			listView.ItemsSource = null;
			Assert.IsNotNull(listView.Items);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Set_To_Empty_Items_Cleared()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			listView.ItemsSource = new List<int>();
			Assert.AreEqual(0, listView.Items.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Unset_Items_Not_Cleared()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			listView.ItemsSource = new List<int>() { 1, 3 };
			listView.ItemsSource = null;
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Unset_When_Already_Null_Items_Not_Cleared()
		{
			var listView = new ListView();
			listView.Items.Add(1);
			listView.ItemsSource = null;
			Assert.AreEqual(1, listView.Items.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Set_Items_Not_Modifiable()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1 };

			var thrown = false;
			try
			{
				listView.Items.Add(2);
			}
			catch (Exception) // Instead of Assert.ThrowsException as Uno throws InvalidOperationException, while UWP throws a generic Exception
			{
				thrown = true;
			}

			Assert.IsTrue(thrown);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Unset_Items_Modifiable()
		{
			var listView = new ListView();
			listView.ItemsSource = new List<int>() { 1 };
			listView.ItemsSource = null;
			listView.Items.Add(2);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_List_Modified_Change_Is_Reflected()
		{
			var listView = new ListView();
			var items = new List<int>() { 1 };
			listView.ItemsSource = items;
			Assert.AreEqual(1, listView.Items.Count);
			items.Add(2);
			Assert.AreEqual(2, listView.Items.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_List_Modified_VectorChange_Not_Triggered()
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
			Assert.AreEqual(2, listView.Items.Count);
			Assert.IsFalse(notified);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Resets_ItemsCollection_Reference_Does_Not_Change()
		{
			var listView = new ListView();
			var oldItems = listView.Items;

			listView.ItemsSource = new List<int>() { 1 };

			Assert.AreEqual(oldItems, listView.Items);

			listView.ItemsSource = new ObservableCollection<int>() { 3, 1 };

			Assert.AreEqual(oldItems, listView.Items);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_ObservableCollection_Modified_VectorChange_Not_Triggered()
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
		[RunsOnUIThread]
		public async Task When_ItemsSource_Set_Items_Sync()
		{
			var listView = new ListView();

			Assert.AreEqual(0, listView.Items.Count);

			listView.ItemsSource = new List<int>() { 1, 2, 3 };

			Assert.AreEqual(2, listView.Items[1]);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_Updated_Items_Sync()
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
		[RunsOnUIThread]
		public async Task When_ItemsSource_Changes_Items_VectorChanged_Triggered()
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
		public async Task When_ItemsSource_Enumerable_Changes_Items_Do_Not_Sync()
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
		public async Task When_ItemsSource_ReadOnly_Items_ReadOnly_Does_Not_Change()
		{
			var listView = new ListView();
			Assert.IsFalse(listView.Items.IsReadOnly);
			listView.ItemsSource = new ReadOnlyCollection<int>(new List<int>());
			Assert.IsFalse(listView.Items.IsReadOnly);
		}

		[TestMethod]
		public async Task When_ItemsSource_ICollection_Items_Do_Not_Sync()
		{
			var listView = new ListView();
			var items = new Dictionary<int, string>() { { 1, "Hello" } };

			listView.ItemsSource = items;
			Assert.AreEqual(1, listView.Items.Count);

			items.Add(2, "World");
			Assert.AreEqual(1, listView.Items.Count);
		}
	}
}
