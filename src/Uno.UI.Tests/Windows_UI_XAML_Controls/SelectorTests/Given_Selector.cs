using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.SelectorTests
{
	[TestClass]
	public class Given_Selector
	{
		[TestMethod]
		public void When_Empty_SelectedValuePath()
		{
			var SUT = new Selector();
			SUT.ItemsSource = new[] { 1, 2, 3 };

			SUT.SelectedIndex = 0;
			Assert.AreEqual(1, SUT.SelectedValue);

			SUT.SelectedIndex = 1;
			Assert.AreEqual(2, SUT.SelectedValue);

			SUT.SelectedIndex = -1;
			Assert.IsNull(SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Single_SelectedValuePath()
		{
			var SUT = new Selector();
			SUT.ItemsSource = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.SelectedValuePath = "Item1";

			SUT.SelectedIndex = 0;
			Assert.AreEqual("1", SUT.SelectedValue);

			SUT.SelectedIndex = 1;
			Assert.AreEqual("2", SUT.SelectedValue);

			SUT.SelectedIndex = -1;
			Assert.IsNull(SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Single_SelectedValuePath_Changed()
		{
			var SUT = new Selector();
			var items = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.ItemsSource = items;

			SUT.SelectedIndex = 0;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = "Item1";

			Assert.AreEqual(items[0].Item1, SUT.SelectedValue);

			SUT.SelectedValuePath = "";

			Assert.AreEqual(items[0], SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Invalid_SelectedValuePath()
		{
			var SUT = new Selector();
			var items = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.ItemsSource = items;

			SUT.SelectedIndex = 0;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			//SUT.SelectedValuePath = "Item42";
			//Assert.IsNull(SUT.SelectedValue);

			SUT.SelectedValuePath = "Item2";
			Assert.AreEqual(1, SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Null_SelectedValuePath()
		{
			var SUT = new Selector();
			var items = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.ItemsSource = items;

			SUT.SelectedIndex = 0;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = null;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = "Item2";
			SUT.SelectedIndex = 0;
			Assert.AreEqual(1, SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Double_SelectedValuePath()
		{
			var SUT = new Selector();

			SUT.ItemsSource = new[] {
				Tuple.Create("1", Tuple.Create("11", 1)),
				Tuple.Create("2", Tuple.Create("22", 2)),
				Tuple.Create("3", Tuple.Create("22", 3))
			};

			SUT.SelectedValuePath = "Item2.Item1";

			SUT.SelectedIndex = 0;
			Assert.AreEqual("11", SUT.SelectedValue);

			SUT.SelectedIndex = 1;
			Assert.AreEqual("22", SUT.SelectedValue);

			SUT.SelectedIndex = -1;
			Assert.IsNull(SUT.SelectedValue);
		}

		[TestMethod]
		public void When_SelectedValue_Is_Set()
		{
			var SUT = new Selector();

			SUT.ItemsSource = new[] {
				"item 0",
				"item 1",
				"item 2",
				"item 3",
				"item 4"
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedValue = "item 3";
			Assert.AreEqual(3, SUT.SelectedIndex);

			SUT.SelectedValue = null;
			Assert.AreEqual(-1, SUT.SelectedIndex);
		}

		[TestMethod]
		public void When_SelectedIndex_Changed()
		{
			var SUT = new Selector();
			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				"item 0",
				"item 1",
				"item 2",
				"item 3",
				"item 4"
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedIndex = 0;

			Assert.AreEqual(source[0], SUT.SelectedValue);
			Assert.HasCount(1, selectionChanged);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.IsEmpty(selectionChanged[0].RemovedItems);
		}

		[TestMethod]
		public void When_SelectionChanged_And_SelectedItem_Changed()
		{
			var SUT = new Selector();
			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				new SelectorItem(){ Content = "item 1" },
				new SelectorItem(){ Content = "item 2" },
				new SelectorItem(){ Content = "item 3" },
				new SelectorItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedItem = source[0];

			Assert.AreEqual(source[0], SUT.SelectedValue);
			Assert.HasCount(1, selectionChanged);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.IsEmpty(selectionChanged[0].RemovedItems);

			SUT.SelectedItem = new ListViewItem() { Content = "unknown item" };

			Assert.AreEqual(source[0], SUT.SelectedValue);
			Assert.HasCount(1, selectionChanged);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.IsEmpty(selectionChanged[0].RemovedItems);
		}

		[TestMethod]
		public void When_SelectionChanged_And_SelectorItem_IsSelected_Changed()
		{
			var SUT = new Selector()
			{
				ItemsPanel = XamlHelper.LoadXaml<ItemsPanelTemplate>("<ItemsPanelTemplate><StackPanel /></ItemsPanelTemplate>"),
				Template = new ControlTemplate(() => new ItemsPresenter()),
			};
			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				new SelectorItem(){ Content = "item 1" },
				new SelectorItem(){ Content = "item 2" },
				new SelectorItem(){ Content = "item 3" },
				new SelectorItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedItem = source[0];

			Assert.AreEqual(source[0], SUT.SelectedValue);
			Assert.HasCount(1, selectionChanged);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.IsEmpty(selectionChanged[0].RemovedItems);
			Assert.IsTrue(source[0].IsSelected);

			source[1].IsSelected = true;

			Assert.AreEqual(source[1], SUT.SelectedItem);
			Assert.HasCount(2, selectionChanged);
			Assert.AreEqual(source[1], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(1, selectionChanged.Last().RemovedItems.Count);
			Assert.AreEqual(source[0], selectionChanged.Last().RemovedItems[0]);
			Assert.IsFalse(source[0].IsSelected);

			source[2].IsSelected = true;

			Assert.AreEqual(source[2], SUT.SelectedItem);
			Assert.HasCount(3, selectionChanged);
			Assert.AreEqual(source[2], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(1, selectionChanged.Last().RemovedItems.Count);
			Assert.AreEqual(source[1], selectionChanged.Last().RemovedItems[0]);
			Assert.IsTrue(source[2].IsSelected);
			Assert.IsFalse(source[1].IsSelected);
			Assert.IsFalse(source[0].IsSelected);

			source[2].IsSelected = false;

			Assert.IsNull(SUT.SelectedItem);
			Assert.HasCount(4, selectionChanged);
			Assert.AreEqual(source[2], selectionChanged.Last().RemovedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().AddedItems.Count);
			Assert.IsFalse(source[0].IsSelected);
			Assert.IsFalse(source[1].IsSelected);
			Assert.IsFalse(source[2].IsSelected);
		}

	}
}
