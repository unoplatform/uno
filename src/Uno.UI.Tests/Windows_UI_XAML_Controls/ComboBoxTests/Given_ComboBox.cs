using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.Tests.ComboBoxTests
{
	[TestClass]
	public class Given_ComboBox
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_ComboBox_Is_First_Opening()
		{
			var itemsPresenter = new ItemsPresenter();

			var popup = new Popup()
			{
				Child = itemsPresenter
			};

			var grid = new Grid()
			{
				Children =
				{
					popup.Name<Popup>("Popup"),
					new Border().Name<Border>("PopupBorder")
				}
			};

			var style = new Style(typeof(ComboBox))
			{
				Setters =  {
					new Setter<ComboBox>("Template", t =>
						t.Template = Funcs.Create(() => grid)
					)
				}
			};

			const int initialCount = 10;
			var array = Enumerable.Range(0, initialCount).Select(i => i * 10).ToArray();
			var source = new CollectionViewSource
			{
				Source = array
			};

			var view = source.View;

			var comboBox = new ComboBox()
			{
				Style = style,
				ItemsSource = view
			};

			comboBox.ApplyTemplate();

			comboBox.IsDropDownOpen = true;

			Assert.IsNotNull(comboBox.InternalItemsPanelRoot);
			Assert.IsNotNull(comboBox.ItemsPanelRoot);
		}

		[TestMethod]
		public void When_New_Item_Is_Inserted_Before_SelectedIndex()
		{
			var comboBox = new ComboBox();
			string[] items = new string[] { "string1", "string2", "string3", "string4" };
			foreach (string item in items.Reverse())
			{
				ComboBoxItem ni = new ComboBoxItem { Content = item, IsSelected = item == "string3" };
				comboBox.Items.Insert(0, ni);
			}

			Assert.AreEqual<int>(2, comboBox.SelectedIndex);
		}

		[TestMethod]
		public void When_Item_Is_Removed_Before_SelectedIndex()
		{
			var comboBox = new ComboBox();
			string[] items = new string[] { "string1", "string2", "string3", "string4" };
			foreach (string item in items)
			{
				ComboBoxItem ni = new ComboBoxItem { Content = item, IsSelected = item == "string3" };
				comboBox.Items.Add(ni);
			}

			Assert.AreEqual<int>(2, comboBox.SelectedIndex);


			comboBox.Items.RemoveAt(0);
			Assert.AreEqual<int>(1, comboBox.SelectedIndex);
		}

		[TestMethod]
		public void When_Removed_Item_Is_The_SelectedIndex()
		{
			var comboBox = new ComboBox();
			string[] items = new string[] { "string1", "string2", "string3", "string4" };
			foreach (string item in items)
			{
				ComboBoxItem ni = new ComboBoxItem { Content = item, IsSelected = item == "string3" };
				comboBox.Items.Add(ni);
			}

			Assert.AreEqual<int>(2, comboBox.SelectedIndex);


			comboBox.Items.RemoveAt(2);
			Assert.AreEqual<int>(-1, comboBox.SelectedIndex);
		}

		[TestMethod]
		public void When_SelectedItem_TwoWay_Binding()
		{
			var itemsControl = new ItemsControl()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemTemplate = new DataTemplate(() =>
				{
					var comboBox = new ComboBox();
					comboBox.Name = "combo";

					comboBox.SetBinding(
						ComboBox.ItemsSourceProperty,
						new Binding { Path = new("Numbers") });

					comboBox.SetBinding(
						ComboBox.SelectedItemProperty,
						new Binding { Path = new("SelectedNumber"), Mode = BindingMode.TwoWay });

					return comboBox;
				})
			};

			var test = new[] {
				new TwoWayBindingItem()
			};

			itemsControl.ForceLoaded();

			itemsControl.ItemsSource = test;

			var comboBox = itemsControl.FindName("combo") as ComboBox;

			Assert.AreEqual(3, test[0].SelectedNumber);
			Assert.AreEqual(3, comboBox.SelectedItem);
		}

		public class TwoWayBindingItem : System.ComponentModel.INotifyPropertyChanged
		{
			private int _selectedNumber;

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public TwoWayBindingItem()
			{
				Numbers = new List<int> { 1, 2, 3, 4 };
				SelectedNumber = 3;
			}

			public List<int> Numbers { get; }

			public int SelectedNumber
			{
				get
				{
					return _selectedNumber;
				}
				set
				{
					_selectedNumber = value;
					PropertyChanged?.Invoke(this, new(nameof(SelectedNumber)));
				}
			}
		}

		public class ItemModel
		{
			public string Text { get; set; }

			public override string ToString() => Text;
		}

	}
}
