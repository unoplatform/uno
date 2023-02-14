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
using System.Windows.Data;

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
	}
}
